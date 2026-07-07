using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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

        services.AddScoped<ISpamProtection, HoneypotSpamProtection>();

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
