namespace CourseAcademy.Streaming.Api.Common.Exceptions;

/// <summary>
/// An expected failure with a code and message that are safe to return to the caller.
/// Services throw these; the exception middleware turns them into an <see cref="ErrorResponse"/>.
/// </summary>
public abstract class AppException(int statusCode, string code, string message, IReadOnlyList<FieldError>? fields = null) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
    public string Code { get; } = code;
    public IReadOnlyList<FieldError> Fields { get; } = fields ?? [];
}

public sealed class ValidationFailedException(IReadOnlyList<FieldError> fields, string message = "The request has invalid fields.")
    : AppException(StatusCodes.Status400BadRequest, ErrorCodes.ValidationFailed, message, fields);

public sealed class NotFoundException(string message = "The requested item was not found.")
    : AppException(StatusCodes.Status404NotFound, ErrorCodes.NotFound, message);

/// <summary>The request does not fit the current state, e.g. completing an upload that is already complete.</summary>
public sealed class ConflictException(string code, string message) : AppException(StatusCodes.Status409Conflict, code, message);
