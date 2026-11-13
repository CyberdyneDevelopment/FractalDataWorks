using System.Collections.Generic;

namespace Fdw.Services.Etl.Flow;

/// <summary>A script whose <c>$(Name)</c> tokens are replaced by identifiers that stand for the names given at run time (FDW-5457).</summary>
/// <param name="Text">The script with each token replaced.</param>
/// <param name="Markers">The identifiers that replaced the tokens, one for each token, in the order they appear.</param>
public sealed record ScriptTokenMarks(string Text, IReadOnlyList<string> Markers);
