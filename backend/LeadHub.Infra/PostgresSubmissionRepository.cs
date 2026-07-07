using Dapper;
using Npgsql;
using LeadHub.Ports.Output;

namespace LeadHub.Infra;

public class PostgresSubmissionRepository(string connectionString) : ISubmissionRepository
{
    public async Task Save(Submission submission)
    {
        using var connection = new NpgsqlConnection(connectionString);
        await connection.ExecuteAsync(
            """
            INSERT INTO "Submissions" ("Id", "FormId", "CreatedAt", "IpAddress", "UserAgent", "Payload")
            VALUES (@Id, @FormId, @CreatedAt, @IpAddress, @UserAgent, @Payload::jsonb)
            """, new
            {
                submission.Id,
                submission.FormId,
                submission.CreatedAt,
                submission.IpAddress,
                submission.UserAgent,
                Payload = JsonPayloadSerializer.Serialize(submission.Payload)
            });
    }

    public async Task<Submission?> GetById(string id)
    {
        using var connection = new NpgsqlConnection(connectionString);
        var row = await connection.QuerySingleOrDefaultAsync<SubmissionRow>(
            """
            SELECT "Id", "FormId", "CreatedAt", "IpAddress", "UserAgent", "Payload"::text AS "Payload"
            FROM "Submissions"
            WHERE "Id" = @Id
            """, new { Id = id });

        return row?.ToDomain();
    }

    public async Task<PaginatedResult<SubmissionSummaryProjection>> ListByForm(string formId, int page, int pageSize)
    {
        using var connection = new NpgsqlConnection(connectionString);

        var total = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM \"Submissions\" WHERE \"FormId\" = @FormId", new { FormId = formId });

        var rows = await connection.QueryAsync<SubmissionRow>(
            """
            SELECT "Id", "FormId", "CreatedAt", "IpAddress", "UserAgent", "Payload"::text AS "Payload"
            FROM "Submissions"
            WHERE "FormId" = @FormId
            ORDER BY "CreatedAt" DESC
            LIMIT @Take OFFSET @Skip
            """, new { FormId = formId, Take = pageSize, Skip = (page - 1) * pageSize });

        var items = rows.Select(r => r.ToSummary()).ToList();
        return new PaginatedResult<SubmissionSummaryProjection>(items, page, pageSize, total);
    }

    public async Task<int> CountByForm(string formId)
    {
        using var connection = new NpgsqlConnection(connectionString);
        return await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM \"Submissions\" WHERE \"FormId\" = @FormId", new { FormId = formId });
    }

    public async Task<DateTimeOffset?> GetLastSubmissionDate(string formId)
    {
        using var connection = new NpgsqlConnection(connectionString);
        var lastSubmissionAt = await connection.ExecuteScalarAsync<DateTime?>(
            "SELECT MAX(\"CreatedAt\") FROM \"Submissions\" WHERE \"FormId\" = @FormId", new { FormId = formId });

        return lastSubmissionAt.HasValue ? new DateTimeOffset(DateTime.SpecifyKind(lastSubmissionAt.Value, DateTimeKind.Utc)) : null;
    }

    // Npgsql maps "timestamp with time zone" columns to DateTime (Kind=Utc) by default, not
    // DateTimeOffset — Dapper's record constructor-matching requires the row type to line up
    // exactly with that, so the conversion to DateTimeOffset happens explicitly below.
    private sealed record SubmissionRow(string Id, string FormId, DateTime CreatedAt, string? IpAddress, string? UserAgent, string Payload)
    {
        public Submission ToDomain() =>
            new(Id, FormId, AsUtc(CreatedAt), IpAddress, UserAgent, JsonPayloadSerializer.Deserialize(Payload));

        public SubmissionSummaryProjection ToSummary() =>
            new(Id, AsUtc(CreatedAt), IpAddress, SubmissionPreviewBuilder.Build(JsonPayloadSerializer.Deserialize(Payload)));

        private static DateTimeOffset AsUtc(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));
    }
}
