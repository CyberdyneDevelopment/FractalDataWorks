using System;
using System.Linq;
using System.Reflection;
using Xunit;
using Shouldly;

namespace Fdw.CodeBuilder.Analysis.Tests;

/// <summary>
/// Contract tests for <see cref="INamespaceExpectations"/>.
/// </summary>
public sealed class INamespaceExpectationsTests
{
    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "SourceGen")]
    public void InterfaceIsPublic()
    {
        typeof(INamespaceExpectations).IsInterface.ShouldBeTrue();
        typeof(INamespaceExpectations).IsPublic.ShouldBeTrue();
    }

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    [InlineData("HasClass", typeof(IClassExpectations))]
    [InlineData("HasInterface", typeof(IInterfaceExpectations))]
    public void HasXxxMethodTakesNameAndOptionalNestedExpectationsCallback(string methodName, Type nestedExpectationType)
    {
        // Arrange
        var method = typeof(INamespaceExpectations).GetMethod(methodName);

        // Act & Assert
        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(INamespaceExpectations));

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
        var methods = typeof(INamespaceExpectations).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        // Assert
        methods.Select(m => m.Name).ShouldBe(["HasClass", "HasInterface"], ignoreOrder: true);
    }
}
