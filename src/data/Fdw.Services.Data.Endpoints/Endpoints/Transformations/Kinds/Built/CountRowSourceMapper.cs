using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.Calculations.Abstractions.Sets;
using Fdw.Services.Data.Clients.Models;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>The Count rows source: as many rows as a count formula says, numbered in an index column.</summary>
[TypeOption(typeof(RowSourceMappers), "Count", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class CountRowSourceMapper : RowSourceMapperBase
{
    /// <summary>Initializes a new instance of the <see cref="CountRowSourceMapper"/> class.</summary>
    public CountRowSourceMapper()
        : base("Count")
    {
    }

    /// <inheritdoc />
    public override IGenericResult<RowSourceDefinition> Map(string output, RowSourcePayload rows, int maxDepth, ILogger logger)
    {
        if (rows.Count is not { } count || string.IsNullOrWhiteSpace(rows.IndexColumn))
        {
            return NotStated(output, rows.Kind, logger);
        }

        if (rows.Where is not null || rows.By is { Count: > 0 })
        {
            return Refused(output, "a Count source takes a count formula and an indexColumn, and optionally a table, not where or by", logger);
        }

        var node = BuiltFormulaMapper.ToNode(string.Concat(output, "/count"), count, maxDepth, logger);
        return node.IsFailure ? node.ToNewResult<RowSourceDefinition>() : GenericResult<RowSourceDefinition>.Success(new CountSource(node.Value!, rows.IndexColumn!, rows.Table));
    }
}
