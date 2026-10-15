namespace SharingSystem.Web.Infrastructure;

public static class WebConstants
{
    public const string AdminPolicy = "Admin";
    public const string AdminRole = "Admin";
    public const string SecurityStampClaim = "security_stamp";
    public const string AuthRateLimitPolicy = "auth";
    public const string UploadTokenHeader = "X-Upload-Token";
    public const string AntiforgeryHeader = "RequestVerificationToken";

    public const string AuthCookieName = "ss.auth";
    public const string AntiforgeryCookieName = "ss.af";
    public const string TwoFactorCookieName = "ss.2fa";
    public const string ShareAccessCookiePrefix = "ss.share.";

    public const string SuccessMessageKey = "SuccessMessage";
    public const string ErrorMessageKey = "ErrorMessage";
}
