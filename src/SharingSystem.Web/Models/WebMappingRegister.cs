using Mapster;
using SharingSystem.Application.Common;
using SharingSystem.Application.ReverseShares;
using SharingSystem.Application.Settings;
using SharingSystem.Application.Shares;

namespace SharingSystem.Web.Models;

/// <summary>
/// Mapster configuration between form models and application requests.
/// </summary>
public sealed class WebMappingRegister : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<CreateReverseShareForm, CreateReverseShareRequest>()
            .Map(dest => dest.MaxShareSizeBytes, src => src.MaxShareSizeMegabytes * ByteSize.BytesPerMegabyte);

        config.NewConfig<ShareDetailsDto, EditShareForm>()
            .Ignore(dest => dest.Expiration);

        config.NewConfig<AppSettingsDto, SettingsForm>()
            .Map(dest => dest.MaxShareSizeMegabytes, src => src.MaxShareSizeBytes / ByteSize.BytesPerMegabyte)
            .Map(dest => dest.ChunkSizeMegabytes, src => src.ChunkSizeBytes / ByteSize.BytesPerMegabyte);

        config.NewConfig<SettingsForm, UpdateAppSettingsRequest>()
            .Map(dest => dest.MaxShareSizeBytes, src => src.MaxShareSizeMegabytes * ByteSize.BytesPerMegabyte)
            .Map(dest => dest.ChunkSizeBytes, src => (int)(src.ChunkSizeMegabytes * ByteSize.BytesPerMegabyte));
    }
}
