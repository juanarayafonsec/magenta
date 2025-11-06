namespace Magenta.Wallet.Domain.ValueObjects;

/// <summary>
/// Value object representing monetary amounts in minor units (long integers).
/// Prevents floating-point arithmetic errors in financial calculations.
/// </summary>
public readonly struct Money
{
    public long MinorUnits { get; }

    public Money(long minorUnits)
    {
        if (minorUnits < 0)
            throw new ArgumentException("Amount cannot be negative", nameof(minorUnits));
        
        MinorUnits = minorUnits;
    }

    public static Money Zero => new(0);

    public static Money operator +(Money left, Money right) => new(left.MinorUnits + right.MinorUnits);
    public static Money operator -(Money left, Money right) => new(left.MinorUnits - right.MinorUnits);
    public static Money operator *(Money money, long multiplier) => new(money.MinorUnits * multiplier);
    public static Money operator /(Money money, long divisor) => new(money.MinorUnits / divisor);
    
    public static bool operator >(Money left, Money right) => left.MinorUnits > right.MinorUnits;
    public static bool operator <(Money left, Money right) => left.MinorUnits < right.MinorUnits;
    public static bool operator >=(Money left, Money right) => left.MinorUnits >= right.MinorUnits;
    public static bool operator <=(Money left, Money right) => left.MinorUnits <= right.MinorUnits;

    public override bool Equals(object? obj) => obj is Money other && MinorUnits == other.MinorUnits;
    public override int GetHashCode() => MinorUnits.GetHashCode();
    public override string ToString() => MinorUnits.ToString();
}




