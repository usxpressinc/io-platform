using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Net;

namespace IO.Platform.Common.Core.Exceptions;

/// <summary>
/// Global exception handling middleware for all IO Platform services
/// Provides consistent error responses and logging
/// </summary>
public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;
    private readonly ExceptionMiddlewareOptions _options;

    public ExceptionMiddleware(
        RequestDelegate next,
        ILogger<ExceptionMiddleware> logger,
        ExceptionMiddlewareOptions options)
    {
        _next = next;
        _logger = logger;
        _options = options;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var errorResponse = CreateErrorResponse(exception);
        var statusCode = GetStatusCode(exception);

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        // Add correlation ID if available
        if (context.Items.TryGetValue("CorrelationId", out var correlationId) && 
            correlationId is string correlationIdStr)
        {
            context.Response.Headers.Add("X-Correlation-ID", correlationIdStr);
        }

        // Log the exception with appropriate level
        LogException(exception, context, errorResponse);

        // Write error response
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            WriteIndented = _options.IncludeStackTrace
        };

        var jsonResponse = JsonSerializer.Serialize(errorResponse, jsonOptions);
        await context.Response.WriteAsync(jsonResponse);
    }

    private ErrorResponse CreateErrorResponse(Exception exception)
    {
        var errorId = Guid.NewGuid().ToString();
        var timestamp = DateTime.UtcNow;

        return exception switch
        {
            ValidationException validationEx => new ErrorResponse
            {
                ErrorId = errorId,
                ErrorCode = "validation_error",
                Message = validationEx.Message,
                Details = validationEx.Errors?.Select(e => new ErrorDetail
                {
                    Field = e.PropertyName,
                    Message = e.ErrorMessage,
                    Code = e.ErrorCode
                }).ToList(),
                Timestamp = timestamp,
                IncludeStackTrace = _options.IncludeStackTrace && !string.IsNullOrEmpty(exception.StackTrace)
            },
            BusinessException businessEx => new ErrorResponse
            {
                ErrorId = errorId,
                ErrorCode = businessEx.ErrorCode ?? "business_error",
                Message = businessEx.Message,
                Details = businessEx.Details?.Select(d => new ErrorDetail
                {
                    Message = d
                }).ToList(),
                Timestamp = timestamp,
                IncludeStackTrace = _options.IncludeStackTrace && !string.IsNullOrEmpty(exception.StackTrace)
            },
            NotFoundException notFoundEx => new ErrorResponse
            {
                ErrorId = errorId,
                ErrorCode = "not_found",
                Message = notFoundEx.Message,
                Timestamp = timestamp,
                IncludeStackTrace = false
            },
            UnauthorizedException unauthorizedEx => new ErrorResponse
            {
                ErrorId = errorId,
                ErrorCode = "unauthorized",
                Message = unauthorizedEx.Message,
                Timestamp = timestamp,
                IncludeStackTrace = false
            },
            ForbiddenException forbiddenEx => new ErrorResponse
            {
                ErrorId = errorId,
                ErrorCode = "forbidden",
                Message = forbiddenEx.Message,
                Timestamp = timestamp,
                IncludeStackTrace = false
            },
            TimeoutException timeoutEx => new ErrorResponse
            {
                ErrorId = errorId,
                ErrorCode = "timeout",
                Message = "Request timed out",
                Timestamp = timestamp,
                IncludeStackTrace = false
            },
            _ => new ErrorResponse
            {
                ErrorId = errorId,
                ErrorCode = "internal_server_error",
                Message = _options.ShowDetailedErrors ? exception.Message : "An unexpected error occurred",
                Timestamp = timestamp,
                IncludeStackTrace = _options.IncludeStackTrace && !string.IsNullOrEmpty(exception.StackTrace)
            }
        };
    }

    private int GetStatusCode(Exception exception)
    {
        return exception switch
        {
            ValidationException => (int)HttpStatusCode.BadRequest,
            NotFoundException => (int)HttpStatusCode.NotFound,
            UnauthorizedException => (int)HttpStatusCode.Unauthorized,
            ForbiddenException => (int)HttpStatusCode.Forbidden,
            TimeoutException => (int)HttpStatusCode.RequestTimeout,
            BusinessException businessEx when businessEx.StatusCode.HasValue => businessEx.StatusCode.Value,
            _ => (int)HttpStatusCode.InternalServerError
        };
    }

    private void LogException(Exception exception, HttpContext context, ErrorResponse errorResponse)
    {
        var logLevel = GetLogLevel(exception);
        var path = context.Request.Path;
        var method = context.Request.Method;
        var userAgent = context.Request.Headers["User-Agent"].FirstOrDefault();
        var correlationId = context.Items.TryGetValue("CorrelationId", out var cid) ? cid?.ToString() : null;

        var logMessage = $"Exception in {method} {path} [{errorResponse.ErrorCode}] {exception.Message}";

        if (logLevel == LogLevel.Error)
        {
            _logger.LogError(exception, 
                "{LogMessage} | ErrorId: {ErrorId} | CorrelationId: {CorrelationId} | UserAgent: {UserAgent}",
                logMessage, errorResponse.ErrorId, correlationId, userAgent);
        }
        else
        {
            _logger.Log(logLevel, exception,
                "{LogMessage} | ErrorId: {ErrorId} | CorrelationId: {CorrelationId} | UserAgent: {UserAgent}",
                logMessage, errorResponse.ErrorId, correlationId, userAgent);
        }
    }

    private LogLevel GetLogLevel(Exception exception)
    {
        return exception switch
        {
            ValidationException => LogLevel.Warning,
            NotFoundException => LogLevel.Information,
            UnauthorizedException => LogLevel.Warning,
            ForbiddenException => LogLevel.Warning,
            BusinessException => LogLevel.Warning,
            TimeoutException => LogLevel.Warning,
            _ => LogLevel.Error
        };
    }
}

/// <summary>
/// Configuration options for exception middleware
/// </summary>
public class ExceptionMiddlewareOptions
{
    public bool ShowDetailedErrors { get; set; } = false;
    public bool IncludeStackTrace { get; set; } = false;
    public bool LogRequestDetails { get; set; } = true;
}

/// <summary>
/// Standard error response format
/// </summary>
public class ErrorResponse
{
    public string ErrorId { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public List<ErrorDetail>? Details { get; set; }
    public DateTime Timestamp { get; set; }
    public bool IncludeStackTrace { get; set; }
    public string? StackTrace { get; set; }
}

/// <summary>
/// Error detail for validation errors
/// </summary>
public class ErrorDetail
{
    public string? Field { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Code { get; set; }
}

/// <summary>
/// Extension methods for registering exception middleware
/// </summary>
public static class ExceptionMiddlewareExtensions
{
    /// <summary>
    /// Adds exception middleware with default options
    /// </summary>
    public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ExceptionMiddleware>(new ExceptionMiddlewareOptions());
    }

    /// <summary>
    /// Adds exception middleware with custom options
    /// </summary>
    public static IApplicationBuilder UseExceptionHandling(
        this IApplicationBuilder builder,
        Action<ExceptionMiddlewareOptions> configureOptions)
    {
        var options = new ExceptionMiddlewareOptions();
        configureOptions(options);
        return builder.UseMiddleware<ExceptionMiddleware>(options);
    }
}
