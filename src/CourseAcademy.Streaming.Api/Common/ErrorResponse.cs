namespace CourseAcademy.Streaming.Api.Common;

/// <summary>
/// Body of every failed response: <c>{ "error": { "code", "message", "fields" } }</c>. Successful responses are the
/// plain DTO, without an envelope (the Main App's HttpStreamingClient reads them directly).
/// </summary>
public sealed record ErrorResponse(ErrorDetail Error)
{
    public static ErrorResponse Of(string code, string message, IReadOnlyList<FieldError>? fields = null) =>
        new(new ErrorDetail(code, message, fields ?? []));
}

/// <summary><see cref="Code"/> is a stable UPPER_SNAKE value the Main App can act on; <see cref="Message"/> is English text for logs.</summary>
public sealed record ErrorDetail(string Code, string Message, IReadOnlyList<FieldError> Fields);

/// <summary>One invalid field. <see cref="Field"/> is the camelCase property path.</summary>
public sealed record FieldError(string Field, string Message);

public static class ErrorCodes
{
    public const string ValidationFailed = "VALIDATION_FAILED";
    public const string Unauthorized = "UNAUTHORIZED";
    public const string Forbidden = "FORBIDDEN";
    public const string NotFound = "NOT_FOUND";
    public const string MethodNotAllowed = "METHOD_NOT_ALLOWED";
    public const string Conflict = "CONFLICT";
    public const string Unexpected = "UNEXPECTED_ERROR";
}
