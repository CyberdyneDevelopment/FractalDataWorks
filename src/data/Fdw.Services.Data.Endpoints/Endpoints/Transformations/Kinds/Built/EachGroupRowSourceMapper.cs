using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.Calculations.Abstractions.Sets;
using Fdw.Services.Data.Clients.Models;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>The EachGroup rows source: one row per distinct combination of the by columns of a table.</summary>
[TypeOption(typeof(RowSourceMappers), "EachGroup", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class EachGroupRowSourceMapper : RowSourceMapperBase
{
    /// <summary>Initializes a new instance of the <see cref="EachGroupRowSourceMapper"/> class.</summary>
    public EachGroupRowSourceMapper()
        : base("EachGroup")
    {
    }

    /// <inheritdoc />
    public override IGenericResult<RowSourceDefinition> Map(string output, RowSourcePayload rows, int maxDepth, ILogger logger)
    {
        if (rows.Table is not { } table || rows.By is not { Count: > 0 } by)
        {
            return NotStated(output, rows.Kind, logger);
        }

        return rows.Where is not null || rows.Count is not null || rows.IndexColumn is not null
            ? Refused(output, "an EachGroup source takes a table and the by columns, not where, count or indexColumn", logger)
            : GenericResult<RowSourceDefinition>.Success(new EachGroupSource(table, [.. by]));
    }
}
