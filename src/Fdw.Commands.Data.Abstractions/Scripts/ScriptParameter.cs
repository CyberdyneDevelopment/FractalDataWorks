namespace Fdw.Commands.Data.Abstractions;

/// <summary>
/// A value a script is given for one of its named parameters (FDW-1435): <c>@Year</c> in the text takes the value of the
/// parameter named <c>Year</c>.
/// </summary>
/// <param name="Name">The parameter's name without the leading <c>@</c>: a letter or underscore, then letters, digits and underscores.</param>
/// <param name="Value">The value, in the CLR type the column or expression it meets expects; null is a SQL null.</param>
public sealed record ScriptParameter(string Name, object? Value);
