using LeadHub.Ports.Output;

namespace LeadHub.Infra;

public class GuidIdGenerator : IIdGenerator
{
    public string NewId() => Guid.NewGuid().ToString();
}
