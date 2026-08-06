using System.Collections.Generic;

namespace Fdw.UI.Services.Formula;

/// <summary>
/// Tokenizes formula expressions for syntax highlighting.
/// </summary>
public interface IFormulaTokenizer
{
    /// <summary>
    /// Tokenizes a formula string.
    /// </summary>
    IEnumerable<Token> Tokenize(string formula);
}
