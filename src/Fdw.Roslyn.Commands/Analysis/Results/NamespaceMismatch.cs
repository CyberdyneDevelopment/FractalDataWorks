using System.Diagnostics.CodeAnalysis;

namespace Fdw.Roslyn.Commands.Analysis.Results;

/// <summary>
/// One type whose namespace disagrees with its file path and/or its owning project.
/// </summary>
// Why: pure data holder, auto-properties only, no logic
[ExcludeFromCodeCoverage]
public sealed class NamespaceMismatch
{
    /// <summary>Gets or sets the type's fully-qualified name.</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>Gets or sets the type's declared namespace.</summary>
    public string Namespace { get; set; } = string.Empty;

    /// <summary>Gets or sets the type's current file path.</summary>
    public string CurrentPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the path the namespace implies, or <see langword="null"/> when it cannot be derived
    /// (typically because no project carries the namespace).
    /// </summary>
    public string? ExpectedPath { get; set; }

    /// <summary>Gets or sets the project that currently compiles the type.</summary>
    public string CurrentProject { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the project whose name EQUALS the namespace, or <see langword="null"/> when no project
    /// does. Null is a finding, not an error: the type has no home yet.
    /// </summary>
    /// <remarks>
    /// Only ever an exact match. The nearest ancestor project is reported separately as
    /// <see cref="NearestOwningProject"/> and is explicitly NOT a proposed destination — merging a
    /// backend-specific namespace into a generic ancestor package is a decision, not a default.
    /// </remarks>
    public string? ExpectedProject { get; set; }

    /// <summary>
    /// Gets or sets the closest existing project the namespace nests under, for information only.
    /// </summary>
    /// <remarks>
    /// Reported so the caller can see the current shape, NOT as a move target. For
    /// <c>Fdw.Data.MsSql</c> this is <c>Fdw.Data</c> — moving SQL Server types there would fold a
    /// backend vocabulary into the generic data package, which is the opposite of what a split intends.
    /// </remarks>
    public string? NearestOwningProject { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether a project named exactly <see cref="Namespace"/> exists.
    /// When false the type has no project of its own and cannot simply be moved into one.
    /// </summary>
    public bool ExpectedProjectExists { get; set; }

    /// <summary>Gets or sets the mismatch kind (a <see cref="Abstractions.Results.MismatchKinds"/> option name).</summary>
    public string MismatchKind { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the type is a <c>[TypeOption]</c> or
    /// <c>[ServiceTypeOption]</c>.
    /// </summary>
    /// <remarks>
    /// The highest-value diagnostic in this tool. A package reference IS a registration here: module
    /// initializers auto-register every <c>[TypeOption]</c> at assembly load, so moving one between
    /// assemblies changes which compilation emits its initializer and can leave a TypeCollection empty
    /// at runtime off a perfectly clean build.
    /// </remarks>
    public bool IsTypeOption { get; set; }
}
