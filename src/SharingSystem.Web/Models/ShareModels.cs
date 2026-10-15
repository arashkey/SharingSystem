using System.ComponentModel.DataAnnotations;
using SharingSystem.Application.Common;
using SharingSystem.Application.ReverseShares;
using SharingSystem.Application.Shares;
using SharingSystem.Domain.Constants;

namespace SharingSystem.Web.Models;

public sealed class UploadPageViewModel
{
    public required UploadLimitsDto Limits { get; init; }
    public required string BaseUrl { get; init; }
    public required string SuggestedId { get; init; }
    public string? ReverseShareToken { get; init; }
    public string? ReverseShareCreator { get; init; }
    public bool IsSimplified { get; init; }
    public bool CanSendToRecipients { get; init; }
}

public sealed class ShareListViewModel
{
    public required PagedResult<ShareSummaryDto> Shares { get; init; }
    public required string BaseUrl { get; init; }
}

public sealed class EditSharePageViewModel
{
    public required ShareDetailsDto Share { get; init; }
    public required EditShareForm Form { get; init; }
    public required UploadLimitsDto Limits { get; init; }
    public required string BaseUrl { get; init; }
}

public sealed class EditShareForm
{
    [StringLength(DomainLimits.ShareNameMaxLength)]
    [Display(Name = "Name")]
    public string? Name { get; set; }

    [StringLength(DomainLimits.ShareDescriptionMaxLength)]
    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Range(1, int.MaxValue)]
    [Display(Name = "Maximum views")]
    public int? MaxViews { get; set; }

    [Display(Name = "Change expiration")]
    public bool ChangeExpiration { get; set; }

    public ExpirationInput Expiration { get; set; } = new();

    [DataType(DataType.Password)]
    [Display(Name = "New password")]
    public string? NewPassword { get; set; }

    [Display(Name = "Remove password")]
    public bool RemovePassword { get; set; }
}

public sealed class ShareViewModel
{
    public required ShareViewDto View { get; init; }
    public required string ShareUrl { get; init; }
}

public sealed class SharePasswordViewModel
{
    public string ShareId { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;
}

public sealed class ReverseSharesPageViewModel
{
    public required IReadOnlyList<ReverseShareDto> ReverseShares { get; init; }
    public required CreateReverseShareForm Form { get; init; }
    public required string BaseUrl { get; init; }
    public long MaxShareSizeBytes { get; init; }
    public bool CanSendEmails { get; init; }
}

public sealed class CreateReverseShareForm
{
    private const int DefaultMaxShareSizeMegabytes = 100;

    public ExpirationInput Expiration { get; set; } = new();

    [Range(1, long.MaxValue)]
    [Display(Name = "Maximum share size (MB)")]
    public long MaxShareSizeMegabytes { get; set; } = DefaultMaxShareSizeMegabytes;

    [Range(1, int.MaxValue)]
    [Display(Name = "Maximum use count")]
    public int MaxUseCount { get; set; } = 1;

    [Display(Name = "Send me an e-mail when a share is created")]
    public bool SendEmailNotification { get; set; }

    [Display(Name = "Simplified mode (hide share options)")]
    public bool IsSimplified { get; set; }

    [Display(Name = "Public access (visitors can open the created shares)")]
    public bool IsPublicAccess { get; set; } = true;
}
