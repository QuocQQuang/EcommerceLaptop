using System.Collections.Generic;
using EcommerceLaptop.Core.Common;
using System.Text.RegularExpressions;

namespace EcommerceLaptop.Core.ValueObjects;

public class Email : ValueObject
{
    public string Value { get; private set; }

    public Email(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Email cannot be empty", nameof(value));

        if (!IsValidEmail(value))
            throw new ArgumentException("Invalid email format", nameof(value));

        Value = value;
    }

    private bool IsValidEmail(string email)
    {
        try 
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch 
        {
            return false;
        }
    }

    public static implicit operator string(Email email) => email.Value;
    public static explicit operator Email(string email) => new Email(email);

    public override string ToString() => Value;

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}
