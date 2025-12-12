using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;

namespace EcommerceLaptop.Infrastructure.Middleware;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger = logger;

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "An unhandled exception has occurred: {Message}", exception.Message);

        var problemDetails = new ProblemDetails
        {
            Instance = httpContext.Request.Path
        };

        if (exception is ValidationException validationException)
        {
            problemDetails.Title = "Validation Error";
            problemDetails.Status = StatusCodes.Status400BadRequest;
            problemDetails.Detail = validationException.Message;
        }
        else if (exception is KeyNotFoundException)
        {
            problemDetails.Title = "Not Found";
            problemDetails.Status = StatusCodes.Status404NotFound;
            problemDetails.Detail = exception.Message;
        }
        else if (exception is UnauthorizedAccessException)
        {
            problemDetails.Title = "Unauthorized";
            problemDetails.Status = StatusCodes.Status401Unauthorized;
            problemDetails.Detail = "You are not authorized to access this resource.";
        }
        else if (exception is ArgumentException)
        {
             problemDetails.Title = "Bad Request";
             problemDetails.Status = StatusCodes.Status400BadRequest;
             problemDetails.Detail = exception.Message;
        }
        else
        {
            problemDetails.Title = "An error occurred while processing your request.";
            problemDetails.Status = StatusCodes.Status500InternalServerError;
            problemDetails.Detail = "An internal server error has occurred.";
        }

        httpContext.Response.StatusCode = problemDetails.Status.Value;

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}
