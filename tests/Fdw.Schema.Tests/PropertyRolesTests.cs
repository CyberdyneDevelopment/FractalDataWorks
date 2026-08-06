using System.Linq;
using Shouldly;
using Xunit;

namespace Fdw.Schema.Tests;

public class PropertyRolesTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AllReturnsAllFiveRoles()
    {
        // Act
        var roles = PropertyRoles.All();

        // Assert
        roles.ShouldNotBeNull();
        roles.Count().ShouldBe(5);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void SurrogateRoleExists()
    {
        // Act
        var surrogate = PropertyRoles.ByName("Surrogate");

        // Assert
        surrogate.ShouldNotBeNull();
        surrogate.Name.ShouldBe("Surrogate");
        surrogate.IsKeyRole.ShouldBeTrue();
        surrogate.IsIndexable.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void NaturalKeyRoleExists()
    {
        // Act
        var naturalKey = PropertyRoles.ByName("NaturalKey");

        // Assert
        naturalKey.ShouldNotBeNull();
        naturalKey.Name.ShouldBe("NaturalKey");
        naturalKey.IsKeyRole.ShouldBeTrue();
        naturalKey.IsIndexable.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void LookupRoleExists()
    {
        // Act
        var lookup = PropertyRoles.ByName("Lookup");

        // Assert
        lookup.ShouldNotBeNull();
        lookup.Name.ShouldBe("Lookup");
        lookup.IsKeyRole.ShouldBeFalse();
        lookup.IsIndexable.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AttributeRoleExists()
    {
        // Act
        var attribute = PropertyRoles.ByName("Attribute");

        // Assert
        attribute.ShouldNotBeNull();
        attribute.Name.ShouldBe("Attribute");
        attribute.IsKeyRole.ShouldBeFalse();
        attribute.IsIndexable.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void MeasureRoleExists()
    {
        // Act
        var measure = PropertyRoles.ByName("Measure");

        // Assert
        measure.ShouldNotBeNull();
        measure.Name.ShouldBe("Measure");
        measure.IsKeyRole.ShouldBeFalse();
        measure.IsIndexable.ShouldBeFalse();
        measure.IsAggregatable.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ByIdReturnsCorrectRole()
    {
        // Arrange
        var surrogateByName = PropertyRoles.ByName("Surrogate");

        // Act
        var surrogateById = PropertyRoles.ById(surrogateByName.Id);

        // Assert
        surrogateById.ShouldBe(surrogateByName);
    }

}
