using System.Text;
using Microsoft.AspNetCore.Razor.Language;

namespace Fdw.UI.Components.RazorConsole.SourceGenerators.CodeGenerators;

/// <summary>
/// Generates the Prompt() method from Razor markup.
/// Converts declarative markup to Spectre.Console prompts.
/// </summary>
internal sealed class PromptMethodGenerator
{
    public static string Generate(RazorCodeDocument document)
    {
        var builder = new StringBuilder();

        builder.AppendLine("    public override async Task<object?> Prompt(IAnsiConsole console)");
        builder.AppendLine("    {");
        builder.AppendLine("        Console = console;");
        builder.AppendLine("        await OnInitializedAsync();");
        builder.AppendLine();

        // Parse markup and generate prompts
        GeneratePrompts(document, builder);

        builder.AppendLine();
        builder.AppendLine("        return Model;");
        builder.AppendLine("    }");

        return builder.ToString();
    }

    private static void GeneratePrompts(RazorCodeDocument document, StringBuilder builder)
    {
        // Parse <Panel>, <Property>, <Collection> tags
        // Generate corresponding Spectre.Console prompt code

        // Example for <Panel Title="Configuration">
        builder.AppendLine("        // Generated prompts will be populated from Razor markup");
        builder.AppendLine("        await OnParametersSetAsync();");
    }
}
