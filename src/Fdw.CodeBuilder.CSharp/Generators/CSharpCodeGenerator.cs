using System.Collections.Generic;
using System.Text;
using Fdw.CodeBuilder.Abstractions;

namespace Fdw.CodeBuilder.CSharp.Generators;

/// <summary>
/// Code generator for C# language.
/// </summary>
public sealed class CSharpCodeGenerator : ICodeGenerator
{
    /// <inheritdoc/>
    public string TargetLanguage => "csharp";

    /// <inheritdoc/>
    public string Generate(ISyntaxTree syntaxTree)
    {
        // This would transform a syntax tree back into C# code
        // For now, return the original source text
        return syntaxTree.SourceText;
    }

    /// <inheritdoc/>
    public string Generate(IClassBuilder classBuilder)
    {
        return classBuilder.Build();
    }

    /// <inheritdoc/>
    public string Generate(IInterfaceBuilder interfaceBuilder)
    {
        return interfaceBuilder.Build();
    }

    /// <inheritdoc/>
    public string Generate(IEnumBuilder enumBuilder)
    {
        return enumBuilder.Build();
    }

    /// <inheritdoc/>
    public string GenerateCompilationUnit(IEnumerable<ICodeBuilder> builders)
    {
        var sb = new StringBuilder();
        var first = true;

        foreach (var builder in builders)
        {
            if (!first)
            {
                sb.AppendLine();
                sb.AppendLine();
            }

            sb.Append(builder.Build());
            first = false;
        }

        return sb.ToString();
    }
}
