using core.jarvis.tests.Components;

namespace core.jarvis.tests.Helpers;

/// <summary>
/// Builder for creating test components with fluent API.
/// </summary>
public class TestBuilder
{
    private readonly TestComponent _component = new();

    public TestBuilder WithEntityId(Guid entityId)
    {
        _component.OwnerEntityId = entityId;
        return this;
    }

    public TestBuilder WithName(string name)
    {
        _component.Name = name;
        return this;
    }

    public TestBuilder WithValue(int value)
    {
        _component.Value = value;
        return this;
    }

    public TestBuilder WithStatus(string status)
    {
        _component.Status = status;
        return this;
    }

    public TestBuilder AsActive() => WithStatus("ACTIVE");
    public TestBuilder AsInactive() => WithStatus("INACTIVE");

    public TestComponent Build() => _component;

    // Static factory methods
    public static TestComponent CreateActive(Guid entityId, string name = "Test")
        => new TestBuilder()
            .WithEntityId(entityId)
            .WithName(name)
            .AsActive()
            .Build();

    public static TestComponent CreateInactive(Guid entityId, string name = "Test")
        => new TestBuilder()
            .WithEntityId(entityId)
            .WithName(name)
            .AsInactive()
            .Build();
}