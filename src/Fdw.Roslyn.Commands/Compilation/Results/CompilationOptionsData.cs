using System;
using System.Collections.Generic;

namespace Fdw.Roslyn.Commands.Compilation.Results;

/// <summary>
/// Data returned by get compilation options operation.
/// </summary>
public sealed class CompilationOptionsData
{
    /// <summary>
    /// Gets or sets the project name.
    /// </summary>
    public required string ProjectName { get; init; }

    /// <summary>
    /// Gets or sets the output kind (ConsoleApplication, Library, etc.).
    /// </summary>
    public required string OutputKind { get; init; }

    /// <summary>
    /// Gets or sets the target platform.
    /// </summary>
    public required string Platform { get; init; }

    /// <summary>
    /// Gets or sets the optimization level.
    /// </summary>
    public required string OptimizationLevel { get; init; }

    /// <summary>
    /// Gets or sets whether overflow checking is enabled.
    /// </summary>
    public required bool CheckOverflow { get; init; }

    /// <summary>
    /// Gets or sets whether unsafe code is allowed.
    /// </summary>
    public required bool AllowUnsafe { get; init; }

    /// <summary>
    /// Gets or sets the nullable context options.
    /// </summary>
    public required string NullableContextOptions { get; init; }

    /// <summary>
    /// Gets or sets the C# language version.
    /// </summary>
    public required string LanguageVersion { get; init; }

    /// <summary>
    /// Gets or sets the preprocessor symbols.
    /// </summary>
    public required IReadOnlyList<string> PreprocessorSymbols { get; init; }

    /// <summary>
    /// Gets or sets the referenced assemblies.
    /// </summary>
    public required IReadOnlyList<string> ReferencedAssemblies { get; init; }
}
