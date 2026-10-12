using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Tds.Endpoints.Tests;

/// <summary>A failure in the Missing category, as an endpoint answers a table the caller cannot read.</summary>
internal sealed class TableMissingCode : ResultCodeBase
{
    public TableMissingCode()
        : base(30_901, "TableMissing", ResultSeverities.ByName("Error"), "No table {name} is readable", "TEST")
    {
    }
}
