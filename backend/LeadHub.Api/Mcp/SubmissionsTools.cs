using System.ComponentModel;
using LeadHub.Ports.Input;
using LeadHub.Ports.Output;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace LeadHub.Api.Mcp;

[McpServerToolType]
public static class SubmissionsTools
{
    [McpServerTool(Name = "list_submissions", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("Lista los envíos de un formulario, del más reciente al más antiguo, con un resumen de cada uno. " +
                 "Estados: PendingReview (sin analizar), Ham (legítimo), SuspectedSpam (dudoso), Spam. " +
                 "Equivale a GET /api/admin/forms/{formId}/submissions.")]
    public static async Task<PaginatedResult<SubmissionSummary>> ListSubmissions(
        RequestContext<CallToolRequestParams> context,
        IAdminSubmissions submissions,
        [Description("Id del formulario (obtenido con list_forms).")] string formId,
        [Description("Filtra por estado; si se omite, devuelve todos.")] SubmissionStatus? status = null,
        [Description("Página, empezando en 1.")] int page = 1,
        [Description("Envíos por página.")] int pageSize = 50) =>
        (await submissions.ListSubmissions(new ListSubmissionsInput(context.OwnerId(), formId, page, pageSize, status))).OrThrow();

    [McpServerTool(Name = "get_submission", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("Devuelve un envío completo: campos enviados, IP, user agent, estado, puntuación de spam y motivos. " +
                 "Equivale a GET /api/admin/submissions/{submissionId}.")]
    public static async Task<SubmissionDetails> GetSubmission(
        RequestContext<CallToolRequestParams> context,
        IAdminSubmissions submissions,
        [Description("Id del envío (obtenido con list_submissions).")] string submissionId) =>
        (await submissions.GetSubmission(new GetSubmissionInput(context.OwnerId(), submissionId))).OrThrow();

    [McpServerTool(Name = "mark_submission", Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Marca un envío como spam (Spam) o como legítimo (Ham), corrigiendo al clasificador. Conserva la puntuación y los motivos, " +
                 "y añade 'marked_by_admin'. No envía el aviso por email. Equivale a PUT /api/admin/submissions/{submissionId}/spam.")]
    public static async Task<SubmissionDetails> MarkSubmission(
        RequestContext<CallToolRequestParams> context,
        IAdminSubmissions submissions,
        [Description("Id del envío (obtenido con list_submissions).")] string submissionId,
        [Description("true para marcarlo como spam; false para marcarlo como legítimo.")] bool isSpam) =>
        (await submissions.MarkSubmission(new MarkSubmissionInput(context.OwnerId(), submissionId, isSpam))).OrThrow();
}
