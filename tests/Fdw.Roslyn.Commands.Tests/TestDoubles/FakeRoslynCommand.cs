using Fdw.Collections;
using Fdw.Roslyn.Commands.Abstractions;

namespace Fdw.Roslyn.Commands.Tests.TestDoubles;

/// <summary>
/// Minimal <see cref="IRoslynCommand"/> test double with no extra reflection-visible properties.
/// Used to exercise the plain dispatch path of <see cref="RoslynCommandHandler"/> where none of the
/// optional BaselineSolution/SnapshotSolution/SnapshotName-style properties are present.
/// </summary>
public class FakeRoslynCommand : IRoslynCommand
{
    public int Id { get; init; } = 1;

    object ITypeOption.Id => Id;

    public string Name { get; init; } = "FakeCommand";

    public string Category => "Fake";

    public IRoslynCommandCategory? CommandCategory => null;
}
