using System.Linq;
using System.Reflection;
using Xunit;
using Shouldly;

namespace Fdw.CodeBuilder.Analysis.Tests;

/// <summary>
/// Contract tests for <see cref="IEnumExpectations"/>.
/// </summary>
public sealed class IEnumExpectationsTests
{
    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "SourceGen")]
    public void InterfaceIsPublic()
    {
        typeof(IEnumExpectations).IsInterface.ShouldBeTrue();
        typeof(IEnumExpectations).IsPublic.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void HasValueTakesNameAndOptionalNullableNumericValueDefaultingToNull()
    {
        // Arrange
        var method = typeof(IEnumExpectations).GetMethod("HasValue");

        // Act & Assert
        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(IEnumExpectations));

        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(2);
        parameters[0].ParameterType.ShouldBe(typeof(string));
        parameters[0].Name.ShouldBe("valueName");
        parameters[1].ParameterType.ShouldBe(typeof(int?));
        parameters[1].Name.ShouldBe("value");
        parameters[1].IsOptional.ShouldBeTrue();
        parameters[1].DefaultValue.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void HasUnderlyingTypeTakesTypeNameAndReturnsSelfForChaining()
    {
        // Arrange
        var method = typeof(IEnumExpectations).GetMethod("HasUnderlyingType");

        // Act & Assert
        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(IEnumExpectations));

        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(1);
        parameters[0].ParameterType.ShouldBe(typeof(string));
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "SourceGen")]
    public void DeclaresExactlyTwoMembers()
    {
        // Act
        var methods = typeof(IEnumExpectations).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        // Assert
        methods.Select(m => m.Name).ShouldBe(["HasValue", "HasUnderlyingType"], ignoreOrder: true);
    }
}
