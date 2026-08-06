using System;
using System.Linq;
using System.Reflection;
using Xunit;
using Shouldly;

namespace Fdw.CodeBuilder.Analysis.Tests;

/// <summary>
/// Contract tests for <see cref="ISyntaxTreeExpectations"/>.
/// </summary>
public sealed class ISyntaxTreeExpectationsTests
{
    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "SourceGen")]
    public void InterfaceIsPublic()
    {
        typeof(ISyntaxTreeExpectations).IsInterface.ShouldBeTrue();
        typeof(ISyntaxTreeExpectations).IsPublic.ShouldBeTrue();
    }

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    [InlineData("HasNamespace", typeof(INamespaceExpectations))]
    [InlineData("HasClass", typeof(IClassExpectations))]
    [InlineData("HasInterface", typeof(IInterfaceExpectations))]
    [InlineData("HasEnum", typeof(IEnumExpectations))]
    [InlineData("HasRecord", typeof(IRecordExpectations))]
    public void HasXxxMethodTakesNameAndOptionalNestedExpectationsCallback(string methodName, Type nestedExpectationType)
    {
        // Arrange
        var method = typeof(ISyntaxTreeExpectations).GetMethod(methodName);

        // Act & Assert
        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(ISyntaxTreeExpectations));

        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(2);
        parameters[0].ParameterType.ShouldBe(typeof(string));

        var actionType = typeof(Action<>).MakeGenericType(nestedExpectationType);
        parameters[1].ParameterType.ShouldBe(actionType);
        parameters[1].IsOptional.ShouldBeTrue();
        parameters[1].HasDefaultValue.ShouldBeTrue();
        parameters[1].DefaultValue.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void CompilesTakesNoParametersAndReturnsSelfForChaining()
    {
        // Arrange
        var method = typeof(ISyntaxTreeExpectations).GetMethod("Compiles");

        // Act & Assert
        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(ISyntaxTreeExpectations));
        method.GetParameters().ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "SourceGen")]
    public void DeclaresExactlySixMembers()
    {
        // Act
        var methods = typeof(ISyntaxTreeExpectations).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        // Assert
        methods.Select(m => m.Name).ShouldBe(
            ["HasNamespace", "HasClass", "HasInterface", "HasEnum", "HasRecord", "Compiles"],
            ignoreOrder: true);
    }
}
