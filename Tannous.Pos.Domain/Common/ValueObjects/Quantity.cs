namespace Tannous.Pos.Domain.Common.ValueObjects;

public record Quantity
{
    public decimal Value { get; }
    public string Unit { get; }

    public Quantity(decimal value, string unit = "pcs")
    {
        if (value < 0)
            throw new ArgumentException("Quantity cannot be negative", nameof(value));
        
        if (string.IsNullOrWhiteSpace(unit))
            throw new ArgumentException("Unit cannot be empty", nameof(unit));

        Value = value;
        Unit = unit.ToLowerInvariant();
    }

    public static Quantity Zero(string unit = "pcs") => new(0, unit);
    
    public static Quantity operator +(Quantity left, Quantity right)
    {
        if (left.Unit != right.Unit)
            throw new InvalidOperationException("Cannot add quantities with different units");
        
        return new Quantity(left.Value + right.Value, left.Unit);
    }
    
    public static Quantity operator -(Quantity left, Quantity right)
    {
        if (left.Unit != right.Unit)
            throw new InvalidOperationException("Cannot subtract quantities with different units");
        
        return new Quantity(left.Value - right.Value, left.Unit);
    }
    
    public static Quantity operator *(Quantity quantity, decimal multiplier)
    {
        return new Quantity(quantity.Value * multiplier, quantity.Unit);
    }
    
    public override string ToString()
    {
        return $"{Value} {Unit}";
    }
}
