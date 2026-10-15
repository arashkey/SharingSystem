using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace SharingSystem.Infrastructure.Persistence;

/// <summary>
/// SQLite does not keep <see cref="DateTimeKind"/>; this converter marks every value read from the database as UTC.
/// </summary>
public sealed class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
    value => value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime(),
    value => DateTime.SpecifyKind(value, DateTimeKind.Utc));
