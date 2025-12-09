namespace EcommerceLaptop.Core.ValueObjects;

/// <summary>
/// Service result wrapper for API responses
/// </summary>
/// <typeparam name="T">Data type</typeparam>
public class ServiceResult<T>
{
    public bool IsSuccess { get; set; }
    public T? Data { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string> Errors { get; set; } = new();

    public static ServiceResult<T> Success(T data)
    {
        return new ServiceResult<T>
        {
            IsSuccess = true,
            Data = data
        };
    }

    public static ServiceResult<T> Failure(string errorMessage)
    {
        return new ServiceResult<T>
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            Errors = new List<string> { errorMessage }
        };
    }

    public static ServiceResult<T> Failure(IEnumerable<string> errors)
    {
        return new ServiceResult<T>
        {
            IsSuccess = false,
            Errors = errors.ToList(),
            ErrorMessage = string.Join("; ", errors)
        };
    }
}

/// <summary>
/// Service result wrapper for operations without return data
/// </summary>
public class ServiceResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string> Errors { get; set; } = new();

    public static ServiceResult Success()
    {
        return new ServiceResult
        {
            IsSuccess = true
        };
    }

    public static ServiceResult Failure(string errorMessage)
    {
        return new ServiceResult
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            Errors = new List<string> { errorMessage }
        };
    }

    public static ServiceResult Failure(IEnumerable<string> errors)
    {
        return new ServiceResult
        {
            IsSuccess = false,
            Errors = errors.ToList(),
            ErrorMessage = string.Join("; ", errors)
        };
    }
}