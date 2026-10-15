namespace SharingSystem.Domain.Constants;

/// <summary>
/// Field length limits and validation patterns shared by persistence and validation.
/// </summary>
public static class DomainLimits
{
    public const int UsernameMinLength = 3;
    public const int UsernameMaxLength = 30;
    public const string UsernamePattern = "^[a-zA-Z0-9._-]+$";

    public const int EmailMaxLength = 256;
    public const int PasswordMinLength = 8;
    public const int PasswordMaxLength = 128;
    public const int HashMaxLength = 512;

    public const int ShareIdMinLength = 3;
    public const int ShareIdMaxLength = 50;
    public const string ShareIdPattern = "^[a-zA-Z0-9_-]+$";
    public const int GeneratedShareIdLength = 8;

    public const int ShareNameMaxLength = 100;
    public const int ShareDescriptionMaxLength = 1024;
    public const int RecipientEmailsMaxLength = 2048;
    public const int MaxRecipients = 20;

    public const int FileNameMaxLength = 255;
    public const int ContentTypeMaxLength = 128;

    public const int ReverseShareTokenLength = 16;

    public const int AppNameMaxLength = 64;
    public const int UrlMaxLength = 512;
    public const int EmailSubjectMaxLength = 256;
    public const int EmailMessageMaxLength = 4096;
    public const int LegalTextMaxLength = 20000;
    public const int SmtpHostMaxLength = 256;
    public const int EncryptedSecretMaxLength = 1024;

    public const int TotpCodeLength = 6;
}
