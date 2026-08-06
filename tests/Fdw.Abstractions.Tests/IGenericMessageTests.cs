using Fdw.Messages;

namespace Fdw.Abstractions.Tests;

/// <summary>
/// Tests for IGenericMessage interface contract.
/// </summary>
public class IGenericMessageTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void IGenericMessageInterfaceExists()
    {
        // Assert - Verify interface can be found
        var type = typeof(IGenericMessage);
        type.ShouldNotBeNull();
        type.IsInterface.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void IGenericMessageHasMessageProperty()
    {
        // Assert
        var type = typeof(IGenericMessage);
        var property = type.GetProperty("Message");
        property.ShouldNotBeNull();
        property!.PropertyType.ShouldBe(typeof(string));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void IGenericMessageHasCodeProperty()
    {
        // Assert
        var type = typeof(IGenericMessage);
        var property = type.GetProperty("Code");
        property.ShouldNotBeNull();
        property!.PropertyType.ShouldBe(typeof(string));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void IGenericMessageHasSourceProperty()
    {
        // Assert
        var type = typeof(IGenericMessage);
        var property = type.GetProperty("Source");
        property.ShouldNotBeNull();
        property!.PropertyType.ShouldBe(typeof(string));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void IGenericMessageGenericInterfaceExists()
    {
        // Assert - Verify generic interface can be found
        var type = typeof(IGenericMessage<>);
        type.ShouldNotBeNull();
        type.IsInterface.ShouldBeTrue();
        type.IsGenericTypeDefinition.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void IGenericMessageGenericHasSeverityProperty()
    {
        // Assert
        var type = typeof(IGenericMessage<>);
        var property = type.GetProperty("Severity");
        property.ShouldNotBeNull();
    }
}
