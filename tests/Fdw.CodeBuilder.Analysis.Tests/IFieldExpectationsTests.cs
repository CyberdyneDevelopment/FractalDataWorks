using System.Linq;
using System.Reflection;
using Xunit;
using Shouldly;

namespace Fdw.CodeBuilder.Analysis.Tests;

/// <summary>
/// Contract tests for <see cref="IFieldExpectations"/>.
/// </summary>
public sealed class IFieldExpectationsTests
{
    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "SourceGen")]
    public void InterfaceIsPublic()
    {
        typeof(IFieldExpectations).IsInterface.ShouldBeTrue();
        typeof(IFieldExpectations).IsPublic.ShouldBeTrue();
    }

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    [InlineData("IsReadOnly")]
    [InlineData("IsStatic")]
    [InlineData("IsConst")]
    public void ModifierPredicateMethodTakesNoParametersAndReturnsSelfForChaining(string methodName)
    {
        // Arrange
        var method = typeof(IFieldExpectations).GetMethod(methodName);

        // Act & Assert
        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(IFieldExpectations));
        method.GetParameters().ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void HasTypeTakesTypeNameAsString()
    {
        // Arrange
        var method = typeof(IFieldExpectations).GetMethod("HasType");

        // Act & Assert
        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(IFieldExpectations));

        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(1);
        parameters[0].ParameterType.ShouldBe(typeof(string));
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "SourceGen")]
    public void DeclaresExactlyFourMembers()
    {
        // Act
        var methods = typeof(IFieldExpectations).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        // Assert
        methods.Select(m => m.Name).ShouldBe(["HasType", "IsReadOnly", "IsStatic", "IsConst"], ignoreOrder: true);
    }
}
