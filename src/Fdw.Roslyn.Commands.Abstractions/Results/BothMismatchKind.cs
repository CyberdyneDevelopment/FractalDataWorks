using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// The namespace disagrees with both the file path and the owning project.
/// </summary>
[TypeOption(typeof(MismatchKinds), "Both")]
[ExcludeFromCodeCoverage]
public sealed class BothMismatchKind : MismatchKindBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BothMismatchKind"/> class.
    /// </summary>
    public BothMismatchKind() : base(3, "Both") { }
}
