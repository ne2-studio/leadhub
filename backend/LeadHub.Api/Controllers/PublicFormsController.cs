using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using LeadHub.Api.Models;
using LeadHub.Infra;
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
        using var reader = new StreamReader(Request.Body);
        var rawBody = await reader.ReadToEndAsync();

        var payload = JsonPayloadSerializer.TryParseObject(rawBody);
        if (payload == null)
        {
            var error = Errors.InvalidSubmissionPayload("Request body must be a JSON object.");
            var (statusCode, code) = ErrorCodeMapper.Map(error.Code);
            return StatusCode(statusCode, ApiResponse<object?>.Fail(new ApiError(code, error.Message)));
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers.UserAgent.ToString();

        var result = await publicForms.SubmitForm(new SubmitFormInput(formSlug, payload, ipAddress, userAgent));
        return this.ToActionResult(result);
    }
}
