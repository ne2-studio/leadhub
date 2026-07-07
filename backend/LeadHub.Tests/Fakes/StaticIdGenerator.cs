using LeadHub.Ports.Output;

namespace LeadHub.Tests.Fakes;

public class StaticIdGenerator(string id) : IIdGenerator
{
    public string NewId() => id;
}
