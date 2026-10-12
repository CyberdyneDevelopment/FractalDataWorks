using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// The ways a type's namespace can disagree with where it physically lives.
/// </summary>
[TypeCollection(typeof(MismatchKindBase), typeof(IMismatchKind), typeof(MismatchKinds))]
[ExcludeFromCodeCoverage]
public abstract partial class MismatchKinds : TypeCollectionBase<MismatchKindBase, IMismatchKind> { }
