using System;
using System.Linq;
using System.Reflection;
using Xunit;
using Shouldly;

namespace Fdw.CodeBuilder.Analysis.Tests;

/// <summary>
/// Contract tests for <see cref="IInterfaceExpectations"/>.
/// </summary>
public sealed class IInterfaceExpectationsTests
{
    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "SourceGen")]
    public void InterfaceIsPublic()
    {
        typeof(IInterfaceExpectations).IsInterface.ShouldBeTrue();
        typeof(IInterfaceExpectations).IsPublic.ShouldBeTrue();
    }

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    [InlineData("HasMethod", typeof(IMethodExpectations))]
    [InlineData("HasProperty", typeof(IPropertyExpectations))]
    public void HasXxxMethodTakesNameAndOptionalNestedExpectationsCallback(string methodName, Type nestedExpectationType)
    {
        // Arrange
        var method = typeof(IInterfaceExpectations).GetMethod(methodName);

        // Act & Assert
        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(IInterfaceExpectations));

        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(2);
        parameters[0].ParameterType.ShouldBe(typeof(string));

        var actionType = typeof(Action<>).MakeGenericType(nestedExpectationType);
        parameters[1].ParameterType.ShouldBe(actionType);
        parameters[1].IsOptional.ShouldBeTrue();
        parameters[1].DefaultValue.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "SourceGen")]
    public void DeclaresExactlyTwoMembers()
    {
        // Act
        var methods = typeof(IInterfaceExpectations).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        // Assert
        methods.Select(m => m.Name).ShouldBe(["HasMethod", "HasProperty"], ignoreOrder: true);
    }
}
