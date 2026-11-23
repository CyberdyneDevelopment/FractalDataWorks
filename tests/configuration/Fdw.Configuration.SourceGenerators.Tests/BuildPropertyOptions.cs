using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Fdw.Configuration.SourceGenerators.Tests;

/// <summary>The MSBuild properties a build hands a generator, as <c>build_property.*</c> options (FDW-7107).</summary>
internal sealed class BuildPropertyOptions : AnalyzerConfigOptions
{
    private readonly IReadOnlyDictionary<string, string> _properties;

    /// <summary>Initializes a new instance of the <see cref="BuildPropertyOptions"/> class.</summary>
    /// <param name="properties">The properties by MSBuild name, without the <c>build_property.</c> prefix.</param>
    public BuildPropertyOptions(IReadOnlyDictionary<string, string> properties) => _properties = properties;

    /// <inheritdoc />
    public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value)
    {
        const string Prefix = "build_property.";
        value = null;
        return key.StartsWith(Prefix, StringComparison.Ordinal) && _properties.TryGetValue(key.Substring(Prefix.Length), out value);
    }
}
