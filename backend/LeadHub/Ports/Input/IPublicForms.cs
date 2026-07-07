using CSharpFunctionalExtensions;

namespace LeadHub.Ports.Input;

/// <summary>
/// Use cases available to external websites submitting form data.
/// </summary>
public interface IPublicForms
{
    Task<Result<SubmitFormOutput, Error>> SubmitForm(SubmitFormInput input);
}

public sealed record SubmitFormInput(
    string FormSlug,
    IReadOnlyDictionary<string, object?> Payload,
    string? IpAddress,
    string? UserAgent
);

public sealed record SubmitFormOutput(bool Accepted, string? RedirectUrl);
