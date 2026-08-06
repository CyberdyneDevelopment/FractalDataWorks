using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// The namespace disagrees with the file path, but the owning project is correct.
/// </summary>
[TypeOption(typeof(MismatchKinds), "Path")]
[ExcludeFromCodeCoverage]
public sealed class PathMismatchKind : MismatchKindBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PathMismatchKind"/> class.
    /// </summary>
    public PathMismatchKind() : base(1, "Path") { }
}
