using Fdw.Data.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Data.Abstractions.Tests.Schema.CompatibilityModes;

public sealed class SchemaCompatibilityModesTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void BackwardModeIsRegistered()
    {
        // Act
        var backward = SchemaCompatibilityModes.ByName("Backward");

        // Assert
        backward.ShouldNotBeNull();
        backward.Name.ShouldBe("Backward");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ForwardModeIsRegistered()
    {
        // Act
        var forward = SchemaCompatibilityModes.ByName("Forward");

        // Assert
        forward.ShouldNotBeNull();
        forward.Name.ShouldBe("Forward");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ExactModeIsRegistered()
    {
        // Act
        var exact = SchemaCompatibilityModes.ByName("Exact");

        // Assert
        exact.ShouldNotBeNull();
        exact.Name.ShouldBe("Exact");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void LooseModeIsRegistered()
    {
        // Act
        var loose = SchemaCompatibilityModes.ByName("Loose");

        // Assert
        loose.ShouldNotBeNull();
        loose.Name.ShouldBe("Loose");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void StructuralModeIsRegistered()
    {
        // Act
        var structural = SchemaCompatibilityModes.ByName("Structural");

        // Assert
        structural.ShouldNotBeNull();
        structural.Name.ShouldBe("Structural");
    }

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
