using System.ComponentModel.DataAnnotations;
using SharingSystem.Application.Shares;
using SharingSystem.Domain.Enums;

namespace SharingSystem.Web.Models;

public sealed class ErrorViewModel
{
    public int StatusCode { get; init; }
    public string? Message { get; init; }
}

/// <summary>Form input for an expiration (maps to <see cref="ExpirationRequest"/>).</summary>
public sealed class ExpirationInput
{
    private const int DefaultValue = 7;

    [Display(Name = "Never expires")]
    public bool NeverExpires { get; set; }

    [Range(1, ExpirationCalculator.MaxValue)]
    [Display(Name = "Expires after")]
    public int Value { get; set; } = DefaultValue;

    [Display(Name = "Unit")]
    public ExpirationUnit Unit { get; set; } = ExpirationUnit.Days;
}
