using System.Collections.Generic;
using EcommerceLaptop.Core.Common;

namespace EcommerceLaptop.Core.ValueObjects;

public class Address : ValueObject
{
    public string Street { get; private set; }
    public string City { get; private set; }
    public string Province { get; private set; }
    public string PostalCode { get; private set; }
    public string Country { get; private set; }

    private Address() { } // EF Core

    public Address(string street, string city, string province, string postalCode, string country)
    {
        Street = street;
        City = city;
        Province = province;
        PostalCode = postalCode;
        Country = country;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Street;
        yield return City;
        yield return Province;
        yield return PostalCode;
        yield return Country;
    }
    
    public override string ToString()
    {
        return $"{Street}, {City}, {Province} {PostalCode}, {Country}";
    }
}
