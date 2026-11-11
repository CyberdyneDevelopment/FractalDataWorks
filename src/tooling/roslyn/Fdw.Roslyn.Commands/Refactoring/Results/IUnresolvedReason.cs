using Fdw.Collections;

namespace Fdw.Roslyn.Commands.Refactoring.Results;

/// <summary>One value of <see cref="UnresolvedReasons"/>: why an <c>&lt;inheritdoc/&gt;</c> site could not be expanded: the true MA0196 candidates that need explicit documentation written by hand.</summary>
public interface IUnresolvedReason : ITypeOption<int, UnresolvedReasonBase> { }
