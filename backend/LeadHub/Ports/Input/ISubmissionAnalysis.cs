using CSharpFunctionalExtensions;

namespace LeadHub.Ports.Input;

/// <summary>
/// The background analysis job: scores a pending submission for spam and triggers notifications
/// when appropriate. Invoked by the queue worker, not by a controller.
/// </summary>
public interface ISubmissionAnalysis
{
    Task<UnitResult<Error>> AnalyzeSubmission(AnalyzeSubmissionInput input);
}

public sealed record AnalyzeSubmissionInput(string SubmissionId);
