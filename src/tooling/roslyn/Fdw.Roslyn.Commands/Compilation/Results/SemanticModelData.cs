using System;
using System.Collections.Generic;

namespace Fdw.Roslyn.Commands.Compilation.Results;

/// <summary>
/// Data returned by get semantic model operation.
/// </summary>
public sealed class SemanticModelData
{
    /// <summary>
    /// Gets or sets the file path.
    /// </summary>
    public required string FilePath { get; init; }

    /// <summary>
    /// Gets or sets the declared symbols.
    /// </summary>
    public required IReadOnlyList<SymbolDeclaration> DeclaredSymbols { get; init; }

    /// <summary>
    /// Gets or sets the referenced assemblies.
    /// </summary>
    public required IReadOnlyList<string> ReferencedAssemblies { get; init; }

    /// <summary>
    /// Gets or sets the language version.
    /// </summary>
    public required string LanguageVersion { get; init; }
}