using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Results;
using Fdw.Services.Calculations.Abstractions.Sets;
using Fdw.Services.Data.Clients.Models;
using Fdw.Services.Data.Endpoints.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>The base of every rows source mapper.</summary>
[ExcludeFromCodeCoverage]
public abstract class RowSourceMapperBase : TypeOptionBase<RowSourceMapperBase>, IRowSourceMapper
{
    /// <summary>Initializes a new instance of the <see cref="RowSourceMapperBase"/> class.</summary>
    /// <param name="name">The kind's name, the name a rows source is stored and chosen by.</param>
    protected RowSourceMapperBase(string name)
        : base(name)
    {
    }

    /// <inheritdoc />
    public abstract IGenericResult<RowSourceDefinition> Map(string output, RowSourcePayload rows, int maxDepth, ILogger logger);

    /// <summary>Refuses a rows source, naming the output and the reason.</summary>
    /// <param name="output">The output the rows source is for.</param>
    /// <param name="reason">What is wrong, in a sentence.</param>
    /// <param name="logger">The logger the refusal is written to.</param>
    /// <returns>The failed result.</returns>
    public static IGenericResult<RowSourceDefinition> Refused(string output, string reason, ILogger logger) =>
        GenericResult<RowSourceDefinition>.Failure(BuiltTransformationEndpointLog.OutputInvalid(logger, output, reason));

    /// <summary>Refuses a rows source whose kind is not one of the kinds, or whose members do not state what its kind needs.</summary>
    /// <param name="output">The output the rows source is for.</param>
    /// <param name="kind">The kind the request states.</param>
    /// <param name="logger">The logger the refusal is written to.</param>
    /// <returns>The failed result.</returns>
    public static IGenericResult<RowSourceDefinition> NotStated(string output, string kind, ILogger logger) =>
        Refused(output, $"the rows source '{kind}' is not Each (with a table), Count (with a count formula and an indexColumn) or EachGroup (with a table and by columns)", logger);
}
