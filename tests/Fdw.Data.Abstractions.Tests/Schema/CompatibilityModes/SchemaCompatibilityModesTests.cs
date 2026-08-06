using Fdw.Data.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Data.Abstractions.Tests.Schema.CompatibilityModes;

public sealed class SchemaCompatibilityModesTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AllReturnsAllCompatibilityModes()
    {
        // Act
        var all = SchemaCompatibilityModes.All();

        // Assert
        all.ShouldNotBeEmpty();
        all.Count.ShouldBeGreaterThanOrEqualTo(5); // Backward, Forward, Exact, Loose, Structural
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ByIdReturnsCorrectMode()
    {
        // Arrange
        var all = SchemaCompatibilityModes.All();
        var first = all.First();

        // Act
        var result = SchemaCompatibilityModes.ById(first.Id);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(first.Id);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ByIdReturnsNullForUnknownId()
    {
        // Act
        var result = SchemaCompatibilityModes.ById(99999);

        // Assert
        result.ShouldBe(SchemaCompatibilityModes.NotFound);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ByNameIsCaseSensitive()
    {
        // Arrange
        var backward = SchemaCompatibilityModes.ByName("Backward");

        // Act & Assert
        backward.ShouldNotBeNull();
        SchemaCompatibilityModes.ByName("backward").ShouldBe(SchemaCompatibilityModes.NotFound);
        SchemaCompatibilityModes.ByName("BACKWARD").ShouldBe(SchemaCompatibilityModes.NotFound);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void NotFoundReturnsEmptyInstance()
    {
        // Act
        var result = SchemaCompatibilityModes.NotFound;

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("_Empty");
    }

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
    public void AllModesImplementISchemaCompatibilityMode()
    {
        // Arrange
        var all = SchemaCompatibilityModes.All();

        // Act & Assert
        foreach (var mode in all)
        {
            mode.ShouldBeAssignableTo<ISchemaCompatibilityMode>();
        }
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AllModesHaveUniqueIds()
    {
        // Arrange
        var all = SchemaCompatibilityModes.All();

        // Act
        var ids = all.Select(m => m.Id).ToHashSet();

        // Assert
        ids.Count.ShouldBe(all.Count);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AllModesHaveUniqueNames()
    {
        // Arrange
        var all = SchemaCompatibilityModes.All();

        // Act
        var names = all.Select(m => m.Name).ToHashSet();

        // Assert
        names.Count.ShouldBe(all.Count);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ByNameReturnsNotFoundForUnknownName()
    {
        // Act
        var result = SchemaCompatibilityModes.ByName("NonExistentMode");

        // Assert
        result.ShouldBe(SchemaCompatibilityModes.NotFound);
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
