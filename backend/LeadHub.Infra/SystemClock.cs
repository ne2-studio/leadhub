using LeadHub.Ports.Output;

namespace LeadHub.Infra;

public class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
