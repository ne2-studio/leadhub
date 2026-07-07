using Dapper;
using Npgsql;
using LeadHub.Ports.Output;

namespace LeadHub.Infra;

public class PostgresFormRepository(string connectionString) : IFormRepository
{
    public async Task<Form?> GetById(string id)
    {
        using var connection = new NpgsqlConnection(connectionString);
        var row = await connection.QuerySingleOrDefaultAsync<FormRow>(
            """
            SELECT "Id", "Name", "Slug", "Description", "NotificationsEnabled", "NotificationEmail", "ThankYouUrl", "CreatedAt", "UpdatedAt"
            FROM "Forms"
            WHERE "Id" = @Id
            """, new { Id = id });

        return row?.ToDomain();
    }

    public async Task<Form?> GetBySlug(string slug)
    {
        using var connection = new NpgsqlConnection(connectionString);
        var row = await connection.QuerySingleOrDefaultAsync<FormRow>(
            """
            SELECT "Id", "Name", "Slug", "Description", "NotificationsEnabled", "NotificationEmail", "ThankYouUrl", "CreatedAt", "UpdatedAt"
            FROM "Forms"
            WHERE "Slug" = @Slug
            """, new { Slug = slug });

        return row?.ToDomain();
    }

    public async Task<bool> ExistsBySlug(string slug, string? excludingFormId = null)
    {
        using var connection = new NpgsqlConnection(connectionString);
        var count = await connection.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(1) FROM "Forms"
            WHERE "Slug" = @Slug AND (@ExcludingFormId IS NULL OR "Id" <> @ExcludingFormId)
            """, new { Slug = slug, ExcludingFormId = excludingFormId });

        return count > 0;
    }

    public async Task<IReadOnlyList<FormSummaryProjection>> List()
    {
        using var connection = new NpgsqlConnection(connectionString);
        var rows = await connection.QueryAsync<FormSummaryRow>(
            """
            SELECT
                f."Id", f."Name", f."Slug", f."NotificationsEnabled",
                COUNT(s."Id")::int AS "SubmissionCount",
                MAX(s."CreatedAt") AS "LastSubmissionAt"
            FROM "Forms" f
            LEFT JOIN "Submissions" s ON s."FormId" = f."Id"
            GROUP BY f."Id", f."Name", f."Slug", f."NotificationsEnabled"
            ORDER BY f."Name"
            """);

        return rows
            .Select(r => new FormSummaryProjection(
                r.Id, r.Name, r.Slug, r.NotificationsEnabled, r.SubmissionCount,
                r.LastSubmissionAt.HasValue ? new DateTimeOffset(DateTime.SpecifyKind(r.LastSubmissionAt.Value, DateTimeKind.Utc)) : null))
            .ToList();
    }

    public async Task Save(Form form)
    {
        using var connection = new NpgsqlConnection(connectionString);
        await connection.ExecuteAsync(
            """
            INSERT INTO "Forms" ("Id", "Name", "Slug", "Description", "NotificationsEnabled", "NotificationEmail", "ThankYouUrl", "CreatedAt", "UpdatedAt")
            VALUES (@Id, @Name, @Slug, @Description, @NotificationsEnabled, @NotificationEmail, @ThankYouUrl, @CreatedAt, @UpdatedAt)
            ON CONFLICT ("Id") DO UPDATE SET
                "Name" = EXCLUDED."Name",
                "Slug" = EXCLUDED."Slug",
                "Description" = EXCLUDED."Description",
                "NotificationsEnabled" = EXCLUDED."NotificationsEnabled",
                "NotificationEmail" = EXCLUDED."NotificationEmail",
                "ThankYouUrl" = EXCLUDED."ThankYouUrl",
                "UpdatedAt" = EXCLUDED."UpdatedAt"
            """, form);
    }

    public async Task Delete(Form form)
    {
        using var connection = new NpgsqlConnection(connectionString);
        await connection.ExecuteAsync("DELETE FROM \"Forms\" WHERE \"Id\" = @Id", new { form.Id });
    }

    // Npgsql maps "timestamp with time zone" columns to DateTime (Kind=Utc) by default, not
    // DateTimeOffset — Dapper's record constructor-matching requires the row type to line up
    // exactly with that, so the conversion to DateTimeOffset happens explicitly in ToDomain().
    private sealed record FormRow(
        string Id,
        string Name,
        string Slug,
        string? Description,
        bool NotificationsEnabled,
        string? NotificationEmail,
        string? ThankYouUrl,
        DateTime CreatedAt,
        DateTime? UpdatedAt)
    {
        public Form ToDomain() =>
            new(Id, Name, Slug, Description, NotificationsEnabled, NotificationEmail, ThankYouUrl,
                AsUtc(CreatedAt), UpdatedAt.HasValue ? AsUtc(UpdatedAt.Value) : null);

        private static DateTimeOffset AsUtc(DateTime value) =>
            new(DateTime.SpecifyKind(value, DateTimeKind.Utc));
    }

    private sealed record FormSummaryRow(
        string Id, string Name, string Slug, bool NotificationsEnabled, int SubmissionCount, DateTime? LastSubmissionAt);
}
