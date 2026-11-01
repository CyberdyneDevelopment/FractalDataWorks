using Fdw.Services.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Fdw.Services.Abstractions.Tests;

public class ServiceLifetimeBaseTests
{
    [ExcludeFromCodeCoverage]
    private sealed class TestServiceLifetime : ServiceLifetimeBase
    {
        public TestServiceLifetime(int id, string name, string description, ServiceLifetime enumValue)
            : base(id, name, description, enumValue)
        {
        }
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ConstructorSetsId()
    {
        // Arrange
        var id = 99;

        // Act
        var result = new TestServiceLifetime(id, "Test", "Test description", ServiceLifetime.Transient);

        // Assert
        result.Id.ShouldBe(id);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ConstructorSetsName()
    {
        // Arrange
        var name = "TestName";

        // Act
        var result = new TestServiceLifetime(1, name, "Test description", ServiceLifetime.Transient);

        // Assert
        result.Name.ShouldBe(name);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ConstructorSetsDescription()
    {
        // Arrange
        var description = "Test description";

        // Act
        var result = new TestServiceLifetime(1, "Test", description, ServiceLifetime.Transient);

        // Assert
        result.Description.ShouldBe(description);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ConstructorSetsEnumValue()
    {
        // Arrange
        var enumValue = ServiceLifetime.Scoped;

        // Act
        var result = new TestServiceLifetime(1, "Test", "Test description", enumValue);

        // Assert
        result.EnumValue.ShouldBe(enumValue);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ImplementsIServiceLifetime()
    {
        // Act
        var result = new TestServiceLifetime(1, "Test", "Test description", ServiceLifetime.Transient);

        // Assert
        result.ShouldBeAssignableTo<IServiceLifetime>();
    }
}
