using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.Calculations.Abstractions.Sets;
using Fdw.Services.Data.Clients.Models;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>The Formula column: runs a formula against each row.</summary>
[TypeOption(typeof(OutputColumnMappers), "Formula", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class FormulaOutputColumnMapper : OutputColumnMapperBase
{
    /// <summary>Initializes a new instance of the <see cref="FormulaOutputColumnMapper"/> class.</summary>
    public FormulaOutputColumnMapper()
        : base("Formula")
    {
    }

    /// <inheritdoc />
    public override string? Problem(OutputColumnPayload column) =>
        OnlyTakes("a formula", column.Source, column.LiteralType, column.LiteralValue);

    /// <inheritdoc />
    public override IGenericResult<OutputColumnDefinition> Make(string output, OutputColumnPayload column, int maxDepth, ILogger logger)
    {
        if (column.Formula is null)
        {
            return Refused(output, column, "a Formula column states its formula", logger);
        }

        var node = BuiltFormulaMapper.ToNode(string.Concat(output, "/", column.Name), column.Formula, maxDepth, logger);
        return node.IsFailure ? node.ToNewResult<OutputColumnDefinition>() : GenericResult<OutputColumnDefinition>.Success(new FormulaColumn(column.Name, node.Value!));
    }
}
