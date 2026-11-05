using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Fdw.Roslyn.Commands.Symbols;

/// <summary>
/// The symbol a source token names: the symbol it declares, or the symbol it refers to.
/// </summary>
/// <remarks>
/// Why the syntax decides: a token that refers to a symbol sits in a name, which is an expression
/// (IdentifierNameSyntax and the other names), and a token that declares one is the identifier of its declaration
/// (a type, member, variable, parameter). Each translator used to ask GetSymbolInfo and fall back to
/// GetDeclaredSymbol; the token's own syntax says which question applies, so one is asked (FDW-3580).
/// </remarks>
internal static class TokenSymbols
{
    /// <summary>
    /// Gets the symbol <paramref name="token"/> declares or refers to.
    /// </summary>
    /// <param name="semanticModel">The semantic model of the token's document.</param>
    /// <param name="token">The token at the command's position.</param>
    /// <param name="cancellationToken">Cancels the lookup.</param>
    /// <returns>The symbol, or <see langword="null"/> when the token names none; the caller fails with its own message.</returns>
    public static ISymbol? Resolve(SemanticModel semanticModel, SyntaxToken token, CancellationToken cancellationToken = default) =>
        token.Parent switch
        {
            null => null,
            ExpressionSyntax reference => semanticModel.GetSymbolInfo(reference, cancellationToken).Symbol,
            { } declaration => semanticModel.GetDeclaredSymbol(declaration, cancellationToken),
        };
}
