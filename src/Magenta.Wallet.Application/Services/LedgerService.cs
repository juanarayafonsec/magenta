using Magenta.Wallet.Application.DTOs;
using Magenta.Wallet.Application.Events;
using Magenta.Wallet.Application.Interfaces;
using Magenta.Wallet.Domain.Entities;
using Magenta.Wallet.Domain.Enums;
using System.Text.Json;

namespace Magenta.Wallet.Application.Services;

public class LedgerService : ILedgerService
{
    private readonly IAccountRepository _accountRepository;
    private readonly ILedgerRepository _ledgerRepository;
    private readonly IIdempotencyRepository _idempotencyRepository;
    private readonly IOutboxRepository _outboxRepository;
    private readonly IWalletUnitOfWork _unitOfWork;
    private readonly IEventPublisher _eventPublisher;

    public LedgerService(
        IAccountRepository accountRepository,
        ILedgerRepository ledgerRepository,
        IIdempotencyRepository idempotencyRepository,
        IOutboxRepository outboxRepository,
        IWalletUnitOfWork unitOfWork,
        IEventPublisher eventPublisher)
    {
        _accountRepository = accountRepository ?? throw new ArgumentNullException(nameof(accountRepository));
        _ledgerRepository = ledgerRepository ?? throw new ArgumentNullException(nameof(ledgerRepository));
        _idempotencyRepository = idempotencyRepository ?? throw new ArgumentNullException(nameof(idempotencyRepository));
        _outboxRepository = outboxRepository ?? throw new ArgumentNullException(nameof(outboxRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
    }

    public async Task<OperationResult> ReserveWithdrawalAsync(ReserveWithdrawalRequest request, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            // Check idempotency
            if (await _idempotencyRepository.ExistsAsync(request.Source, request.IdempotencyKey, cancellationToken))
            {
                var existingTxId = await _idempotencyRepository.GetTransactionIdAsync(request.Source, request.IdempotencyKey, cancellationToken);
                return OperationResult.SuccessResult(existingTxId!.Value);
            }

            // Lock and get accounts
            var mainAccount = await _accountRepository.GetOrCreateAccountAsync(
                request.PlayerId, request.CurrencyNetworkId, "MAIN", cancellationToken);
            var holdAccount = await _accountRepository.GetOrCreateAccountAsync(
                request.PlayerId, request.CurrencyNetworkId, "WITHDRAW_HOLD", cancellationToken);

            // Lock accounts for update (SERIALIZABLE isolation)
            await _accountRepository.LockAccountForUpdateAsync(mainAccount.AccountId, cancellationToken);
            await _accountRepository.LockAccountForUpdateAsync(holdAccount.AccountId, cancellationToken);

            // Check balance
            var balance = await _ledgerRepository.CalculateAccountBalanceAsync(mainAccount.AccountId, cancellationToken);
            if (balance < request.AmountMinor)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                return OperationResult.FailureResult($"Insufficient balance. Available: {balance}, Requested: {request.AmountMinor}");
            }

            // Create transaction
            var transaction = new LedgerTransaction
            {
                LedgerTransactionId = Guid.NewGuid(),
                ReferenceType = ReferenceType.Withdrawal.ToString(),
                ReferenceId = request.IdempotencyKey,
                Source = request.Source,
                Metadata = JsonDocument.Parse($"{{ \"playerId\": {request.PlayerId}, \"amountMinor\": {request.AmountMinor} }}")
            };

            // Create postings: DR MAIN / CR WITHDRAW_HOLD
            transaction.Postings.Add(Domain.Services.LedgerService.CreatePosting(mainAccount.AccountId, PostingDirection.DR, request.AmountMinor));
            transaction.Postings.Add(Domain.Services.LedgerService.CreatePosting(holdAccount.AccountId, PostingDirection.CR, request.AmountMinor));

            // Validate transaction
            Domain.Services.LedgerService.ValidateTransaction(transaction);

            // Save transaction
            await _ledgerRepository.CreateTransactionAsync(transaction, cancellationToken);

            // Create idempotency key
            await _idempotencyRepository.CreateIdempotencyKeyAsync(request.Source, request.IdempotencyKey, transaction.LedgerTransactionId, cancellationToken);

            // Create outbox event
            var withdrawalEvent = new WithdrawalReservedEvent
            {
                PlayerId = request.PlayerId,
                CurrencyNetworkId = request.CurrencyNetworkId,
                AmountMinor = request.AmountMinor,
                TransactionId = transaction.LedgerTransactionId,
                IdempotencyKey = request.IdempotencyKey
            };

            var outboxEvent = new OutboxEvent
            {
                EventType = nameof(WithdrawalReservedEvent),
                RoutingKey = "wallet.withdrawal.reserved",
                Payload = JsonDocument.Parse(JsonSerializer.Serialize(withdrawalEvent)),
                Status = "PENDING"
            };
            await _outboxRepository.CreateOutboxEventAsync(outboxEvent, cancellationToken);

            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            return OperationResult.SuccessResult(transaction.LedgerTransactionId);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            return OperationResult.FailureResult(ex.Message);
        }
    }

