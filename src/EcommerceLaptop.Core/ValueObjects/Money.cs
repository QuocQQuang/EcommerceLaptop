using System.Collections.Generic;
using EcommerceLaptop.Core.Common;

namespace EcommerceLaptop.Core.ValueObjects;

public class Money : ValueObject
{
    public decimal Amount { get; private set; }
    public string Currency { get; private set; }

    public Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency ?? "VND";
    }

    public static Money FromDecimal(decimal amount, string currency = "VND")
    {
        return new Money(amount, currency);
    }

    public static Money Zero(string currency = "VND") => new Money(0, currency);

    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException("Cannot add money with different currencies.");
        
        return new Money(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
         if (Currency != other.Currency)
            throw new InvalidOperationException("Cannot subtract money with different currencies.");

        return new Money(Amount - other.Amount, Currency);
    }

    public static Money operator +(Money a, Money b) => a.Add(b);
    public static Money operator -(Money a, Money b) => a.Subtract(b);
    
    public override string ToString()
    {
        return $"{Amount} {Currency}";
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
}
