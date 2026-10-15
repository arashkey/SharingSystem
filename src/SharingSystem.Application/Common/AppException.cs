namespace SharingSystem.Application.Common;

public enum AppErrorType
{
    Validation,
    NotFound,
    Forbidden
}

/// <summary>
/// An expected business error whose message can be shown to the user.
/// </summary>
public class AppException : Exception
{
    public AppException(AppErrorType errorType, string message)
        : base(message)
    {
        ErrorType = errorType;
    }

    public AppErrorType ErrorType { get; }

    public static AppException Validation(string message) => new(AppErrorType.Validation, message);

    public static AppException NotFound(string message) => new(AppErrorType.NotFound, message);

    public static AppException Forbidden(string message = "You are not allowed to perform this action.") =>
        new(AppErrorType.Forbidden, message);
}
