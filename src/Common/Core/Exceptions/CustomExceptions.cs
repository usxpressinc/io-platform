using System.ComponentModel.DataAnnotations;

namespace IO.Platform.Common.Core.Exceptions;

/// <summary>
/// Base exception for all business logic errors
/// </summary>
public class BusinessException : Exception
{
    public string? ErrorCode { get; }
    public int? StatusCode { get; }
    public List<string>? Details { get; }

    public BusinessException(string message, string? errorCode = null, int? statusCode = null, List<string>? details = null)
        : base(message)
    {
        ErrorCode = errorCode;
        StatusCode = statusCode;
        Details = details;
    }

    public BusinessException(string message, Exception innerException, string? errorCode = null, int? statusCode = null)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
        StatusCode = statusCode;
    }
}

/// <summary>
/// Exception for validation errors
/// </summary>
public class ValidationException : BusinessException
{
    public List<ValidationError> Errors { get; }

    public ValidationException(List<ValidationError> errors)
        : base("Validation failed", "validation_error", 400)
    {
        Errors = errors ?? new List<ValidationError>();
    }

    public ValidationException(string message, List<ValidationError>? errors = null)
        : base(message, "validation_error", 400)
    {
        Errors = errors ?? new List<ValidationError>();
    }
}

/// <summary>
/// Individual validation error
/// </summary>
public class ValidationError
{
    public string PropertyName { get; }
    public string ErrorMessage { get; }
    public string? ErrorCode { get; }

    public ValidationError(string propertyName, string errorMessage, string? errorCode = null)
    {
        PropertyName = propertyName;
        ErrorMessage = errorMessage;
        ErrorCode = errorCode;
    }
}

/// <summary>
/// Exception for resource not found errors
/// </summary>
public class NotFoundException : BusinessException
{
    public string ResourceType { get; }
    public string ResourceId { get; }

    public NotFoundException(string resourceType, string resourceId)
        : base($"{resourceType} with ID '{resourceId}' was not found", "not_found", 404)
    {
        ResourceType = resourceType;
        ResourceId = resourceId;
    }

    public NotFoundException(string message)
        : base(message, "not_found", 404)
    {
    }
}

/// <summary>
/// Exception for unauthorized access
/// </summary>
public class UnauthorizedException : BusinessException
{
    public UnauthorizedException(string message = "Unauthorized access")
        : base(message, "unauthorized", 401)
    {
    }
}

/// <summary>
/// Exception for forbidden access
/// </summary>
public class ForbiddenException : BusinessException
{
    public string? RequiredPermission { get; }

    public ForbiddenException(string? requiredPermission = null)
        : base("Access forbidden", "forbidden", 403)
    {
        RequiredPermission = requiredPermission;
    }

    public ForbiddenException(string message, string? requiredPermission = null)
        : base(message, "forbidden", 403)
    {
        RequiredPermission = requiredPermission;
    }
}

/// <summary>
/// Exception for conflicts
/// </summary>
public class ConflictException : BusinessException
{
    public string ResourceType { get; }
    public string ResourceId { get; }

    public ConflictException(string resourceType, string resourceId, string message = null)
        : base(message ?? $"Conflict with existing {resourceType}", "conflict", 409)
    {
        ResourceType = resourceType;
        ResourceId = resourceId;
    }
}

/// <summary>
/// Exception for timeout errors
/// </summary>
public class TimeoutException : BusinessException
{
    public TimeSpan TimeoutDuration { get; }
    public string Operation { get; }

    public TimeoutException(string operation, TimeSpan timeoutDuration)
        : base($"Operation '{operation}' timed out after {timeoutDuration.TotalSeconds} seconds", "timeout", 408)
    {
        Operation = operation;
        TimeoutDuration = timeoutDuration;
    }
}

/// <summary>
/// Exception for external service errors
/// </summary>
public class ExternalServiceException : BusinessException
{
    public string ServiceName { get; }
    public string? ServiceErrorCode { get; }
    public int? ServiceStatusCode { get; }

    public ExternalServiceException(
        string serviceName, 
        string message, 
        string? serviceErrorCode = null, 
        int? serviceStatusCode = null)
        : base(message, "external_service_error", 502)
    {
        ServiceName = serviceName;
        ServiceErrorCode = serviceErrorCode;
        ServiceStatusCode = serviceStatusCode;
    }

    public ExternalServiceException(
        string serviceName, 
        string message, 
        Exception innerException,
        string? serviceErrorCode = null, 
        int? serviceStatusCode = null)
        : base(message, innerException, "external_service_error", 502)
    {
        ServiceName = serviceName;
        ServiceErrorCode = serviceErrorCode;
        ServiceStatusCode = serviceStatusCode;
    }
}

/// <summary>
/// Exception for configuration errors
/// </summary>
public class ConfigurationException : BusinessException
{
    public string ConfigurationKey { get; }

    public ConfigurationException(string configurationKey, string message)
        : base($"Configuration error for '{configurationKey}': {message}", "configuration_error", 500)
    {
        ConfigurationKey = configurationKey;
    }
}

/// <summary>
/// Exception for rate limiting
/// </summary>
public class RateLimitExceededException : BusinessException
{
    public string LimitType { get; }
    public int Limit { get; }
    public TimeSpan RetryAfter { get; }

    public RateLimitExceededException(string limitType, int limit, TimeSpan retryAfter)
        : base($"Rate limit exceeded for {limitType}. Limit: {limit}. Retry after {retryAfter.TotalSeconds}s", "rate_limit_exceeded", 429)
    {
        LimitType = limitType;
        Limit = limit;
        RetryAfter = retryAfter;
    }
}

/// <summary>
/// Extension methods for validation
/// </summary>
public static class ValidationExtensions
{
    /// <summary>
    /// Validates an object and throws ValidationException if invalid
    /// </summary>
    public static void ValidateAndThrow(this object obj)
    {
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(obj);

        if (!Validator.TryValidateObject(obj, validationContext, validationResults, true))
        {
            var errors = validationResults.Select(vr => 
                new ValidationError(vr.MemberNames.FirstOrDefault() ?? "", vr.ErrorMessage));
            throw new ValidationException(errors.ToList());
        }
    }

    /// <summary>
    /// Checks if a condition is true and throws NotFoundException if false
    /// </summary>
    public static T ThrowIfNull<T>(this T? value, string resourceType, string resourceId) where T : class
    {
        if (value == null)
        {
            throw new NotFoundException(resourceType, resourceId);
        }
        return value;
    }

    /// <summary>
    /// Checks if a condition is true and throws BusinessException if false
    /// </summary>
    public static void ThrowIfFalse(this bool condition, string message, string? errorCode = null)
    {
        if (!condition)
        {
            throw new BusinessException(message, errorCode);
        }
    }
}
