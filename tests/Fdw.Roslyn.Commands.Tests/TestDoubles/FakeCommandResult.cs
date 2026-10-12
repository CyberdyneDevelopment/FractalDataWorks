using Fdw.Roslyn.Commands.Abstractions;
using Microsoft.CodeAnalysis;

namespace Fdw.Roslyn.Commands.Tests.TestDoubles;

/// <summary>
/// Minimal <see cref="IRoslynCommandResult"/> test double.
/// </summary>
public sealed class FakeCommandResult : IRoslynCommandResult
{
    public string Summary { get; init; } = "ok";

    public bool IsMutation { get; init; }

    public Solution? NewSolution { get; init; }
}
