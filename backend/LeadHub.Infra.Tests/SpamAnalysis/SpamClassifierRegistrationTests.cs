using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using LeadHub.Infra.SpamAnalysis;

namespace LeadHub.Infra.Tests.SpamAnalysis;

public class SpamClassifierRegistrationTests
{
    private static ServiceProvider Build(Dictionary<string, string?> settings)
    {
        settings["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=unused";
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        return new ServiceCollection().AddInfrastructure(configuration).BuildServiceProvider();
    }

    [Fact]
    public void AddInfrastructure_ShouldNotRegisterClassifier_WhenFlagOff()
    {
        using var provider = Build(new Dictionary<string, string?>());
        using var scope = provider.CreateScope();

        Assert.Empty(scope.ServiceProvider.GetServices<IAsyncSpamRule>());
    }

    [Fact]
    public void AddInfrastructure_ShouldRegisterOpenRouterClassifier_UsingJevClassifierWithOpenRouterApiKeyEnvVar()
    {
        using var provider = Build(new Dictionary<string, string?>
        {
            ["Features:SpamClassifier:Enabled"] = "true",
            ["OPENROUTER_API_KEY"] = "sk-or-test"
        });
        using var scope = provider.CreateScope();

        Assert.IsType<ClassifierSpamRule>(Assert.Single(scope.ServiceProvider.GetServices<IAsyncSpamRule>()));
        var client = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(ISpamClassifier));
        Assert.Equal("Bearer sk-or-test", client.DefaultRequestHeaders.Authorization?.ToString());
        Assert.Equal(new Uri("https://openrouter.ai/api/"), client.BaseAddress);
        Assert.Equal(TimeSpan.FromSeconds(5), client.Timeout);
    }
}
