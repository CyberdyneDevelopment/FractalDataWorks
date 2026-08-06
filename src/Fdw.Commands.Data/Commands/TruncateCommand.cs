using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Commands.Data.Abstractions;

namespace Fdw.Commands.Data;

/// <summary>
/// Truncate command for emptying a container (removes ALL records). Returns the number of affected rows.
/// </summary>
/// <remarks>
/// <para>
/// Unlike <see cref="DeleteCommand"/> — which REQUIRES a filter so an accidental WHERE-less delete is
/// impossible — <see cref="TruncateCommand"/> is the explicit "empty this container" intent (used by
/// pipeline <c>TruncateBeforeLoad</c>). Translators convert it to an unconditional removal:
/// <list type="bullet">
/// <item>SQL: <c>DELETE FROM &lt;table&gt;</c> with no WHERE clause. DELETE — NOT <c>TRUNCATE TABLE</c> —
/// so it works with a plain DELETE grant and on tables with FK references, requiring no ALTER/DDL permission.</item>
/// <item>REST/File: remove all records in the target collection.</item>
/// </list>
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataCommands), "Truncate", RestrictToCurrentCompilation = true)]
public sealed class TruncateCommand : DataCommandBase<int>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TruncateCommand"/> class.
    /// </summary>
    public TruncateCommand()
        : base("Truncate")
    {
    }
}
