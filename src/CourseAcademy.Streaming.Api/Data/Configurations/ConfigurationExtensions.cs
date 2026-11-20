using CourseAcademy.Streaming.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CourseAcademy.Streaming.Api.Data.Configurations;

internal static class ConfigurationExtensions
{
    /// <summary>Table named after the entity, int key, and database defaults for the audit columns.</summary>
    public static EntityTypeBuilder<T> ConfigureBase<T>(this EntityTypeBuilder<T> builder) where T : BaseEntity
    {
        builder.ToTable(typeof(T).Name);
        builder.HasKey(e => e.Id);
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("sysutcdatetime()");
        builder.Property(e => e.IsDeleted).HasDefaultValue(false);
        return builder;
    }

    /// <summary>SQL for "column holds one of these codes", used in check constraints.</summary>
    public static string In(string column, IEnumerable<string> codes) =>
        $"[{column}] IN ({string.Join(", ", codes.Select(c => $"'{c}'"))})";
}
