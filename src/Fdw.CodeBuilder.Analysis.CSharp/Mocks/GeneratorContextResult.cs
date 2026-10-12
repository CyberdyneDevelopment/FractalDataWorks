using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Fdw.CodeBuilder.Abstractions;
using Microsoft.CodeAnalysis;

namespace Fdw.CodeBuilder.Analysis.CSharp.Mocks;

/// <summary>
/// Result of a source production context for testing diagnostic reporting.
/// </summary>
public class GeneratorContextResult
{
    private readonly List<Diagnostic> _reportedDiagnostics = [];
    private readonly Dictionary<string, string> _generatedSources = [];

    /// <summary>
    /// Gets list of reported diagnostics.
    /// </summary>
    public IReadOnlyList<Diagnostic> ReportedDiagnostics => _reportedDiagnostics.AsReadOnly();

    /// <summary>
    /// Gets dictionary of generated sources.
    /// </summary>
    public IReadOnlyDictionary<string, string> GeneratedSources => _generatedSources;

    /// <summary>
    /// Adds a diagnostic to the result.
    /// </summary>
    /// <param name="diagnostic">The diagnostic to add.</param>
    internal void AddDiagnostic(Diagnostic diagnostic)
    {
        _reportedDiagnostics.Add(diagnostic);
    }

    /// <summary>
    /// Adds a generated source to the result.
    /// </summary>
    /// <param name="hintName">The hint name for the source.</param>
    /// <param name="source">The source content.</param>
    internal void AddGeneratedSource(string hintName, string source)
    {
        _generatedSources[hintName] = source;
    }
}