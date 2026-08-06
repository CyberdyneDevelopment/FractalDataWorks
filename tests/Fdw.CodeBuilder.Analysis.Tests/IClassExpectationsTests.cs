using System;
using System.Linq;
using System.Reflection;
using Xunit;
using Shouldly;

namespace Fdw.CodeBuilder.Analysis.Tests;

/// <summary>
/// Contract tests for <see cref="IClassExpectations"/>.
/// </summary>
public sealed class IClassExpectationsTests
{
    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "SourceGen")]
    public void InterfaceIsPublic()
    {
        typeof(IClassExpectations).IsInterface.ShouldBeTrue();
        typeof(IClassExpectations).IsPublic.ShouldBeTrue();
    }

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    [InlineData("HasAccessModifier")]
    [InlineData("ImplementsInterface")]
    [InlineData("InheritsFrom")]
    [InlineData("IsAbstract")]
    [InlineData("IsSealed")]
    [InlineData("IsStatic")]
    [InlineData("IsPartial")]
    public void FluentMethodReturnsSelfForChaining(string methodName)
    {
        // Arrange
        var method = typeof(IClassExpectations).GetMethod(methodName);

        // Act & Assert
        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(IClassExpectations));
    }

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    [InlineData("HasMethod", typeof(IMethodExpectations))]
    [InlineData("HasProperty", typeof(IPropertyExpectations))]
    [InlineData("HasField", typeof(IFieldExpectations))]
    public void HasXxxMethodTakesNameAndOptionalNestedExpectationsCallback(string methodName, Type nestedExpectationType)
    {
        // Arrange
        var method = typeof(IClassExpectations).GetMethod(methodName);

        // Act & Assert
        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(IClassExpectations));

        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(2);
        parameters[0].ParameterType.ShouldBe(typeof(string));

        var actionType = typeof(Action<>).MakeGenericType(nestedExpectationType);
        parameters[1].ParameterType.ShouldBe(actionType);
        parameters[1].IsOptional.ShouldBeTrue();
        parameters[1].DefaultValue.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void HasConstructorTakesOnlyOptionalNestedExpectationsCallback()
    {
        // Arrange
        var method = typeof(IClassExpectations).GetMethod("HasConstructor");

        // Act & Assert
        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(IClassExpectations));

        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(1);
        parameters[0].ParameterType.ShouldBe(typeof(Action<IConstructorExpectations>));
        parameters[0].IsOptional.ShouldBeTrue();
        parameters[0].DefaultValue.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "SourceGen")]
    public void DeclaresExactlyElevenMembers()
    {
        // Act
        var methods = typeof(IClassExpectations).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        // Assert
        methods.Select(m => m.Name).ShouldBe(
            [
                "HasAccessModifier", "HasMethod", "HasProperty", "HasField", "HasConstructor",
                "ImplementsInterface", "InheritsFrom", "IsAbstract", "IsSealed", "IsStatic", "IsPartial"
            ],
            ignoreOrder: true);
    }
}
