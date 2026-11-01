namespace Fdw.CodeBuilder.Analysis;

/// <summary>
/// Interface for syntax tree expectations and verification.
/// </summary>
public interface ISyntaxExpectations
{
    /// <summary>
    /// Creates expectations for generated code.
    /// </summary>
    /// <param name="generatedCode">The generated code as a string.</param>
    /// <returns>A syntax tree expectations instance.</returns>
    ISyntaxTreeExpectations ExpectCode(string generatedCode);

    /// <summary>
    /// Creates expectations for a syntax tree.
    /// </summary>
    /// <param name="syntaxTree">The syntax tree object.</param>
    /// <returns>A syntax tree expectations instance.</returns>
    ISyntaxTreeExpectations ExpectSyntaxTree(object syntaxTree);
}
