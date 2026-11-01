using System;
using System.Collections.Generic;

namespace Fdw.Roslyn.Commands.Analysis.Results;

/// <summary>
/// Data returned by symbol information retrieval.
/// </summary>
public sealed record SymbolInfoData
{
    /// <summary>
    /// Gets or sets the symbol name.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the full symbol name.
    /// </summary>
    public string FullName { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the symbol kind.
    /// </summary>
    public string Kind { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the accessibility.
    /// </summary>
    public string Accessibility { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the symbol is static.
    /// </summary>
    public bool IsStatic { get; init; }

    /// <summary>
    /// Gets or sets whether the symbol is abstract.
    /// </summary>
    public bool IsAbstract { get; init; }

    /// <summary>
    /// Gets or sets whether the symbol is virtual.
    /// </summary>
    public bool IsVirtual { get; init; }

    /// <summary>
    /// Gets or sets whether the symbol is an override.
    /// </summary>
    public bool IsOverride { get; init; }

    /// <summary>
    /// Gets or sets whether the symbol is sealed.
    /// </summary>
    public bool IsSealed { get; init; }

    /// <summary>
    /// Gets or sets whether the symbol is extern.
    /// </summary>
    public bool IsExtern { get; init; }

    /// <summary>
    /// Gets or sets whether the symbol is implicitly declared.
    /// </summary>
    public bool IsImplicitlyDeclared { get; init; }

    /// <summary>
    /// Gets or sets the containing namespace.
    /// </summary>
    public string ContainingNamespace { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the containing type.
    /// </summary>
    public string ContainingType { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets additional type-specific information.
    /// </summary>
    public IReadOnlyDictionary<string, object>? AdditionalInfo { get; init; }

    /// <summary>
    /// Gets or sets the definition file path.
    /// </summary>
    public string? DefinitionFile { get; init; }

    /// <summary>
    /// Gets or sets the definition line number.
    /// </summary>
    public int? DefinitionLine { get; init; }

    /// <summary>
    /// Gets or sets the definition column number.
    /// </summary>
    public int? DefinitionColumn { get; init; }

    /// <summary>
    /// Gets or sets the XML documentation.
    /// </summary>
    public string? Documentation { get; init; }
}
