using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.AspNetCore.Razor.Language;

namespace Fdw.UI.Components.RazorConsole.SourceGenerators;

/// <summary>
/// Source generator that compiles .cshtml files to Spectre.Console rendering code.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class RazorConsoleSourceGenerator : IIncrementalGenerator
{
    /// <summary>
    /// Initializes the source generator to find and compile Razor files.
    /// </summary>
    /// <param name="context">The incremental generator initialization context.</param>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Find all .cshtml files
        var razorFiles = context.AdditionalTextsProvider
            .Where(static file => file.Path.EndsWith(".cshtml", StringComparison.OrdinalIgnoreCase));

        // Compile each Razor file
        context.RegisterSourceOutput(razorFiles, static (context, file) =>
        {
            var result = RazorCompiler.Compile(file);

            if (result != null)
            {
                var fileName = System.IO.Path.GetFileNameWithoutExtension(file.Path);
                context.AddSource($"{fileName}.g.cs", SourceText.From(result, Encoding.UTF8));
            }
        });
    }
}
