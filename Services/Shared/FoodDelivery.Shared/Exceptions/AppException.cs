namespace FoodDelivery.Shared.Exceptions;

/// <summary>Base application exception — maps to 400 Bad Request by default</summary>
public class AppException : Exception
{
    public int StatusCode { get; }

    public AppException(string message, int statusCode = 400) : base(message)
        => StatusCode = statusCode;
}

/// <summary>Resource not found — maps to 404</summary>
public class NotFoundException : AppException
{
    public NotFoundException(string resource, object id)
        : base($"{resource} with id '{id}' was not found.", 404) { }

    public NotFoundException(string message)
        : base(message, 404) { }
}

/// <summary>Duplicate / conflict — maps to 409</summary>
public class ConflictException : AppException
{
    public ConflictException(string message) : base(message, 409) { }
}

/// <summary>Business rule violation — maps to 422</summary>
public class BusinessRuleException : AppException
{
    public BusinessRuleException(string message) : base(message, 422) { }
}

/// <summary>Unauthorized access — maps to 403</summary>
public class ForbiddenException : AppException
{
    public ForbiddenException(string message = "Access denied.") : base(message, 403) { }
}

/// <summary>Invalid state transition — maps to 422</summary>
public class InvalidStatusTransitionException : AppException
{
    public InvalidStatusTransitionException(string from, string to, string role)
        : base($"Role '{role}' cannot transition order from '{from}' to '{to}'.", 422) { }
}
