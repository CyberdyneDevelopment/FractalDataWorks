using System;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Fdw.UI.Components.RazorConsole.SourceGenerators;

/// <summary>
/// Compiles Razor .cshtml files to C# code that renders Spectre.Console output.
/// </summary>
public sealed class RazorCompiler
{
    /// <summary>
    /// Compiles a Razor file to C# code.
    /// </summary>
    /// <param name="file">The additional text file to compile.</param>
    /// <returns>The generated C# code, or null if compilation failed.</returns>
    public static string? Compile(AdditionalText file)
    {
        var text = file.GetText()?.ToString();
        if (text == null) return null;

        // Parse Razor syntax
        var document = ParseRazor(text, file.Path);

        // Generate C# code
        return CodeGenerator.Generate(document);
    }

    private static RazorCodeDocument ParseRazor(string content, string filePath)
    {
        // Create Razor engine with default configuration
        var fileSystem = RazorProjectFileSystem.Create(".");
        var engine = RazorProjectEngine.Create(RazorConfiguration.Default, fileSystem);

        // Create source document
        var sourceDocument = RazorSourceDocument.Create(content, filePath);

        // Create a code document by wrapping the source document
        var codeDocument = RazorCodeDocument.Create(sourceDocument);

        // Parse using the engine's pipeline
        engine.Engine.Process(codeDocument);

        return codeDocument;
    }
}