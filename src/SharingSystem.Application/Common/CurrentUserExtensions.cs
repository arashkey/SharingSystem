using SharingSystem.Application.Abstractions;

namespace SharingSystem.Application.Common;

public static class CurrentUserExtensions
{
    public static Guid RequireUserId(this ICurrentUser currentUser) =>
        currentUser.UserId ?? throw AppException.Forbidden("You need to sign in first.");

    public static bool CanManage(this ICurrentUser currentUser, Guid? ownerId) =>
        currentUser.IsAdmin || (ownerId.HasValue && currentUser.UserId == ownerId);
}
