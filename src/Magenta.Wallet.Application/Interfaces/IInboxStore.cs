using System.Text.Json;

namespace Magenta.Wallet.Application.Interfaces;

public interface IInboxStore
{
    Task<bool> TryRecordInboxEventAsync(string source, string idempotencyKey, JsonDocument payload, CancellationToken cancellationToken = default);
    Task MarkAsProcessedAsync(string source, string idempotencyKey, CancellationToken cancellationToken = default);
}

