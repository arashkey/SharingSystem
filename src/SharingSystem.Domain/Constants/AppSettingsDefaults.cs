namespace SharingSystem.Domain.Constants;

/// <summary>
/// Default values for the runtime configuration that is stored in the database.
/// </summary>
public static class AppSettingsDefaults
{
    private const long BytesPerMegabyte = 1024L * 1024L;
    private const long MegabytesPerGigabyte = 1024L;

    public const string AppName = "Sharing System";

    public const int SessionDurationHours = 72;

    public const long MaxShareSizeBytes = MegabytesPerGigabyte * BytesPerMegabyte;

    public const int ChunkSizeBytes = (int)(10 * BytesPerMegabyte);

    public const int SmtpPort = 587;

    public const string ShareRecipientsSubject = "Files shared with you";

    public const string ShareRecipientsMessage =
        "Hey!\n\n{creator} shared some files with you. View or download the files with this link: {shareUrl}\n\n" +
        "The share will expire {expires}.\n\nNote: {desc}\n\nShared securely with {appName}";

    public const string ReverseShareSubject = "Reverse share link used";

    public const string ReverseShareMessage =
        "Hey!\n\nA share was just created with your reverse share link: {shareUrl}\n\nShared securely with {appName}";

    public const string ResetPasswordSubject = "Reset your password";

    public const string ResetPasswordMessage =
        "Hey!\n\nYou requested a password reset. Click this link to reset your password: {url}\n\n" +
        "The link expires in one hour.\n\n{appName}";

    public const string InviteSubject = "You have been invited";

    public const string InviteMessage =
        "Hey!\n\nYou were invited to {appName}. You can sign in at {url} with your e-mail address and " +
        "the following password: {password}\n\nPlease change your password after signing in.";
}
