using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LeadHub.Ports.Input;
using LeadHub.Ports.Output;

namespace LeadHub.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/admin")]
public class AdminSubmissionsController(IAdminSubmissions adminSubmissions) : ControllerBase
{
    [HttpGet("forms/{formId}/submissions")]
    public async Task<IActionResult> ListByForm(
        string formId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, [FromQuery] SubmissionStatus? status = null)
    {
        if (User.GetUserId() is not { } ownerId)
            return this.UnauthorizedResult();

        var result = await adminSubmissions.ListSubmissions(new ListSubmissionsInput(ownerId, formId, page, pageSize, status));
        return this.ToActionResult(result);
    }

    [HttpGet("submissions/{submissionId}")]
    public async Task<IActionResult> Get(string submissionId)
    {
        if (User.GetUserId() is not { } ownerId)
            return this.UnauthorizedResult();

        var result = await adminSubmissions.GetSubmission(new GetSubmissionInput(ownerId, submissionId));
        return this.ToActionResult(result);
    }
}