    public async Task<OperationResult> FinalizeWithdrawalSettledAsync(FinalizeWithdrawalSettledRequest request, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            // Check idempotency
            if (await _idempotencyRepository.ExistsAsync(request.Source, request.IdempotencyKey, cancellationToken))
            {
                var existingTxId = await _idempotencyRepository.GetTransactionIdAsync(request.Source, request.IdempotencyKey, cancellationToken);
                return OperationResult.SuccessResult(existingTxId!.Value);
            }

            var holdAccount = await _accountRepository.GetOrCreateAccountAsync(
                request.PlayerId, request.CurrencyNetworkId, "WITHDRAW_HOLD", cancellationToken);
            var houseAccount = await _accountRepository.GetOrCreateAccountAsync(
                null, request.CurrencyNetworkId, "HOUSE", cancellationToken);

            await _accountRepository.LockAccountForUpdateAsync(holdAccount.AccountId, cancellationToken);
            await _accountRepository.LockAccountForUpdateAsync(houseAccount.AccountId, cancellationToken);

            var transaction = new LedgerTransaction
            {
                LedgerTransactionId = Guid.NewGuid(),
                ReferenceType = ReferenceType.Withdrawal.ToString(),
                ReferenceId = request.IdempotencyKey,
                Source = request.Source
            };

            // DR WITHDRAW_HOLD / CR HOUSE
            transaction.Postings.Add(Domain.Services.LedgerService.CreatePosting(holdAccount.AccountId, PostingDirection.DR, request.AmountMinor));
            transaction.Postings.Add(Domain.Services.LedgerService.CreatePosting(houseAccount.AccountId, PostingDirection.CR, request.AmountMinor));

            // Add fee if applicable
            if (request.FeeMinor.HasValue && request.FeeMinor.Value > 0)
            {
                var feesAccount = await _accountRepository.GetOrCreateAccountAsync(
                    null, request.CurrencyNetworkId, "HOUSE:FEES", cancellationToken);
                await _accountRepository.LockAccountForUpdateAsync(feesAccount.AccountId, cancellationToken);
                transaction.Postings.Add(Domain.Services.LedgerService.CreatePosting(feesAccount.AccountId, PostingDirection.CR, request.FeeMinor.Value));
            }

            Domain.Services.LedgerService.ValidateTransaction(transaction);
            await _ledgerRepository.CreateTransactionAsync(transaction, cancellationToken);
            await _idempotencyRepository.CreateIdempotencyKeyAsync(request.Source, request.IdempotencyKey, transaction.LedgerTransactionId, cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            return OperationResult.SuccessResult(transaction.LedgerTransactionId);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            return OperationResult.FailureResult(ex.Message);
        }
    }

    public async Task<OperationResult> FinalizeWithdrawalFailedAsync(FinalizeWithdrawalFailedRequest request, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            if (await _idempotencyRepository.ExistsAsync(request.Source, request.IdempotencyKey, cancellationToken))
            {
                var existingTxId = await _idempotencyRepository.GetTransactionIdAsync(request.Source, request.IdempotencyKey, cancellationToken);
                return OperationResult.SuccessResult(existingTxId!.Value);
            }

            var holdAccount = await _accountRepository.GetOrCreateAccountAsync(
                request.PlayerId, request.CurrencyNetworkId, "WITHDRAW_HOLD", cancellationToken);
            var mainAccount = await _accountRepository.GetOrCreateAccountAsync(
                request.PlayerId, request.CurrencyNetworkId, "MAIN", cancellationToken);

            await _accountRepository.LockAccountForUpdateAsync(holdAccount.AccountId, cancellationToken);
            await _accountRepository.LockAccountForUpdateAsync(mainAccount.AccountId, cancellationToken);

            // Get the amount from the hold account balance
            var holdBalance = await _ledgerRepository.CalculateAccountBalanceAsync(holdAccount.AccountId, cancellationToken);
            if (holdBalance <= 0)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                return OperationResult.FailureResult("No funds in withdrawal hold account to reverse");
            }

            var transaction = new LedgerTransaction
            {
                LedgerTransactionId = Guid.NewGuid(),
                ReferenceType = ReferenceType.Withdrawal.ToString(),
                ReferenceId = request.IdempotencyKey,
                Source = request.Source
            };

