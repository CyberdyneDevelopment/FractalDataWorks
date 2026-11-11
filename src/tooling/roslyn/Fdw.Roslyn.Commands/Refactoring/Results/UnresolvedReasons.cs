using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Roslyn.Commands.Refactoring.Results;

/// <summary>TypeCollection of why an <c>&lt;inheritdoc/&gt;</c> site could not be expanded: the true MA0196 candidates that need explicit documentation written by hand.</summary>
[TypeCollection(typeof(UnresolvedReasonBase), typeof(IUnresolvedReason), typeof(UnresolvedReasons))]
[ExcludeFromCodeCoverage]
public abstract partial class UnresolvedReasons : TypeCollectionBase<UnresolvedReasonBase, IUnresolvedReason> { }
