namespace Fdw.Services.Connections.Abstractions;

/// <summary>Why a script did not parse (FDW-5457): where, and what the parser said.</summary>
/// <param name="Line">The line, from one.</param>
/// <param name="Column">The column, from one.</param>
/// <param name="Text">What the parser said.</param>
public sealed record ScriptParseError(int Line, int Column, string Text);
