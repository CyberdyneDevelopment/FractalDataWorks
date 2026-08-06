using Fdw.Data.DataStores.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Data.Abstractions.Tests.DataStores;

public sealed class PathParameterTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsName()
    {
        // Arrange & Act
        var parameter = new PathParameter("id", typeof(int));

        // Assert
        parameter.Name.ShouldBe("id");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsParameterType()
    {
        // Arrange & Act
        var parameter = new PathParameter("id", typeof(int));

        // Assert
        parameter.ParameterType.ShouldBe(typeof(int));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void IsRequiredDefaultsToTrue()
    {
        // Arrange & Act
        var parameter = new PathParameter("id", typeof(int));

        // Assert
        parameter.IsRequired.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void IsRequiredCanBeSetToFalse()
    {
        // Arrange & Act
        var parameter = new PathParameter("id", typeof(int), isRequired: false);

        // Assert
        parameter.IsRequired.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void DefaultValueDefaultsToNull()
    {
        // Arrange & Act
        var parameter = new PathParameter("id", typeof(int));

        // Assert
        parameter.DefaultValue.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void DefaultValueCanBeSet()
    {
        // Arrange & Act
        var parameter = new PathParameter("page", typeof(int), isRequired: false, defaultValue: 1);

        // Assert
        parameter.DefaultValue.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void DescriptionDefaultsToNull()
    {
        // Arrange & Act
        var parameter = new PathParameter("id", typeof(int));

        // Assert
        parameter.Description.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void DescriptionCanBeSet()
    {
        // Arrange & Act
        var parameter = new PathParameter(
            "id",
            typeof(int),
            description: "The unique identifier");

        // Assert
        parameter.Description.ShouldBe("The unique identifier");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ValidationRulesDefaultsToEmptyList()
    {
        // Arrange & Act
        var parameter = new PathParameter("id", typeof(int));

        // Assert
        parameter.ValidationRules.ShouldNotBeNull();
        parameter.ValidationRules.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ValidationRulesCanBeSet()
    {
        // Arrange
        var rules = new[] { "min:1", "max:100" };

        // Act
        var parameter = new PathParameter("id", typeof(int), validationRules: rules);

        // Assert
        parameter.ValidationRules.Count.ShouldBe(2);
        parameter.ValidationRules.ShouldContain("min:1");
        parameter.ValidationRules.ShouldContain("max:100");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorThrowsWhenNameIsNull()
    {
        // Arrange & Act & Assert
        Should.Throw<ArgumentNullException>(() => new PathParameter(null!, typeof(int)));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorThrowsWhenParameterTypeIsNull()
    {
        // Arrange & Act & Assert
        Should.Throw<ArgumentNullException>(() => new PathParameter("id", null!));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void CanCreateWithStringType()
    {
        // Arrange & Act
        var parameter = new PathParameter("name", typeof(string));

        // Assert
        parameter.ParameterType.ShouldBe(typeof(string));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void CanCreateWithGuidType()
    {
        // Arrange & Act
        var parameter = new PathParameter("correlationId", typeof(Guid));

        // Assert
        parameter.ParameterType.ShouldBe(typeof(Guid));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void CanCreateWithDateTimeType()
    {
        // Arrange & Act
        var parameter = new PathParameter("timestamp", typeof(DateTime));

        // Assert
        parameter.ParameterType.ShouldBe(typeof(DateTime));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void CompleteParameterConfiguration()
    {
        // Arrange
        var rules = new[] { "format:email", "maxLength:255" };

        // Act
        var parameter = new PathParameter(
            name: "email",
            parameterType: typeof(string),
            isRequired: true,
            defaultValue: null,
            description: "User email address",
            validationRules: rules);

        // Assert
        parameter.Name.ShouldBe("email");
        parameter.ParameterType.ShouldBe(typeof(string));
        parameter.IsRequired.ShouldBeTrue();
        parameter.DefaultValue.ShouldBeNull();
        parameter.Description.ShouldBe("User email address");
        parameter.ValidationRules.Count.ShouldBe(2);
        parameter.ValidationRules.ShouldContain("format:email");
        parameter.ValidationRules.ShouldContain("maxLength:255");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void OptionalParameterWithDefault()
    {
        // Arrange & Act
        var parameter = new PathParameter(
            name: "pageSize",
            parameterType: typeof(int),
            isRequired: false,
            defaultValue: 10,
            description: "Number of items per page");

        // Assert
        parameter.IsRequired.ShouldBeFalse();
        parameter.DefaultValue.ShouldBe(10);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ParameterWithMultipleValidationRules()
    {
        // Arrange
        var rules = new[] { "min:18", "max:120", "integer" };

        // Act
        var parameter = new PathParameter(
            "age",
            typeof(int),
            validationRules: rules);

        // Assert
        parameter.ValidationRules.Count.ShouldBe(3);
        parameter.ValidationRules[0].ShouldBe("min:18");
        parameter.ValidationRules[1].ShouldBe("max:120");
        parameter.ValidationRules[2].ShouldBe("integer");
    }
}
