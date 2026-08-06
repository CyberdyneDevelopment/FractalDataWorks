using Microsoft.CodeAnalysis;

namespace Fdw.Roslyn.Commands.Abstractions;

/// <summary>
/// Marks a command that needs the workspace's baseline solution injected before translation.
/// </summary>
/// <remarks>
/// Translators are stateless and never see <c>IRoslynWorkspace</c>, so anything that compares against
/// the baseline has to be handed it. This replaces a reflection probe for a property literally named
/// "BaselineSolution": a command that spelled it differently, or made it get-only, was skipped in
/// silence and the translator ran against a null baseline. Declaring the capability makes the compiler
/// enforce what the probe could only hope for.
/// </remarks>
public interface IBaselineAwareCommand
{
    /// <summary>Gets or sets the baseline solution. Set by the handler before translation.</summary>
    Solution? BaselineSolution { get; set; }
}
