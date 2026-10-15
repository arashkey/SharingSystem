using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SharingSystem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    AppName = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    AppUrl = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    ShowHomePage = table.Column<bool>(type: "INTEGER", nullable: false),
                    SessionDurationHours = table.Column<int>(type: "INTEGER", nullable: false),
                    AllowRegistration = table.Column<bool>(type: "INTEGER", nullable: false),
                    AllowUnauthenticatedShares = table.Column<bool>(type: "INTEGER", nullable: false),
                    MaxShareSizeBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    MaxExpirationHours = table.Column<int>(type: "INTEGER", nullable: false),
                    ZipCompression = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    ChunkSizeBytes = table.Column<int>(type: "INTEGER", nullable: false),
                    EnableShareEmailRecipients = table.Column<bool>(type: "INTEGER", nullable: false),
                    ShareRecipientsSubject = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    ShareRecipientsMessage = table.Column<string>(type: "TEXT", maxLength: 4096, nullable: false),
                    ReverseShareSubject = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    ReverseShareMessage = table.Column<string>(type: "TEXT", maxLength: 4096, nullable: false),
                    ResetPasswordSubject = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    ResetPasswordMessage = table.Column<string>(type: "TEXT", maxLength: 4096, nullable: false),
                    InviteSubject = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    InviteMessage = table.Column<string>(type: "TEXT", maxLength: 4096, nullable: false),
                    SmtpEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    SmtpHost = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    SmtpPort = table.Column<int>(type: "INTEGER", nullable: false),
                    SmtpFromEmail = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    SmtpUsername = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    SmtpPasswordEncrypted = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
                    SmtpAllowUnauthorizedCertificates = table.Column<bool>(type: "INTEGER", nullable: false),
                    LegalEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    ImprintText = table.Column<string>(type: "TEXT", maxLength: 20000, nullable: true),
                    PrivacyPolicyText = table.Column<string>(type: "TEXT", maxLength: 20000, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Username = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    IsAdmin = table.Column<bool>(type: "INTEGER", nullable: false),
                    SecurityStamp = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    TotpSecretEncrypted = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
                    TotpEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PasswordResetTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TokenHash = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasswordResetTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PasswordResetTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReverseShares",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Token = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    CreatorId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    MaxShareSizeBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    RemainingUses = table.Column<int>(type: "INTEGER", nullable: false),
                    SendEmailNotification = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsSimplified = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsPublicAccess = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReverseShares", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReverseShares_Users_CreatorId",
                        column: x => x.CreatorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Shares",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
                    PasswordHash = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    MaxViews = table.Column<int>(type: "INTEGER", nullable: true),
                    Views = table.Column<int>(type: "INTEGER", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsCompleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    UploadTokenHash = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    RecipientEmails = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: true),
                    CreatorId = table.Column<Guid>(type: "TEXT", nullable: true),
                    IsCreatorOnly = table.Column<bool>(type: "INTEGER", nullable: false),
                    ReverseShareId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Shares", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Shares_ReverseShares_ReverseShareId",
                        column: x => x.ReverseShareId,
                        principalTable: "ReverseShares",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Shares_Users_CreatorId",
                        column: x => x.CreatorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ShareFiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ShareId = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Size = table.Column<long>(type: "INTEGER", nullable: false),
                    ContentType = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShareFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShareFiles_Shares_ShareId",
                        column: x => x.ShareId,
                        principalTable: "Shares",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetTokens_TokenHash",
                table: "PasswordResetTokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetTokens_UserId",
                table: "PasswordResetTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReverseShares_CreatorId",
                table: "ReverseShares",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_ReverseShares_Token",
                table: "ReverseShares",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShareFiles_ShareId",
                table: "ShareFiles",
                column: "ShareId");

            migrationBuilder.CreateIndex(
                name: "IX_Shares_CreatorId",
                table: "Shares",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_Shares_ExpiresAt",
                table: "Shares",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_Shares_ReverseShareId",
                table: "Shares",
                column: "ReverseShareId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppSettings");

            migrationBuilder.DropTable(
                name: "PasswordResetTokens");

            migrationBuilder.DropTable(
                name: "ShareFiles");

            migrationBuilder.DropTable(
                name: "Shares");

            migrationBuilder.DropTable(
                name: "ReverseShares");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
