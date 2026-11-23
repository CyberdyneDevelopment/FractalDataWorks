using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.Calculations.Abstractions.Sets;
using Fdw.Services.Data.Clients.Models;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>The Each rows source: one row per row of a table, optionally the ones a condition keeps.</summary>
[TypeOption(typeof(RowSourceMappers), "Each", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class EachRowSourceMapper : RowSourceMapperBase
{
    /// <summary>Initializes a new instance of the <see cref="EachRowSourceMapper"/> class.</summary>
    public EachRowSourceMapper()
        : base("Each")
    {
    }

    /// <inheritdoc />
    public override IGenericResult<RowSourceDefinition> Map(string output, RowSourcePayload rows, int maxDepth, ILogger logger)
    {
        if (rows.Table is not { } table)
        {
            return NotStated(output, rows.Kind, logger);
        }

        if (rows.Count is not null || rows.IndexColumn is not null || rows.By is { Count: > 0 })
        {
            return Refused(output, "an Each source takes a table and optionally a where condition, not count, indexColumn or by", logger);
        }

        if (rows.Where is null)
        {
            return GenericResult<RowSourceDefinition>.Success(new EachRowSource(table, null));
        }

        var where = BuiltFormulaMapper.ToNode(string.Concat(output, "/where"), rows.Where, maxDepth, logger);
        return where.IsFailure ? where.ToNewResult<RowSourceDefinition>() : GenericResult<RowSourceDefinition>.Success(new EachRowSource(table, where.Value!));
    }
}
