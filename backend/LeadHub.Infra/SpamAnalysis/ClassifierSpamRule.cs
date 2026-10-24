using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using LeadHub.Ports.Output;

namespace LeadHub.Infra.SpamAnalysis;

public class SpamClassifierOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://openrouter.ai/api/";
    public string Model { get; set; } = "typesafe/jev-1.14";
    public int TimeoutSeconds { get; set; } = 5;
    public int MaxInputChars { get; set; } = 4000;
    public double SuspectedSpamProbability { get; set; } = 0.5;
    public double SpamProbability { get; set; } = 0.85;
    public int SuspectedSpamPoints { get; set; } = 10;
    public int SpamPoints { get; set; } = 20;
}

/// <summary>
/// Turns an <see cref="ISpamClassifier"/> verdict into spam points. Fails open: if the classifier
/// can't answer, the lead keeps the status the local rules give it and is marked
/// <c>classifier_unavailable</c> (0 points) so admins can tell it wasn't model-checked.
/// </summary>
public class ClassifierSpamRule(
    ISpamClassifier classifier,
    IOptions<SpamClassifierOptions> options,
    ILogger<ClassifierSpamRule> logger) : IAsyncSpamRule
{
    public async Task<SpamRuleHit?> Evaluate(Submission submission, CancellationToken cancellationToken)
    {
        var fields = PayloadFields.VisibleTextFields(submission).ToList();
        if (fields.Count == 0)
            return null;

        var result = await classifier.Classify(fields, cancellationToken);
        if (result.IsFailure)
        {
            logger.LogWarning("SpamClassifier - Submission {SubmissionId} left unclassified: {Error}", submission.Id, result.Error);
            return new SpamRuleHit(0, "classifier_unavailable");
        }

        var settings = options.Value;
        var probability = result.Value.SpamProbability;
        logger.LogInformation("SpamClassifier - Submission {SubmissionId} spam probability {Probability}", submission.Id, probability);

        return probability switch
        {
            var p when p >= settings.SpamProbability => new SpamRuleHit(settings.SpamPoints, "classifier_spam"),
            var p when p >= settings.SuspectedSpamProbability => new SpamRuleHit(settings.SuspectedSpamPoints, "classifier_suspected_spam"),
            _ => null
        };
    }
}
