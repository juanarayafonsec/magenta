using Magenta.Payment.Application.DTOs;
using Magenta.Payment.Application.Interfaces;
using Magenta.Payment.Application.Services;
using Magenta.Payment.Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Magenta.Payment.Application.Tests;

public class DepositServiceTests
{
    private readonly Mock<IDepositSessionRepository> _sessionRepositoryMock;
    private readonly Mock<IDepositRequestRepository> _depositRepositoryMock;
    private readonly Mock<IPaymentProviderRepository> _providerRepositoryMock;
    private readonly Mock<IIdempotencyRepository> _idempotencyRepositoryMock;
    private readonly Mock<IWalletGrpcClient> _walletClientMock;
    private readonly Mock<IOutboxRepository> _outboxRepositoryMock;
    private readonly Mock<IPaymentUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IEventPublisher> _eventPublisherMock;
    private readonly Mock<ILogger<DepositService>> _loggerMock;
    private readonly DepositService _depositService;

    public DepositServiceTests()
    {
        _sessionRepositoryMock = new Mock<IDepositSessionRepository>();
        _depositRepositoryMock = new Mock<IDepositRequestRepository>();
        _providerRepositoryMock = new Mock<IPaymentProviderRepository>();
        _idempotencyRepositoryMock = new Mock<IIdempotencyRepository>();
        _walletClientMock = new Mock<IWalletGrpcClient>();
        _outboxRepositoryMock = new Mock<IOutboxRepository>();
        _unitOfWorkMock = new Mock<IPaymentUnitOfWork>();
        _eventPublisherMock = new Mock<IEventPublisher>();
        _loggerMock = new Mock<ILogger<DepositService>>();

        var providerAdapter = new Mock<IPaymentProvider>();
        providerAdapter.Setup(p => p.CreateDepositSessionAsync(
            It.IsAny<int>(),
            It.IsAny<long?>(),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DepositSessionResult
            {
                Address = "test-address",
                MemoOrTag = "test-memo",
                ProviderReference = "ref-123",
                ExpiresAt = DateTime.UtcNow.AddHours(24)
            });

        _depositService = new DepositService(
            _sessionRepositoryMock.Object,
            _depositRepositoryMock.Object,
            _providerRepositoryMock.Object,
            _idempotencyRepositoryMock.Object,
            _walletClientMock.Object,
            _outboxRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _eventPublisherMock.Object,
            _loggerMock.Object,
            new[] { providerAdapter.Object });
    }

    [Fact]
    public async Task CreateDepositSessionAsync_WithNewIdempotencyKey_CreatesSession()
    {
        // Arrange
        var request = new CreateDepositSessionRequest
        {
            PlayerId = 1,
            ProviderId = 1,
            CurrencyNetworkId = 1,
            ExpectedAmountMinor = 1000000,
            ConfirmationsRequired = 1
        };
        var idempotencyKey = Guid.NewGuid().ToString();

        _idempotencyRepositoryMock.Setup(r => r.ExistsAsync("payments", idempotencyKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _providerRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentProvider
            {
                ProviderId = 1,
                Name = "Test Provider",
                IsActive = true
            });

        _sessionRepositoryMock.Setup(r => r.CreateAsync(It.IsAny<DepositSession>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DepositSession s, CancellationToken ct) => s);

        // Act
        var result = await _depositService.CreateDepositSessionAsync(request, idempotencyKey);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.SessionId);
        Assert.Equal("test-address", result.Address);
        _sessionRepositoryMock.Verify(r => r.CreateAsync(It.IsAny<DepositSession>(), It.IsAny<CancellationToken>()), Times.Once);
        _idempotencyRepositoryMock.Verify(r => r.CreateAsync(It.IsAny<IdempotencyKey>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateDepositSessionAsync_WithExistingIdempotencyKey_ReturnsExistingSession()
    {
        // Arrange
        var request = new CreateDepositSessionRequest
        {
            PlayerId = 1,
            ProviderId = 1,
            CurrencyNetworkId = 1
        };
        var idempotencyKey = Guid.NewGuid().ToString();
        var existingSessionId = Guid.NewGuid();

        _idempotencyRepositoryMock.Setup(r => r.ExistsAsync("payments", idempotencyKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _idempotencyRepositoryMock.Setup(r => r.GetTxIdAsync("payments", idempotencyKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingSessionId);

        _sessionRepositoryMock.Setup(r => r.GetByIdAsync(existingSessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DepositSession
            {
                SessionId = existingSessionId,
                Address = "existing-address",
                Status = "OPEN",
                ExpiresAt = DateTime.UtcNow.AddHours(24)
            });

        // Act
        var result = await _depositService.CreateDepositSessionAsync(request, idempotencyKey);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(existingSessionId, result.SessionId);
        Assert.Equal("existing-address", result.Address);
        _sessionRepositoryMock.Verify(r => r.CreateAsync(It.IsAny<DepositSession>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
