using System.Linq;
using System.Reflection;
using Xunit;
using Shouldly;

namespace Fdw.CodeBuilder.Analysis.Tests;

/// <summary>
/// Contract tests for <see cref="IConstructorExpectations"/>.
/// </summary>
public sealed class IConstructorExpectationsTests
{
    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "SourceGen")]
    public void InterfaceIsPublic()
    {
        typeof(IConstructorExpectations).IsInterface.ShouldBeTrue();
        typeof(IConstructorExpectations).IsPublic.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void HasParameterTakesNameAndTypeAsStrings()
    {
        // Arrange
        var method = typeof(IConstructorExpectations).GetMethod("HasParameter");

        // Act & Assert
        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(IConstructorExpectations));

        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(2);
        parameters[0].ParameterType.ShouldBe(typeof(string));
        parameters[0].Name.ShouldBe("parameterName");
        parameters[1].ParameterType.ShouldBe(typeof(string));
        parameters[1].Name.ShouldBe("parameterType");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void IsStaticTakesNoParametersAndReturnsSelfForChaining()
    {
        // Arrange
        var method = typeof(IConstructorExpectations).GetMethod("IsStatic");

        // Act & Assert
        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(IConstructorExpectations));
        method.GetParameters().ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "SourceGen")]
    public void DeclaresExactlyTwoMembers()
    {
        // Act
        var methods = typeof(IConstructorExpectations).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        // Assert
        methods.Select(m => m.Name).ShouldBe(["HasParameter", "IsStatic"], ignoreOrder: true);
    }
}
