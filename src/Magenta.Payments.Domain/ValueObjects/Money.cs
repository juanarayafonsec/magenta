namespace Magenta.Payments.Domain.ValueObjects;

/// <summary>
/// Represents monetary amounts in minor units (long integers).
/// Prevents floating-point arithmetic errors.
/// </summary>
public readonly struct Money
{
    public long MinorUnits { get; }

    public Money(long minorUnits)
    {
        if (minorUnits < 0)
            throw new ArgumentException("Money amount cannot be negative", nameof(minorUnits));
        
        MinorUnits = minorUnits;
    }

    public static Money Zero => new(0);

    public static Money operator +(Money left, Money right) => new(left.MinorUnits + right.MinorUnits);
    public static Money operator -(Money left, Money right) => new(left.MinorUnits - right.MinorUnits);
    public static bool operator >(Money left, Money right) => left.MinorUnits > right.MinorUnits;
    public static bool operator <(Money left, Money right) => left.MinorUnits < right.MinorUnits;
    public static bool operator >=(Money left, Money right) => left.MinorUnits >= right.MinorUnits;
    public static bool operator <=(Money left, Money right) => left.MinorUnits <= right.MinorUnits;
    public static bool operator ==(Money left, Money right) => left.MinorUnits == right.MinorUnits;
    public static bool operator !=(Money left, Money right) => left.MinorUnits != right.MinorUnits;

    public override bool Equals(object? obj) => obj is Money other && MinorUnits == other.MinorUnits;
    public override int GetHashCode() => MinorUnits.GetHashCode();
    public override string ToString() => MinorUnits.ToString();

    /// <summary>
    /// Converts minor units to major units using decimal places
    /// </summary>
    public decimal ToMajorUnits(int decimals)
    {
        return MinorUnits / (decimal)Math.Pow(10, decimals);
    }

    /// <summary>
    /// Converts major units to minor units using decimal places
    /// </summary>
    public static Money FromMajorUnits(decimal majorUnits, int decimals)
    {
        var minorUnits = (long)(majorUnits * (decimal)Math.Pow(10, decimals));
        return new Money(minorUnits);
    }
}

