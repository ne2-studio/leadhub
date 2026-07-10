namespace LeadHub.Infra.SpamAnalysis;

public class SpamScoringOptions
{
    public int SuspectedSpamThreshold { get; set; } = 10;
    public int SpamThreshold { get; set; } = 20;
}
