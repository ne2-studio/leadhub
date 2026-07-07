using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LeadHub.Api.Models;
using LeadHub.Ports.Input;

namespace LeadHub.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/admin/forms")]
public class AdminFormsController(IAdminForms adminForms) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List()
    {
        var result = await adminForms.ListForms();
        return this.ToActionResult(result);
    }

    [HttpGet("{formId}")]
    public async Task<IActionResult> Get(string formId)
    {
        var result = await adminForms.GetForm(new GetFormInput(formId));
        return this.ToActionResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateFormRequest request)
    {
        var result = await adminForms.CreateForm(new CreateFormInput(
            request.Name, request.Slug, request.Description,
            request.NotificationsEnabled, request.NotificationEmail, request.ThankYouUrl));

        return this.ToActionResult(result, StatusCodes.Status201Created);
    }

    [HttpPut("{formId}")]
    public async Task<IActionResult> Update(string formId, [FromBody] UpdateFormRequest request)
    {
        var result = await adminForms.UpdateForm(new UpdateFormInput(
            formId, request.Name, request.Slug, request.Description,
            request.NotificationsEnabled, request.NotificationEmail, request.ThankYouUrl));

        return this.ToActionResult(result);
    }

    [HttpDelete("{formId}")]
    public async Task<IActionResult> Delete(string formId)
    {
        var result = await adminForms.DeleteForm(new DeleteFormInput(formId));
        return this.ToActionResult(result);
    }
}
