using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Fdw.Collections.SourceGenerators.Tests.Mocks;

internal static class MockGeneratorDriver
{
    public static (Compilation, ImmutableArray<Diagnostic>) RunWithMocks(
        Compilation compilation,
        string source)
    {
        var diagnostics = ImmutableArray<Diagnostic>.Empty;

        // Detect MutableTypeCollection
        var mutableMatches = Regex.Matches(source, @"\[MutableTypeCollection\(typeof\(([^)]+)\),\s*typeof\(([^)]+)\),\s*typeof\(([^)]+)\)");
        foreach (System.Text.RegularExpressions.Match match in mutableMatches)
        {
            var returnType = match.Groups[2].Value; // Second type param
            var className = match.Groups[3].Value;
            var mockCode = MockGeneratedCode.GenerateMutableCollection(className, returnType);
            compilation = compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(mockCode, path: $"{className}.g.cs"));
        }

        // Detect TypeInstanceCollection
        var factoryMatches = Regex.Matches(source, @"\[TypeInstanceCollection\(typeof\(([^)]+)\),\s*typeof\(([^)]+)\),\s*typeof\(([^)]+)\)");
        foreach (System.Text.RegularExpressions.Match match in factoryMatches)
        {
            var returnType = match.Groups[2].Value;
            var className = match.Groups[3].Value;
            var options = ExtractOptionNames(source);
            var mockCode = MockGeneratedCode.GenerateFactoryCollection(className, returnType, options);
            compilation = compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(mockCode, path: $"{className}.g.cs"));
        }

        return (compilation, diagnostics);
    }

    private static string[] ExtractOptionNames(string source)
    {
        var names = new System.Collections.Generic.List<string>();
        var matches = Regex.Matches(source, @"\[TypeOption\([^,]+,\s*""([^""]+)""\)");

        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            if (match.Success)
                names.Add(match.Groups[1].Value);
        }

        return names.ToArray();
    }
}
