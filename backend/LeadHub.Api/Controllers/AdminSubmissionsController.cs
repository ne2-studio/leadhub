using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LeadHub.Ports.Input;

namespace LeadHub.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/admin")]
public class AdminSubmissionsController(IAdminSubmissions adminSubmissions) : ControllerBase
{
    [HttpGet("forms/{formId}/submissions")]
    public async Task<IActionResult> ListByForm(string formId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var result = await adminSubmissions.ListSubmissions(new ListSubmissionsInput(formId, page, pageSize));
        return this.ToActionResult(result);
    }

    [HttpGet("submissions/{submissionId}")]
    public async Task<IActionResult> Get(string submissionId)
    {
        var result = await adminSubmissions.GetSubmission(new GetSubmissionInput(submissionId));
        return this.ToActionResult(result);
    }
}
