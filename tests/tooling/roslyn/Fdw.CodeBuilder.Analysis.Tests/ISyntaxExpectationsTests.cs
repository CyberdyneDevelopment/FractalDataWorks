using System.Linq;
using System.Reflection;
using Xunit;
using Shouldly;

namespace Fdw.CodeBuilder.Analysis.Tests;

/// <summary>
/// Contract tests for <see cref="ISyntaxExpectations"/>.
/// </summary>
/// <remarks>
/// Why: <c>Fdw.CodeBuilder.Analysis</c> ships only interfaces (the fluent code-assertion DSL
/// consumed by the concrete Roslyn-backed implementation in <c>Fdw.CodeBuilder.Analysis.CSharp</c>).
/// There is no executable IL in this assembly beyond the interface method table, so there is no
/// "business logic" to unit test in the usual sense. What CAN be pinned down — and is genuinely
/// worth pinning — is the public contract's shape: entry-point methods must return the documented
/// interface so the fluent chain compiles for every consumer. These reflection-based tests catch
/// an accidental rename or return-type change before it breaks every downstream implementation.
/// </remarks>
public sealed class ISyntaxExpectationsTests
{
    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "SourceGen")]
    public void InterfaceIsPublic()
    {
        typeof(ISyntaxExpectations).IsInterface.ShouldBeTrue();
        typeof(ISyntaxExpectations).IsPublic.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void ExpectCodeAcceptsGeneratedCodeStringAndReturnsSyntaxTreeExpectations()
    {
        // Arrange
        var method = typeof(ISyntaxExpectations).GetMethod("ExpectCode");

        // Act & Assert
        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(ISyntaxTreeExpectations));
        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(1);
        parameters[0].ParameterType.ShouldBe(typeof(string));
        parameters[0].Name.ShouldBe("generatedCode");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void ExpectSyntaxTreeAcceptsObjectAndReturnsSyntaxTreeExpectations()
    {
        // Arrange
        var method = typeof(ISyntaxExpectations).GetMethod("ExpectSyntaxTree");

        // Act & Assert
        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(ISyntaxTreeExpectations));
        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(1);
        parameters[0].ParameterType.ShouldBe(typeof(object));
        parameters[0].Name.ShouldBe("syntaxTree");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "SourceGen")]
    public void DeclaresExactlyTwoEntryPointMethods()
    {
        // Act
        var methods = typeof(ISyntaxExpectations).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        // Assert
        methods.Select(m => m.Name).ShouldBe(["ExpectCode", "ExpectSyntaxTree"], ignoreOrder: true);
    }
}
