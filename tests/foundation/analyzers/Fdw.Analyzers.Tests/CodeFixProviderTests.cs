using Fdw.CodeFixes;

namespace Fdw.Analyzers.Tests;

/// <summary>
/// Structural verification tests for code fix providers.
/// Confirms each provider is properly wired up with correct diagnostic IDs and FixAllProvider support.
/// </summary>
public class CodeFixProviderTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void UncheckedGenericResultCodeFixFixesCorrectDiagnosticId()
    {
        // Arrange
        var provider = new UncheckedGenericResultCodeFixProvider();

        // Act
        var ids = provider.FixableDiagnosticIds;

        // Assert
        ids.ShouldContain("FDW012");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void UnhandledFailurePathCodeFixFixesCorrectDiagnosticId()
    {
        // Arrange
        var provider = new UnhandledFailurePathCodeFixProvider();

        // Act
        var ids = provider.FixableDiagnosticIds;

        // Assert
        ids.ShouldContain("FDW013");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void ExceptionNotPropagatedCodeFixFixesCorrectDiagnosticId()
    {
        // Arrange
        var provider = new ExceptionNotPropagatedCodeFixProvider();

        // Act
        var ids = provider.FixableDiagnosticIds;

        // Assert
        ids.ShouldContain("FDW014");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void BrokenResultChainCodeFixFixesCorrectDiagnosticId()
    {
        // Arrange
        var provider = new BrokenResultChainCodeFixProvider();

        // Act
        var ids = provider.FixableDiagnosticIds;

        // Assert
        ids.ShouldContain("FDW015");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void UncheckedGenericResultCodeFixProvidesFixAllProvider()
    {
        // Arrange
        var provider = new UncheckedGenericResultCodeFixProvider();

        // Act
        var fixAllProvider = provider.GetFixAllProvider();

        // Assert
        fixAllProvider.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void UnhandledFailurePathCodeFixProvidesFixAllProvider()
    {
        // Arrange
        var provider = new UnhandledFailurePathCodeFixProvider();

        // Act
        var fixAllProvider = provider.GetFixAllProvider();

        // Assert
        fixAllProvider.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void ExceptionNotPropagatedCodeFixProvidesFixAllProvider()
    {
        // Arrange
        var provider = new ExceptionNotPropagatedCodeFixProvider();

        // Act
        var fixAllProvider = provider.GetFixAllProvider();

        // Assert
        fixAllProvider.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void BrokenResultChainCodeFixProvidesFixAllProvider()
    {
        // Arrange
        var provider = new BrokenResultChainCodeFixProvider();

        // Act
        var fixAllProvider = provider.GetFixAllProvider();

        // Assert
        fixAllProvider.ShouldNotBeNull();
    }
}
