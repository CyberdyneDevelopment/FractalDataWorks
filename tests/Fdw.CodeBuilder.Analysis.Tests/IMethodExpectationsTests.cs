using System.Linq;
using System.Reflection;
using Xunit;
using Shouldly;

namespace Fdw.CodeBuilder.Analysis.Tests;

/// <summary>
/// Contract tests for <see cref="IMethodExpectations"/>.
/// </summary>
public sealed class IMethodExpectationsTests
{
    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "SourceGen")]
    public void InterfaceIsPublic()
    {
        typeof(IMethodExpectations).IsInterface.ShouldBeTrue();
        typeof(IMethodExpectations).IsPublic.ShouldBeTrue();
    }

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    [InlineData("IsAsync")]
    [InlineData("IsStatic")]
    [InlineData("IsVirtual")]
    [InlineData("IsOverride")]
    [InlineData("IsAbstract")]
    public void ModifierPredicateMethodTakesNoParametersAndReturnsSelfForChaining(string methodName)
    {
        // Arrange
        var method = typeof(IMethodExpectations).GetMethod(methodName);

        // Act & Assert
        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(IMethodExpectations));
        method.GetParameters().ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void HasReturnTypeTakesTypeNameAsString()
    {
        // Arrange
        var method = typeof(IMethodExpectations).GetMethod("HasReturnType");

        // Act & Assert
        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(IMethodExpectations));

        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(1);
        parameters[0].ParameterType.ShouldBe(typeof(string));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void HasParameterTakesNameAndTypeAsStrings()
    {
        // Arrange
        var method = typeof(IMethodExpectations).GetMethod("HasParameter");

        // Act & Assert
        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(IMethodExpectations));

        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(2);
        parameters[0].ParameterType.ShouldBe(typeof(string));
        parameters[0].Name.ShouldBe("parameterName");
        parameters[1].ParameterType.ShouldBe(typeof(string));
        parameters[1].Name.ShouldBe("parameterType");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "SourceGen")]
    public void DeclaresExactlySevenMembers()
    {
        // Act
        var methods = typeof(IMethodExpectations).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        // Assert
        methods.Select(m => m.Name).ShouldBe(
            ["HasReturnType", "HasParameter", "IsAsync", "IsStatic", "IsVirtual", "IsOverride", "IsAbstract"],
            ignoreOrder: true);
    }
}
