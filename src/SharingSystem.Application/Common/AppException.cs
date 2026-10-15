using System.Globalization;

namespace SharingSystem.Application.Common;

public enum AppErrorType
{
    Validation,
    NotFound,
    Forbidden
}

/// <summary>
/// An expected business error whose message can be shown to the user.
/// The message is kept as an English template with <c>{0}</c>-style placeholders plus its arguments,
/// so the presentation layer can translate it; <see cref="Exception.Message"/> holds the formatted English text.
/// </summary>
public class AppException : Exception
{
    public const string DefaultForbiddenMessage = "You are not allowed to perform this action.";

    public AppException(AppErrorType errorType, string messageTemplate, params object[] messageArguments)
        : base(FormatMessage(messageTemplate, messageArguments))
    {
        ErrorType = errorType;
        MessageTemplate = messageTemplate;
        MessageArguments = messageArguments;
    }

    public AppErrorType ErrorType { get; }

    /// <summary>English message with <c>{0}</c>-style placeholders; also used as the translation key.</summary>
    public string MessageTemplate { get; }

    /// <summary>Values for the placeholders of <see cref="MessageTemplate"/>.</summary>
    public object[] MessageArguments { get; }

    public static AppException Validation(string messageTemplate, params object[] arguments) =>
        new(AppErrorType.Validation, messageTemplate, arguments);

    public static AppException NotFound(string messageTemplate, params object[] arguments) =>
        new(AppErrorType.NotFound, messageTemplate, arguments);

    public static AppException Forbidden() => new(AppErrorType.Forbidden, DefaultForbiddenMessage);

    public static AppException Forbidden(string messageTemplate, params object[] arguments) =>
        new(AppErrorType.Forbidden, messageTemplate, arguments);

    private static string FormatMessage(string template, object[] arguments) =>
        arguments.Length == 0 ? template : string.Format(CultureInfo.InvariantCulture, template, arguments);
}
