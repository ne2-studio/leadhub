namespace LeadHub.Infra;

public class SubmissionAnalysisOptions
{
    public int MaxAttempts { get; set; } = 3;
    public int InitialRetryDelayMs { get; set; } = 2000;
}
