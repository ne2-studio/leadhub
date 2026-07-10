using System.Text.Json;
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
            INSERT INTO "Submissions" ("Id", "FormId", "CreatedAt", "IpAddress", "UserAgent", "Payload", "Status", "SpamScore", "SpamReasons")
            VALUES (@Id, @FormId, @CreatedAt, @IpAddress, @UserAgent, @Payload::jsonb, @Status, @SpamScore, @SpamReasons::jsonb)
            """, new
            {
                submission.Id,
                submission.FormId,
                submission.CreatedAt,
                submission.IpAddress,
                submission.UserAgent,
                Payload = JsonPayloadSerializer.Serialize(submission.Payload),
                Status = submission.Status.ToString(),
                submission.SpamScore,
                SpamReasons = JsonSerializer.Serialize(submission.SpamReasons)
            });
    }

    public async Task<Submission?> GetById(string id)
    {
        using var connection = new NpgsqlConnection(connectionString);
        var row = await connection.QuerySingleOrDefaultAsync<SubmissionRow>(
            """
            SELECT "Id", "FormId", "CreatedAt", "IpAddress", "UserAgent", "Payload"::text AS "Payload",
                   "Status", "SpamScore", "SpamReasons"::text AS "SpamReasons"
            FROM "Submissions"
            WHERE "Id" = @Id
            """, new { Id = id });

        return row?.ToDomain();
    }

    public async Task<PaginatedResult<SubmissionSummaryProjection>> ListByForm(string formId, int page, int pageSize, SubmissionStatus? status)
    {
        using var connection = new NpgsqlConnection(connectionString);

        var statusFilter = status?.ToString();

        var total = await connection.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(1) FROM "Submissions" WHERE "FormId" = @FormId AND (@Status IS NULL OR "Status" = @Status)
            """, new { FormId = formId, Status = statusFilter });

        var rows = await connection.QueryAsync<SubmissionRow>(
            """
            SELECT "Id", "FormId", "CreatedAt", "IpAddress", "UserAgent", "Payload"::text AS "Payload",
                   "Status", "SpamScore", "SpamReasons"::text AS "SpamReasons"
            FROM "Submissions"
            WHERE "FormId" = @FormId AND (@Status IS NULL OR "Status" = @Status)
            ORDER BY "CreatedAt" DESC
            LIMIT @Take OFFSET @Skip
            """, new { FormId = formId, Status = statusFilter, Take = pageSize, Skip = (page - 1) * pageSize });

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

    public async Task UpdateAnalysis(string submissionId, SpamVerdict verdict)
    {
        using var connection = new NpgsqlConnection(connectionString);
        await connection.ExecuteAsync(
            """
            UPDATE "Submissions"
            SET "Status" = @Status, "SpamScore" = @Score, "SpamReasons" = @Reasons::jsonb
            WHERE "Id" = @Id
            """, new
            {
                Id = submissionId,
                Status = verdict.Status.ToString(),
                verdict.Score,
                Reasons = JsonSerializer.Serialize(verdict.Reasons)
            });
    }

    // Npgsql maps "timestamp with time zone" columns to DateTime (Kind=Utc) by default, not
    // DateTimeOffset — Dapper's record constructor-matching requires the row type to line up
    // exactly with that, so the conversion to DateTimeOffset happens explicitly below.
    private sealed record SubmissionRow(
        string Id, string FormId, DateTime CreatedAt, string? IpAddress, string? UserAgent, string Payload,
        string Status, int SpamScore, string SpamReasons)
    {
        public Submission ToDomain() =>
            new(Id, FormId, AsUtc(CreatedAt), IpAddress, UserAgent, JsonPayloadSerializer.Deserialize(Payload),
                Enum.Parse<SubmissionStatus>(Status), SpamScore, DeserializeReasons(SpamReasons));

        public SubmissionSummaryProjection ToSummary() =>
            new(Id, AsUtc(CreatedAt), IpAddress, SubmissionPreviewBuilder.Build(JsonPayloadSerializer.Deserialize(Payload)),
                Enum.Parse<SubmissionStatus>(Status));

        private static DateTimeOffset AsUtc(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));

        private static IReadOnlyList<string> DeserializeReasons(string json) =>
            JsonSerializer.Deserialize<List<string>>(json) ?? [];
    }
}
