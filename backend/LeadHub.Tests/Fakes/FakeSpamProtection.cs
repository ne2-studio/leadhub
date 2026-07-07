using CSharpFunctionalExtensions;
using LeadHub.Ports.Output;

namespace LeadHub.Tests.Fakes;

public class FakeSpamProtection : ISpamProtection
{
    public bool IsSpam { get; set; }

    public Task<UnitResult<Error>> EnsureNotSpam(SpamCheckInput input) =>
        Task.FromResult(IsSpam ? UnitResult.Failure(Errors.SpamDetected()) : UnitResult.Success<Error>());
}
