using Fdw.Roslyn.Commands.Abstractions;
using Microsoft.CodeAnalysis;

namespace Fdw.Roslyn.Commands.Tests.TestDoubles;

/// <summary>
/// A command that declares it needs the workspace baseline injected before translation.
/// </summary>
public sealed class FakeBaselineAwareCommand : FakeRoslynCommand, IBaselineAwareCommand
{
    public Solution? BaselineSolution { get; set; }
}
