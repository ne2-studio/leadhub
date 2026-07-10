using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using LeadHub.Ports.Input;

namespace LeadHub.Infra;

/// <summary>
/// Drains <see cref="SubmissionAnalysisQueue"/> and runs <see cref="ISubmissionAnalysis"/> for
/// each submission. Failures (a thrown exception, or an <c>ISubmissionAnalysis</c> failure result
/// such as a notification send failing) are retried with backoff, up to a configured attempt
/// limit, before being given up on and logged. Each attempt runs in its own DI scope because
/// application/repository services are Scoped, not Singleton.
/// </summary>
public class SubmissionAnalysisBackgroundService(
    SubmissionAnalysisQueue queue,
    IServiceScopeFactory scopeFactory,
    IOptions<SubmissionAnalysisOptions> options,
    ILogger<SubmissionAnalysisBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var submissionId in queue.Reader(stoppingToken))
            await ProcessWithRetry(submissionId, stoppingToken);
    }

    private async Task ProcessWithRetry(string submissionId, CancellationToken stoppingToken)
    {
        var maxAttempts = options.Value.MaxAttempts;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var analysis = scope.ServiceProvider.GetRequiredService<ISubmissionAnalysis>();
                var result = await analysis.AnalyzeSubmission(new AnalyzeSubmissionInput(submissionId));

                if (result.IsSuccess)
                    return;

                logger.LogWarning(
                    "Submission analysis failed for {SubmissionId} on attempt {Attempt}/{MaxAttempts}: {Error}",
                    submissionId, attempt, maxAttempts, result.Error.Message);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex,
                    "Submission analysis threw for {SubmissionId} on attempt {Attempt}/{MaxAttempts}",
                    submissionId, attempt, maxAttempts);
            }

            if (attempt == maxAttempts)
            {
                logger.LogError(
                    "Submission analysis permanently failed for {SubmissionId} after {MaxAttempts} attempts",
                    submissionId, maxAttempts);
                return;
            }

            var delay = TimeSpan.FromMilliseconds(options.Value.InitialRetryDelayMs * Math.Pow(2, attempt - 1));
            await Task.Delay(delay, stoppingToken);
        }
    }
}
