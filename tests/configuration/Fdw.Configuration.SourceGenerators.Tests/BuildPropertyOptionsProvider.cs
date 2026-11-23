using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Fdw.Configuration.SourceGenerators.Tests;

/// <summary>Hands a generator the MSBuild properties a build states, and nothing per file (FDW-7107).</summary>
internal sealed class BuildPropertyOptionsProvider : AnalyzerConfigOptionsProvider
{
    // Why a file has none: the build's properties are global options, never per file.
    private static readonly BuildPropertyOptions NoFileOptions = new(new Dictionary<string, string>());

    private readonly BuildPropertyOptions _global;

    /// <summary>Initializes a new instance of the <see cref="BuildPropertyOptionsProvider"/> class.</summary>
    /// <param name="global">The build's properties.</param>
    public BuildPropertyOptionsProvider(BuildPropertyOptions global) => _global = global;

    /// <inheritdoc />
    public override AnalyzerConfigOptions GlobalOptions => _global;

    /// <inheritdoc />
    public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => NoFileOptions;

    /// <inheritdoc />
    public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => NoFileOptions;
}
