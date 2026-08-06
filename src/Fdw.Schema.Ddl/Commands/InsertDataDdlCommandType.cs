using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Schema.Ddl.Commands;

/// <summary>Inserts data into a table (for metadata and lookup values).</summary>
[TypeOption(typeof(DdlCommandTypes), "InsertData")]
[ExcludeFromCodeCoverage]
public sealed class InsertDataDdlCommandType : DdlCommandTypeBase
{
    /// <summary>Initializes a new instance of <see cref="InsertDataDdlCommandType"/>.</summary>
    public InsertDataDdlCommandType() : base(10, "InsertData") { }
}
