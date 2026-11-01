namespace Fdw.Roslyn.Commands.Conventions.Results;

/// <summary>
/// Whether a reference rule requires or forbids an assembly.
/// </summary>
public enum GuideReferenceRuleKind
{
    /// <summary>The assembly must be among the project's references.</summary>
    Require,
    /// <summary>The assembly must not be among the project's references.</summary>
    Forbid,
}
