using Fdw.Data.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Data.Abstractions.Tests.Schema.CompatibilityModes;

public sealed class SchemaCompatibilityModesTests
{

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void BackwardExtensionPropertyWorks()
    {
        // Act
        var backward = SchemaCompatibilityModes.Backward;

        // Assert
        backward.ShouldNotBeNull();
        backward.Name.ShouldBe("Backward");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ForwardExtensionPropertyWorks()
    {
        // Act
        var forward = SchemaCompatibilityModes.Forward;

        // Assert
        forward.ShouldNotBeNull();
        forward.Name.ShouldBe("Forward");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ExactExtensionPropertyWorks()
    {
        // Act
        var exact = SchemaCompatibilityModes.Exact;

        // Assert
        exact.ShouldNotBeNull();
        exact.Name.ShouldBe("Exact");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void LooseExtensionPropertyWorks()
    {
        // Act
        var loose = SchemaCompatibilityModes.Loose;

        // Assert
        loose.ShouldNotBeNull();
        loose.Name.ShouldBe("Loose");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void StructuralExtensionPropertyWorks()
    {
        // Act
        var structural = SchemaCompatibilityModes.Structural;

        // Assert
        structural.ShouldNotBeNull();
        structural.Name.ShouldBe("Structural");
    }
}
