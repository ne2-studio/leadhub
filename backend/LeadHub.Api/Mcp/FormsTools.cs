using System.ComponentModel;
using LeadHub.Ports.Input;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace LeadHub.Api.Mcp;

[McpServerToolType]
public static class FormsTools
{
    private const string FormIdDescription = "Id del formulario (obtenido con list_forms).";

    [McpServerTool(Name = "list_forms", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("Lista los formularios del usuario con su número de envíos y la fecha del último. Equivale a GET /api/admin/forms.")]
    public static async Task<IReadOnlyList<FormSummary>> ListForms(RequestContext<CallToolRequestParams> context, IAdminForms forms) =>
        (await forms.ListForms(new ListFormsInput(context.OwnerId()))).OrThrow();

    [McpServerTool(Name = "get_form", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("Devuelve un formulario completo: descripción, avisos por email y thankYouUrl. Equivale a GET /api/admin/forms/{formId}.")]
    public static async Task<FormDetails> GetForm(
        RequestContext<CallToolRequestParams> context,
        IAdminForms forms,
        [Description(FormIdDescription)] string formId) =>
        (await forms.GetForm(new GetFormInput(context.OwnerId(), formId))).OrThrow();

    [McpServerTool(Name = "create_form", Destructive = false, OpenWorld = false)]
    [Description("Crea un formulario. La web lo envía con POST a /api/forms/{slug}/submit y, si tiene thankYouUrl, el visitante vuelve a esa URL. " +
                 "Equivale a POST /api/admin/forms. Devuelve el formulario creado.")]
    public static async Task<FormDetails> CreateForm(
        RequestContext<CallToolRequestParams> context,
        IAdminForms forms,
        [Description("Nombre visible, p. ej. 'Contacto'.")] string name,
        [Description("Identificador público y único en la URL de envío: minúsculas, números y guiones, p. ej. 'contacto'.")] string slug,
        [Description("Descripción interna.")] string? description = null,
        [Description("Envía un email por cada envío legítimo (no spam). Exige notificationEmail.")] bool notificationsEnabled = false,
        [Description("Destinatario de los avisos.")] string? notificationEmail = null,
        [Description("URL absoluta a la que vuelve el visitante tras enviar el formulario.")] string? thankYouUrl = null) =>
        (await forms.CreateForm(new CreateFormInput(
            context.OwnerId(), name, slug, description, notificationsEnabled, notificationEmail, thankYouUrl))).OrThrow();

    [McpServerTool(Name = "update_form", Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Modifica un formulario. Solo cambian los campos indicados; en description, notificationEmail y thankYouUrl, " +
                 "una cadena vacía borra el valor. Equivale a PUT /api/admin/forms/{formId} con el resto de campos sin cambios.")]
    public static async Task<FormDetails> UpdateForm(
        RequestContext<CallToolRequestParams> context,
        IAdminForms forms,
        [Description(FormIdDescription)] string formId,
        [Description("Nuevo nombre.")] string? name = null,
        [Description("Nuevo slug. Cambiarlo rompe las webs que envían al slug antiguo.")] string? slug = null,
        [Description("Nueva descripción; vacía para borrarla.")] string? description = null,
        [Description("Activa o desactiva los avisos por email.")] bool? notificationsEnabled = null,
        [Description("Nuevo destinatario de los avisos; vacío para borrarlo.")] string? notificationEmail = null,
        [Description("Nueva URL de agradecimiento; vacía para borrarla.")] string? thankYouUrl = null)
    {
        var ownerId = context.OwnerId();
        var current = (await forms.GetForm(new GetFormInput(ownerId, formId))).OrThrow();

        return (await forms.UpdateForm(new UpdateFormInput(
            ownerId,
            formId,
            name ?? current.Name,
            slug ?? current.Slug,
            Patch(description, current.Description),
            notificationsEnabled ?? current.NotificationsEnabled,
            Patch(notificationEmail, current.NotificationEmail),
            Patch(thankYouUrl, current.ThankYouUrl)))).OrThrow();
    }

    [McpServerTool(Name = "delete_form", Destructive = true, Idempotent = true, OpenWorld = false)]
    [Description("Elimina un formulario y deja de aceptar envíos en su slug. Equivale a DELETE /api/admin/forms/{formId}.")]
    public static async Task<object> DeleteForm(
        RequestContext<CallToolRequestParams> context,
        IAdminForms forms,
        [Description(FormIdDescription)] string formId)
    {
        (await forms.DeleteForm(new DeleteFormInput(context.OwnerId(), formId))).OrThrow();
        return new { success = true };
    }

    /// <summary>Omitted keeps the current value; empty clears it.</summary>
    private static string? Patch(string? value, string? current) =>
        value is null ? current : value.Length == 0 ? null : value;
}
