namespace Magenta.Wallet.Domain.Entities;

/// <summary>
/// Blockchain network (e.g., TRON, ETHEREUM).
/// </summary>
public class Network
{
    public int NetworkId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NativeSymbol { get; set; } = string.Empty;
    public int ConfirmationsRequired { get; set; } = 1;
    public string? ExplorerUrl { get; set; }
    public bool IsActive { get; set; } = true;
    
    public ICollection<CurrencyNetwork> CurrencyNetworks { get; set; } = new List<CurrencyNetwork>();
}




