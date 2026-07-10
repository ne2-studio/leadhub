using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using LeadHub.Infra.SpamAnalysis;
using LeadHub.Ports.Output;

namespace LeadHub.Infra;

public static class ServiceRegistration
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDatabase(configuration);

        var connectionString = configuration.GetConnectionString("DefaultConnection")!;

        // Singleton: the caching decorator holds state (in-memory dictionaries) that must survive
        // across requests to be useful — a deliberate exception to the default Scoped lifetime.
        services.AddSingleton<IFormRepository>(_ => new CachedFormRepository(new PostgresFormRepository(connectionString)));
        services.AddScoped<ISubmissionRepository>(_ => new PostgresSubmissionRepository(connectionString));

        services.AddScoped<IIdGenerator, GuidIdGenerator>();
        services.AddScoped<IClock, SystemClock>();

        // Spam analysis: each ISpamRule contributes an independent score/reason; adding a new rule
        // is a new class + one more registration line here, never a change to an existing rule.
        services.Configure<SpamScoringOptions>(configuration.GetSection("SpamScoring"));
        services.AddScoped<ISpamRule, HoneypotFilledRule>();
        services.AddScoped<ISpamRule, MessageContainsUrlRule>();
        services.AddScoped<ISpamRule, SuspiciousKeywordRule>();
        services.AddScoped<ISpamRule, SuspiciousNamePatternRule>();
        services.AddScoped<ISpamRule, MessageTooLongRule>();
        services.AddScoped<ISpamAnalyzer, RuleBasedSpamAnalyzer>();

        // Singleton: the channel backing the queue must survive across requests; consumed by the
        // hosted service registered below, which resolves Scoped services per job via a new scope.
        services.AddSingleton<SubmissionAnalysisQueue>();
        services.AddSingleton<ISubmissionAnalysisQueue>(sp => sp.GetRequiredService<SubmissionAnalysisQueue>());
        services.Configure<SubmissionAnalysisOptions>(configuration.GetSection("SubmissionAnalysis:Retry"));
        services.AddHostedService<SubmissionAnalysisBackgroundService>();

        services.Configure<RateLimiterOptions>(configuration.GetSection("RateLimiter:PublicSubmit"));
        // Singleton: submission counters must survive across requests to enforce a rolling window —
        // another deliberate exception to the default Scoped lifetime.
        services.AddSingleton<IRateLimiter, InMemoryRateLimiter>();

        // Null Object pattern: the feature flag is read once, here, at the composition root.
        // Use-case code depends only on IEmailNotificationSender and never checks the flag itself.
        services.Configure<ResendOptions>(configuration.GetSection("Resend"));
        var emailNotificationsEnabled = configuration.GetValue<bool>("Features:EmailNotifications:Enabled");
        if (emailNotificationsEnabled)
        {
            services.AddHttpClient<IEmailNotificationSender, ResendEmailNotificationSender>(client =>
            {
                client.BaseAddress = new Uri("https://api.resend.com/");
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", configuration["Resend:ApiKey"]);
            });
        }
        else
        {
            services.AddScoped<IEmailNotificationSender, NullEmailNotificationSender>();
        }

        return services;
    }
}
