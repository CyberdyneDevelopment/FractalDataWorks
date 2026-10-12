using System.Linq;
using System.Reflection;
using Xunit;
using Shouldly;

namespace Fdw.CodeBuilder.Analysis.Tests;

/// <summary>
/// Contract tests for <see cref="IPropertyExpectations"/>.
/// </summary>
public sealed class IPropertyExpectationsTests
{
    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "SourceGen")]
    public void InterfaceIsPublic()
    {
        typeof(IPropertyExpectations).IsInterface.ShouldBeTrue();
        typeof(IPropertyExpectations).IsPublic.ShouldBeTrue();
    }

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    [InlineData("HasGetter")]
    [InlineData("HasSetter")]
    [InlineData("HasInitSetter")]
    [InlineData("IsReadOnly")]
    [InlineData("IsStatic")]
    public void ModifierPredicateMethodTakesNoParametersAndReturnsSelfForChaining(string methodName)
    {
        // Arrange
        var method = typeof(IPropertyExpectations).GetMethod(methodName);

        // Act & Assert
        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(IPropertyExpectations));
        method.GetParameters().ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void HasTypeTakesTypeNameAsString()
    {
        // Arrange
        var method = typeof(IPropertyExpectations).GetMethod("HasType");

        // Act & Assert
        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(IPropertyExpectations));

        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(1);
        parameters[0].ParameterType.ShouldBe(typeof(string));
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "SourceGen")]
    public void DeclaresExactlySixMembers()
    {
        // Act
        var methods = typeof(IPropertyExpectations).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        // Assert
        methods.Select(m => m.Name).ShouldBe(
            ["HasType", "HasGetter", "HasSetter", "HasInitSetter", "IsReadOnly", "IsStatic"],
            ignoreOrder: true);
    }
}
