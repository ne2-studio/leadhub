using LeadHub.Ports.Output;

namespace LeadHub.Tests.Fakes;

public class StaticClock : IClock
{
    public DateTimeOffset UtcNow { get; } = new DateTimeOffset(2026, 7, 7, 8, 0, 0, TimeSpan.Zero);
}
