using Fdw.Results;

namespace Fdw.Abstractions.Tests;

/// <summary>
/// Tests for IGenericResult interface contract.
/// </summary>
public class IGenericResultTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void IGenericResultInterfaceExists()
    {
        // Assert
        var type = typeof(IGenericResult);
        type.ShouldNotBeNull();
        type.IsInterface.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void IGenericResultHasRequiredProperties()
    {
        // Assert
        var type = typeof(IGenericResult);

        type.GetProperty("IsSuccess").ShouldNotBeNull();
        type.GetProperty("IsFailure").ShouldNotBeNull();
        type.GetProperty("Error").ShouldNotBeNull();
        type.GetProperty("IsEmpty").ShouldNotBeNull();
        type.GetProperty("CurrentMessage").ShouldNotBeNull();
        type.GetProperty("Messages").ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void IGenericResultHasCodeProperty()
    {
        // Assert
        var type = typeof(IGenericResult);
        var property = type.GetProperty("Code");
        property.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void IGenericResultHasDetailsProperty()
    {
        // Assert
        var type = typeof(IGenericResult);
        var property = type.GetProperty("Details");
        property.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void IGenericResultHasInnerResultProperty()
    {
        // Assert
        var type = typeof(IGenericResult);
        var property = type.GetProperty("InnerResult");
        property.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void IGenericResultHasCodeChainProperty()
    {
        // Assert
        var type = typeof(IGenericResult);
        var property = type.GetProperty("CodeChain");
        property.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void IGenericResultHasRootCauseProperty()
    {
        // Assert
        var type = typeof(IGenericResult);
        var property = type.GetProperty("RootCause");
        property.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void IGenericResultOfTGenericInterfaceExists()
    {
        // Assert
        var type = typeof(IGenericResult<>);
        type.ShouldNotBeNull();
        type.IsInterface.ShouldBeTrue();
        type.IsGenericTypeDefinition.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void IGenericResultOfTHasValueProperty()
    {
        // Assert
        var type = typeof(IGenericResult<>);

        var property = type.GetProperty("Value");
        property.ShouldNotBeNull();
    }
}
