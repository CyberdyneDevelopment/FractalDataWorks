using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// The namespace disagrees with the owning project.
/// </summary>
[TypeOption(typeof(MismatchKinds), "Project")]
[ExcludeFromCodeCoverage]
public sealed class ProjectMismatchKind : MismatchKindBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectMismatchKind"/> class.
    /// </summary>
    public ProjectMismatchKind() : base(2, "Project") { }
}
