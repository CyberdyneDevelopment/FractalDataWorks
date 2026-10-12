using System.Threading.Tasks;
using Fdw.Roslyn.Commands.Abstractions.Results;
using Fdw.Roslyn.Commands.Workspace.Commands;
using Fdw.Roslyn.Commands.Workspace.Translators;
using Microsoft.CodeAnalysis;

namespace Fdw.Roslyn.Commands.Tests;

/// <summary>
/// Unit tests for <see cref="ApplyWorkspaceChangesTranslator"/> — verifies it stays parameterless
/// and returns an empty placeholder; the real workspace commit is performed by
/// <see cref="RoslynCommandHandler"/> (see <c>RoslynCommandHandlerTests</c>).
/// </summary>
public sealed class ApplyWorkspaceChangesTranslatorTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task TranslateReturnsAnEmptyPlaceholderResult()
    {
        // Arrange
        var translator = new ApplyWorkspaceChangesTranslator();
        var solution = new AdhocWorkspace().CurrentSolution;

        // Act
        var result = await translator.Translate(
            new ApplyWorkspaceChangesCommand(), solution, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull().Data.ShouldBeEmpty();
    }
}
