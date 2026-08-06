using System;
using System.Linq;
using System.Reflection;
using Xunit;
using Shouldly;

namespace Fdw.CodeBuilder.Analysis.Tests;

/// <summary>
/// Contract tests for <see cref="IRecordExpectations"/>.
/// </summary>
public sealed class IRecordExpectationsTests
{
    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "SourceGen")]
    public void InterfaceIsPublic()
    {
        typeof(IRecordExpectations).IsInterface.ShouldBeTrue();
        typeof(IRecordExpectations).IsPublic.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void HasPropertyTakesNameAndOptionalNestedExpectationsCallback()
    {
        // Arrange
        var method = typeof(IRecordExpectations).GetMethod("HasProperty");

        // Act & Assert
        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(IRecordExpectations));

        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(2);
        parameters[0].ParameterType.ShouldBe(typeof(string));
        parameters[1].ParameterType.ShouldBe(typeof(Action<IPropertyExpectations>));
        parameters[1].IsOptional.ShouldBeTrue();
        parameters[1].DefaultValue.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void HasParameterTakesNameAndTypeAsStrings()
    {
        // Arrange
        var method = typeof(IRecordExpectations).GetMethod("HasParameter");

        // Act & Assert
        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(IRecordExpectations));

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
    public void DeclaresExactlyTwoMembers()
    {
        // Act
        var methods = typeof(IRecordExpectations).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        // Assert
        methods.Select(m => m.Name).ShouldBe(["HasProperty", "HasParameter"], ignoreOrder: true);
    }
}
