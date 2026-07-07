namespace LeadHub.Ports.Output;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
