namespace EcommerceLaptop.Core.Configuration;

/// <summary>
/// Configuration options for address validation
/// </summary>
public class AddressValidationOptions
{
    public const string SectionName = "AddressValidation";

    /// <summary>
    /// Cache expiration time in minutes
    /// </summary>
    public int CacheExpirationMinutes { get; set; } = 60;

    /// <summary>
    /// Maximum retry attempts for validation
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// API timeout in seconds
    /// </summary>
    public int ApiTimeoutSeconds { get; set; } = 10;

    /// <summary>
    /// Enable fuzzy matching for addresses
    /// </summary>
    public bool EnableFuzzyMatching { get; set; } = true;

    /// <summary>
    /// Fuzzy match threshold (0.0 to 1.0)
    /// </summary>
    public double FuzzyMatchThreshold { get; set; } = 0.8;

    /// <summary>
    /// Province validation settings
    /// </summary>
    public ValidationRules ProvinceValidation { get; set; } = new();

    /// <summary>
    /// District validation settings
    /// </summary>
    public ValidationRules DistrictValidation { get; set; } = new();

    /// <summary>
    /// Ward validation settings
    /// </summary>
    public ValidationRules WardValidation { get; set; } = new();
}

/// <summary>
/// Validation rules for address components
/// </summary>
public class ValidationRules
{
    /// <summary>
    /// Require exact match for validation
    /// </summary>
    public bool RequireExactMatch { get; set; } = false;

    /// <summary>
    /// Allow common abbreviations (e.g., "TP" for "Thnh ph")
    /// </summary>
    public bool AllowCommonAbbreviations { get; set; } = true;

    /// <summary>
    /// Custom validation patterns (regex)
    /// </summary>
    public List<string> CustomPatterns { get; set; } = new();

    /// <summary>
    /// Excluded terms that should not be considered valid
    /// </summary>
    public List<string> ExcludedTerms { get; set; } = new();
}