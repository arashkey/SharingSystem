using Mapster;
using SharingSystem.Application.Settings;
using SharingSystem.Application.Shares;
using SharingSystem.Application.Users;
using SharingSystem.Domain.Entities;

namespace SharingSystem.Application.Mapping;

/// <summary>
/// Mapster configuration for mappings that cannot be resolved by naming conventions.
/// </summary>
public sealed class MappingRegister : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<ShareEntity, ShareSummaryDto>()
            .Map(dest => dest.HasPassword, src => src.PasswordHash != null)
            .Map(dest => dest.FileCount, src => src.Files.Count)
            .Map(dest => dest.TotalSize, src => src.Files.Sum(f => f.Size))
            .Map(dest => dest.CreatorUsername, src => src.Creator != null ? src.Creator.Username : null);

        config.NewConfig<ShareEntity, ShareDetailsDto>()
            .Map(dest => dest.HasPassword, src => src.PasswordHash != null)
            .Map(dest => dest.CreatorUsername, src => src.Creator != null ? src.Creator.Username : null);

        config.NewConfig<UserEntity, UserDto>()
            .Map(dest => dest.ShareCount, src => src.Shares.Count(s => s.IsCompleted));

        config.NewConfig<AppSettingsEntity, AppSettingsDto>()
            .Map(dest => dest.HasSmtpPassword, src => src.SmtpPasswordEncrypted != null);

        config.NewConfig<UpdateAppSettingsRequest, AppSettingsEntity>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.AppUrl)
            .Ignore(dest => dest.SmtpPasswordEncrypted)
            .Ignore(dest => dest.UpdatedAt);
    }
}
