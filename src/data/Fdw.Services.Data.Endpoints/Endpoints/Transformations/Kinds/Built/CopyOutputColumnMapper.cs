using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.Calculations.Abstractions.Sets;
using Fdw.Services.Data.Clients.Models;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>The Copy column: carries a visible value through under the column's name.</summary>
[TypeOption(typeof(OutputColumnMappers), "Copy", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class CopyOutputColumnMapper : OutputColumnMapperBase
{
    /// <summary>Initializes a new instance of the <see cref="CopyOutputColumnMapper"/> class.</summary>
    public CopyOutputColumnMapper()
        : base("Copy")
    {
    }

    /// <inheritdoc />
    public override string? Problem(OutputColumnPayload column) =>
        OnlyTakes("a source", column.LiteralType, column.LiteralValue, column.Formula);

    /// <inheritdoc />
    public override IGenericResult<OutputColumnDefinition> Make(string output, OutputColumnPayload column, int maxDepth, ILogger logger) =>
        string.IsNullOrWhiteSpace(column.Source)
            ? Refused(output, column, "a Copy column states its source", logger)
            : GenericResult<OutputColumnDefinition>.Success(new CopyColumn(column.Name, column.Source));
}
