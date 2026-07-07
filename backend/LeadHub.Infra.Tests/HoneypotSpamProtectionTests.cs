using LeadHub.Ports.Output;

namespace LeadHub.Infra.Tests;

public class HoneypotSpamProtectionTests
{
    private readonly HoneypotSpamProtection _spamProtection = new();

    [Fact]
    public async Task EnsureNotSpam_ShouldSucceed_WhenHoneypotIsAbsent()
    {
        var input = new SpamCheckInput("contact", new Dictionary<string, object?> { ["name"] = "Pedro" }, "1.2.3.4", "agent");

        var result = await _spamProtection.EnsureNotSpam(input);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task EnsureNotSpam_ShouldSucceed_WhenHoneypotIsEmpty()
    {
        var input = new SpamCheckInput(
            "contact", new Dictionary<string, object?> { ["name"] = "Pedro", ["_honeypot"] = "" }, "1.2.3.4", "agent");

        var result = await _spamProtection.EnsureNotSpam(input);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task EnsureNotSpam_ShouldFail_WhenHoneypotHasValue()
    {
        var input = new SpamCheckInput(
            "contact", new Dictionary<string, object?> { ["name"] = "Pedro", ["_honeypot"] = "bot-value" }, "1.2.3.4", "agent");

        var result = await _spamProtection.EnsureNotSpam(input);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCode.SpamDetected, result.Error.Code);
    }
}
