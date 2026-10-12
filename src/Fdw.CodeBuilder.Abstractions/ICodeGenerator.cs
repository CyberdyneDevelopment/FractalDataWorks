using System.Collections.Generic;

namespace Fdw.CodeBuilder.Abstractions;

/// <summary>
/// Interface for code generators that transform syntax trees or definitions into source code.
/// </summary>
public interface ICodeGenerator
{
    /// <summary>
    /// Gets the target language for this generator.
    /// </summary>
    string TargetLanguage { get; }

    /// <summary>
    /// Generates code from a syntax tree.
    /// </summary>
    /// <param name="syntaxTree">The syntax tree to generate from.</param>
    /// <returns>The generated source code.</returns>
    string Generate(ISyntaxTree syntaxTree);

    /// <summary>
    /// Generates code from a class builder.
    /// </summary>
    /// <param name="classBuilder">The class builder.</param>
    /// <returns>The generated source code.</returns>
    string Generate(IClassBuilder classBuilder);

    /// <summary>
    /// Generates code from an interface builder.
    /// </summary>
    /// <param name="interfaceBuilder">The interface builder.</param>
    /// <returns>The generated source code.</returns>
    string Generate(IInterfaceBuilder interfaceBuilder);

    /// <summary>
    /// Generates code from an enum builder.
    /// </summary>
    /// <param name="enumBuilder">The enum builder.</param>
    /// <returns>The generated source code.</returns>
    string Generate(IEnumBuilder enumBuilder);

    /// <summary>
    /// Generates a compilation unit with multiple types.
    /// </summary>
    /// <param name="builders">The builders for the types to include.</param>
    /// <returns>The generated source code.</returns>
    string GenerateCompilationUnit(IEnumerable<ICodeBuilder> builders);
}
