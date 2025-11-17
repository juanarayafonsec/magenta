namespace Magenta.Wallet.Infrastructure.Data;

public class Network
{
    public int NetworkId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NativeSymbol { get; set; } = string.Empty;
    public int ConfirmationsRequired { get; set; } = 1;
    public string? ExplorerUrl { get; set; }
    public bool IsActive { get; set; } = true;
}

