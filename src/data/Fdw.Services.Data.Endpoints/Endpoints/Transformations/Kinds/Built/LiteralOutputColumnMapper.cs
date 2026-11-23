using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.Calculations.Abstractions.Sets;
using Fdw.Services.Data.Clients.Models;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>The Literal column: holds the same stated value in every row.</summary>
[TypeOption(typeof(OutputColumnMappers), "Literal", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class LiteralOutputColumnMapper : OutputColumnMapperBase
{
    /// <summary>Initializes a new instance of the <see cref="LiteralOutputColumnMapper"/> class.</summary>
    public LiteralOutputColumnMapper()
        : base("Literal")
    {
    }

    /// <inheritdoc />
    public override string? Problem(OutputColumnPayload column) =>
        OnlyTakes("literalType and literalValue", column.Source, column.Formula);

    /// <inheritdoc />
    public override IGenericResult<OutputColumnDefinition> Make(string output, OutputColumnPayload column, int maxDepth, ILogger logger)
    {
        if (column.LiteralValue is null)
        {
            return Refused(output, column, "a Literal column states its literalValue", logger);
        }

        var literalType = column.LiteralType is { } typeName ? LiteralTypes.ByName(typeName) : LiteralTypes.NotFound;
        var literal = ReferenceEquals(literalType, LiteralTypes.NotFound) ? null : literalType.Read(column.LiteralValue);
        return literal is null
            ? Refused(output, column, $"'{column.LiteralValue}' is not a {column.LiteralType}; a literal is Text, Number or Flag", logger)
            : GenericResult<OutputColumnDefinition>.Success(new LiteralColumn(column.Name, literal));
    }
}