            // DR WITHDRAW_HOLD / CR MAIN
            transaction.Postings.Add(Domain.Services.LedgerService.CreatePosting(holdAccount.AccountId, PostingDirection.DR, holdBalance));
            transaction.Postings.Add(Domain.Services.LedgerService.CreatePosting(mainAccount.AccountId, PostingDirection.CR, holdBalance));

            Domain.Services.LedgerService.ValidateTransaction(transaction);
            await _ledgerRepository.CreateTransactionAsync(transaction, cancellationToken);
            await _idempotencyRepository.CreateIdempotencyKeyAsync(request.Source, request.IdempotencyKey, transaction.LedgerTransactionId, cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            return OperationResult.SuccessResult(transaction.LedgerTransactionId);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            return OperationResult.FailureResult(ex.Message);
        }
    }

    public async Task<OperationResult> ApplyDepositSettlementAsync(DepositSettlementRequest request, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            // Use transaction hash as idempotency key
            if (await _idempotencyRepository.ExistsAsync(request.Source, request.TransactionHash, cancellationToken))
            {
                var existingTxId = await _idempotencyRepository.GetTransactionIdAsync(request.Source, request.TransactionHash, cancellationToken);
                return OperationResult.SuccessResult(existingTxId!.Value);
            }

            var mainAccount = await _accountRepository.GetOrCreateAccountAsync(
                request.PlayerId, request.CurrencyNetworkId, "MAIN", cancellationToken);
            var houseAccount = await _accountRepository.GetOrCreateAccountAsync(
                null, request.CurrencyNetworkId, "HOUSE", cancellationToken);

            await _accountRepository.LockAccountForUpdateAsync(mainAccount.AccountId, cancellationToken);
            await _accountRepository.LockAccountForUpdateAsync(houseAccount.AccountId, cancellationToken);

            var transaction = new LedgerTransaction
            {
                LedgerTransactionId = Guid.NewGuid(),
                ReferenceType = ReferenceType.Deposit.ToString(),
                ReferenceId = request.TransactionHash,
                Source = request.Source,
                Metadata = JsonDocument.Parse($"{{ \"txHash\": \"{request.TransactionHash}\" }}")
            };

            // DR HOUSE / CR MAIN
            transaction.Postings.Add(Domain.Services.LedgerService.CreatePosting(houseAccount.AccountId, PostingDirection.DR, request.AmountMinor));
            transaction.Postings.Add(Domain.Services.LedgerService.CreatePosting(mainAccount.AccountId, PostingDirection.CR, request.AmountMinor));

            Domain.Services.LedgerService.ValidateTransaction(transaction);
            await _ledgerRepository.CreateTransactionAsync(transaction, cancellationToken);
            await _idempotencyRepository.CreateIdempotencyKeyAsync(request.Source, request.TransactionHash, transaction.LedgerTransactionId, cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            return OperationResult.SuccessResult(transaction.LedgerTransactionId);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            return OperationResult.FailureResult(ex.Message);
        }
    }

    public async Task<OperationResult> PostBetAsync(BetRequest request, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            if (await _idempotencyRepository.ExistsAsync(request.Source, request.BetId, cancellationToken))
            {
                var existingTxId = await _idempotencyRepository.GetTransactionIdAsync(request.Source, request.BetId, cancellationToken);
                return OperationResult.SuccessResult(existingTxId!.Value);
            }

            var mainAccount = await _accountRepository.GetOrCreateAccountAsync(
                request.PlayerId, request.CurrencyNetworkId, "MAIN", cancellationToken);
            var wagerAccount = await _accountRepository.GetOrCreateAccountAsync(
                null, request.CurrencyNetworkId, "HOUSE:WAGER", cancellationToken);

            await _accountRepository.LockAccountForUpdateAsync(mainAccount.AccountId, cancellationToken);
            await _accountRepository.LockAccountForUpdateAsync(wagerAccount.AccountId, cancellationToken);

            // Check balance
            var balance = await _ledgerRepository.CalculateAccountBalanceAsync(mainAccount.AccountId, cancellationToken);
            if (balance < request.AmountMinor)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                return OperationResult.FailureResult($"Insufficient balance for bet. Available: {balance}, Required: {request.AmountMinor}");
            }

            var transaction = new LedgerTransaction
            {
                LedgerTransactionId = Guid.NewGuid(),
                ReferenceType = ReferenceType.Bet.ToString(),
                ReferenceId = request.BetId,
                Source = request.Source
            };

            // DR MAIN / CR HOUSE:WAGER
            transaction.Postings.Add(Domain.Services.LedgerService.CreatePosting(mainAccount.AccountId, PostingDirection.DR, request.AmountMinor));
            transaction.Postings.Add(Domain.Services.LedgerService.CreatePosting(wagerAccount.AccountId, PostingDirection.CR, request.AmountMinor));

            Domain.Services.LedgerService.ValidateTransaction(transaction);
            await _ledgerRepository.CreateTransactionAsync(transaction, cancellationToken);
            await _idempotencyRepository.CreateIdempotencyKeyAsync(request.Source, request.BetId, transaction.LedgerTransactionId, cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            return OperationResult.SuccessResult(transaction.LedgerTransactionId);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            return OperationResult.FailureResult(ex.Message);
        }
    }

    public async Task<OperationResult> PostWinAsync(WinRequest request, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            if (await _idempotencyRepository.ExistsAsync(request.Source, request.WinId, cancellationToken))
            {
                var existingTxId = await _idempotencyRepository.GetTransactionIdAsync(request.Source, request.WinId, cancellationToken);
                return OperationResult.SuccessResult(existingTxId!.Value);
            }

            var mainAccount = await _accountRepository.GetOrCreateAccountAsync(
                request.PlayerId, request.CurrencyNetworkId, "MAIN", cancellationToken);
            var wagerAccount = await _accountRepository.GetOrCreateAccountAsync(
                null, request.CurrencyNetworkId, "HOUSE:WAGER", cancellationToken);

            await _accountRepository.LockAccountForUpdateAsync(mainAccount.AccountId, cancellationToken);
            await _accountRepository.LockAccountForUpdateAsync(wagerAccount.AccountId, cancellationToken);

            var transaction = new LedgerTransaction
            {
                LedgerTransactionId = Guid.NewGuid(),
                ReferenceType = ReferenceType.Win.ToString(),
                ReferenceId = request.WinId,
                Source = request.Source
            };

            // DR HOUSE:WAGER / CR MAIN
            transaction.Postings.Add(Domain.Services.LedgerService.CreatePosting(wagerAccount.AccountId, PostingDirection.DR, request.AmountMinor));
            transaction.Postings.Add(Domain.Services.LedgerService.CreatePosting(mainAccount.AccountId, PostingDirection.CR, request.AmountMinor));

            Domain.Services.LedgerService.ValidateTransaction(transaction);
            await _ledgerRepository.CreateTransactionAsync(transaction, cancellationToken);
            await _idempotencyRepository.CreateIdempotencyKeyAsync(request.Source, request.WinId, transaction.LedgerTransactionId, cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            return OperationResult.SuccessResult(transaction.LedgerTransactionId);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            return OperationResult.FailureResult(ex.Message);
        }
    }

    public async Task<OperationResult> RollbackTransactionAsync(RollbackRequest request, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            if (await _idempotencyRepository.ExistsAsync(request.Source, request.RollbackId, cancellationToken))
            {
                var existingTxId = await _idempotencyRepository.GetTransactionIdAsync(request.Source, request.RollbackId, cancellationToken);
                return OperationResult.SuccessResult(existingTxId!.Value);
            }

            // Find original transaction
            var originalTransaction = await _ledgerRepository.GetTransactionByReferenceAsync(
                request.Source, request.OriginalTransactionReference, cancellationToken);

            if (originalTransaction == null)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                return OperationResult.FailureResult($"Original transaction not found: {request.OriginalTransactionReference}");
            }

            // Create compensating transaction with reversed postings
            var rollbackTransaction = new LedgerTransaction
            {
                LedgerTransactionId = Guid.NewGuid(),
                ReferenceType = ReferenceType.Rollback.ToString(),
                ReferenceId = request.RollbackId,
                Source = request.Source,
                Metadata = JsonDocument.Parse($"{{ \"originalReference\": \"{request.OriginalTransactionReference}\" }}")
            };

            // Lock all accounts involved in original transaction
            foreach (var posting in originalTransaction.Postings)
            {
                await _accountRepository.LockAccountForUpdateAsync(posting.AccountId, cancellationToken);
                
                // Reverse the direction
                var reversedDirection = posting.Direction == PostingDirection.DR.ToString() 
                    ? PostingDirection.CR 
                    : PostingDirection.DR;
                    
                rollbackTransaction.Postings.Add(Domain.Services.LedgerService.CreatePosting(
                    posting.AccountId, reversedDirection, posting.AmountMinor));
            }

            Domain.Services.LedgerService.ValidateTransaction(rollbackTransaction);
            await _ledgerRepository.CreateTransactionAsync(rollbackTransaction, cancellationToken);
            await _idempotencyRepository.CreateIdempotencyKeyAsync(request.Source, request.RollbackId, rollbackTransaction.LedgerTransactionId, cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            return OperationResult.SuccessResult(rollbackTransaction.LedgerTransactionId);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            return OperationResult.FailureResult(ex.Message);
        }
    }
}
