using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharingSystem.Domain.Constants;
using SharingSystem.Domain.Entities;

namespace SharingSystem.Infrastructure.Persistence.Configurations;

public sealed class UserEntityConfiguration : IEntityTypeConfiguration<UserEntity>
{
    private const int SecurityStampMaxLength = 64;

    public void Configure(EntityTypeBuilder<UserEntity> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Username).HasMaxLength(DomainLimits.UsernameMaxLength).IsRequired();
        builder.Property(u => u.Email).HasMaxLength(DomainLimits.EmailMaxLength).IsRequired();
        builder.Property(u => u.PasswordHash).HasMaxLength(DomainLimits.HashMaxLength).IsRequired();
        builder.Property(u => u.SecurityStamp).HasMaxLength(SecurityStampMaxLength).IsRequired();
        builder.Property(u => u.TotpSecretEncrypted).HasMaxLength(DomainLimits.EncryptedSecretMaxLength);
        builder.HasIndex(u => u.Username).IsUnique();
        builder.HasIndex(u => u.Email).IsUnique();
    }
}

public sealed class ShareEntityConfiguration : IEntityTypeConfiguration<ShareEntity>
{
    public void Configure(EntityTypeBuilder<ShareEntity> builder)
    {
        builder.ToTable("Shares");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasMaxLength(DomainLimits.ShareIdMaxLength);
        builder.Property(s => s.Name).HasMaxLength(DomainLimits.ShareNameMaxLength);
        builder.Property(s => s.Description).HasMaxLength(DomainLimits.ShareDescriptionMaxLength);
        builder.Property(s => s.PasswordHash).HasMaxLength(DomainLimits.HashMaxLength);
        builder.Property(s => s.UploadTokenHash).HasMaxLength(DomainLimits.HashMaxLength);
        builder.Property(s => s.RecipientEmails).HasMaxLength(DomainLimits.RecipientEmailsMaxLength);
        builder.HasIndex(s => s.ExpiresAt);
        builder.HasIndex(s => s.CreatorId);

        builder.HasOne(s => s.Creator)
            .WithMany(u => u.Shares)
            .HasForeignKey(s => s.CreatorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.ReverseShare)
            .WithMany(r => r.Shares)
            .HasForeignKey(s => s.ReverseShareId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public sealed class ShareFileEntityConfiguration : IEntityTypeConfiguration<ShareFileEntity>
{
    public void Configure(EntityTypeBuilder<ShareFileEntity> builder)
    {
        builder.ToTable("ShareFiles");
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Name).HasMaxLength(DomainLimits.FileNameMaxLength).IsRequired();
        builder.Property(f => f.ContentType).HasMaxLength(DomainLimits.ContentTypeMaxLength).IsRequired();

        builder.HasOne(f => f.Share)
            .WithMany(s => s.Files)
            .HasForeignKey(f => f.ShareId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ReverseShareEntityConfiguration : IEntityTypeConfiguration<ReverseShareEntity>
{
    public void Configure(EntityTypeBuilder<ReverseShareEntity> builder)
    {
        builder.ToTable("ReverseShares");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Token).HasMaxLength(DomainLimits.ReverseShareTokenLength).IsRequired();
        builder.HasIndex(r => r.Token).IsUnique();

        builder.HasOne(r => r.Creator)
            .WithMany(u => u.ReverseShares)
            .HasForeignKey(r => r.CreatorId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class PasswordResetTokenEntityConfiguration : IEntityTypeConfiguration<PasswordResetTokenEntity>
{
    public void Configure(EntityTypeBuilder<PasswordResetTokenEntity> builder)
    {
        builder.ToTable("PasswordResetTokens");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.TokenHash).HasMaxLength(DomainLimits.HashMaxLength).IsRequired();
        builder.HasIndex(t => t.TokenHash).IsUnique();

        builder.HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class AppSettingsEntityConfiguration : IEntityTypeConfiguration<AppSettingsEntity>
{
    private const int EnumMaxLength = 32;

    public void Configure(EntityTypeBuilder<AppSettingsEntity> builder)
    {
        builder.ToTable("AppSettings");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.Property(s => s.AppName).HasMaxLength(DomainLimits.AppNameMaxLength).IsRequired();
        builder.Property(s => s.AppUrl).HasMaxLength(DomainLimits.UrlMaxLength);
        builder.Property(s => s.ZipCompression).HasConversion<string>().HasMaxLength(EnumMaxLength);

        builder.Property(s => s.ShareRecipientsSubject).HasMaxLength(DomainLimits.EmailSubjectMaxLength);
        builder.Property(s => s.ReverseShareSubject).HasMaxLength(DomainLimits.EmailSubjectMaxLength);
        builder.Property(s => s.ResetPasswordSubject).HasMaxLength(DomainLimits.EmailSubjectMaxLength);
        builder.Property(s => s.InviteSubject).HasMaxLength(DomainLimits.EmailSubjectMaxLength);
        builder.Property(s => s.ShareRecipientsMessage).HasMaxLength(DomainLimits.EmailMessageMaxLength);
        builder.Property(s => s.ReverseShareMessage).HasMaxLength(DomainLimits.EmailMessageMaxLength);
        builder.Property(s => s.ResetPasswordMessage).HasMaxLength(DomainLimits.EmailMessageMaxLength);
        builder.Property(s => s.InviteMessage).HasMaxLength(DomainLimits.EmailMessageMaxLength);

        builder.Property(s => s.SmtpHost).HasMaxLength(DomainLimits.SmtpHostMaxLength);
        builder.Property(s => s.SmtpFromEmail).HasMaxLength(DomainLimits.EmailMaxLength);
        builder.Property(s => s.SmtpUsername).HasMaxLength(DomainLimits.EmailMaxLength);
        builder.Property(s => s.SmtpPasswordEncrypted).HasMaxLength(DomainLimits.EncryptedSecretMaxLength);

        builder.Property(s => s.ImprintText).HasMaxLength(DomainLimits.LegalTextMaxLength);
        builder.Property(s => s.PrivacyPolicyText).HasMaxLength(DomainLimits.LegalTextMaxLength);
    }
}
