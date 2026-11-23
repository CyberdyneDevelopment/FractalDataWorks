using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Collections;
using Fdw.Results;
using Fdw.Services.Calculations.Abstractions.Sets;
using Fdw.Services.Data.Clients.Models;
using Fdw.Services.Data.Endpoints.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>The base of every output column mapper: a kind refuses the members that are not its own, then makes its definition.</summary>
[ExcludeFromCodeCoverage]
public abstract class OutputColumnMapperBase : TypeOptionBase<OutputColumnMapperBase>, IOutputColumnMapper
{
    /// <summary>Initializes a new instance of the <see cref="OutputColumnMapperBase"/> class.</summary>
    /// <param name="name">The kind's name, the name a column is stored and chosen by.</param>
    protected OutputColumnMapperBase(string name)
        : base(name)
    {
    }

    /// <inheritdoc />
    public IGenericResult<OutputColumnDefinition> Map(string output, OutputColumnPayload column, int maxDepth, ILogger logger)
    {
        var problem = Problem(column);
        return problem is null ? Make(output, column, maxDepth, logger) : Refused(output, column, problem, logger);
    }

    /// <summary>Refuses a column, naming the output, the column and the reason.</summary>
    /// <param name="output">The output the column is in.</param>
    /// <param name="column">The column the request states.</param>
    /// <param name="reason">What is wrong, in a sentence.</param>
    /// <param name="logger">The logger the refusal is written to.</param>
    /// <returns>The failed result.</returns>
    public static IGenericResult<OutputColumnDefinition> Refused(string output, OutputColumnPayload column, string reason, ILogger logger) =>
        GenericResult<OutputColumnDefinition>.Failure(BuiltTransformationEndpointLog.OutputInvalid(logger, output, $"column '{column.Name}': {reason}"));

    /// <summary>Names the members of the payload that this kind does not take.</summary>
    /// <param name="column">The column the request states.</param>
    /// <returns>The reason, or null when the column states only what this kind takes.</returns>
    public abstract string? Problem(OutputColumnPayload column);

    /// <summary>Makes the definition of a column that has no problem.</summary>
    /// <param name="output">The output the column is in.</param>
    /// <param name="column">The column the request states.</param>
    /// <param name="maxDepth">The deepest formula the set allows.</param>
    /// <param name="logger">The logger a refusal is written to.</param>
    /// <returns>The definition, or the refusal.</returns>
    public abstract IGenericResult<OutputColumnDefinition> Make(string output, OutputColumnPayload column, int maxDepth, ILogger logger);

    /// <summary>Refuses a column that states a member this kind does not take.</summary>
    /// <param name="takes">What the kind takes.</param>
    /// <param name="others">The members the kind does not take.</param>
    /// <returns>The reason, or null when none of the members is stated.</returns>
    protected string? OnlyTakes(string takes, params object?[] others) =>
        others.Any(other => other is not null) ? $"a {Name} column takes only {takes}" : null;
}
