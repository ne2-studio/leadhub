using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using LeadHub.Api.Models;
using LeadHub.Ports.Input;

namespace LeadHub.Api.Controllers;

[ApiController]
[Route("api/forms")]
[EnableRateLimiting("PublicLimiter")]
public class PublicFormsController(IPublicForms publicForms) : ControllerBase
{
    [HttpPost("{formSlug}/submit")]
    public async Task<IActionResult> Submit(string formSlug)
    {
        if (!Request.HasFormContentType)
        {
            var error = Errors.InvalidSubmissionPayload("Request body must be a form submission.");
            var (statusCode, code) = ErrorCodeMapper.Map(error.Code);
            return StatusCode(statusCode, ApiResponse<object?>.Fail(new ApiError(code, error.Message)));
        }

        var form = await Request.ReadFormAsync();
        var payload = form.ToDictionary(pair => pair.Key, pair => (object?)pair.Value.ToString());

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers.UserAgent.ToString();

        var result = await publicForms.SubmitForm(new SubmitFormInput(formSlug, payload, ipAddress, userAgent));

        if (result.IsSuccess && !string.IsNullOrWhiteSpace(result.Value.RedirectUrl))
            return Redirect(result.Value.RedirectUrl);

        return this.ToActionResult(result);
    }
}
