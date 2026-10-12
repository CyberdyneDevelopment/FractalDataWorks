using Fdw.Collections;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// Marker interface for the kind of disagreement between a type's namespace, its file path and its
/// owning project.
/// </summary>
public interface IMismatchKind : ITypeOption<int, MismatchKindBase> { }
