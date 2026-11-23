using Fdw.Collections;
using Fdw.Results;
using Fdw.Services.Calculations.Abstractions.Sets;
using Fdw.Services.Data.Clients.Models;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>Turns the output column a request states, of one kind, into its definition.</summary>
public interface IOutputColumnMapper : ITypeOption<int, OutputColumnMapperBase>
{
    /// <summary>Maps the payload, or refuses what does not belong to this kind.</summary>
    /// <param name="output">The output the column is in.</param>
    /// <param name="column">The column the request states.</param>
    /// <param name="maxDepth">The deepest formula the set allows.</param>
    /// <param name="logger">The logger a refusal is written to.</param>
    /// <returns>The definition, or the refusal.</returns>
    IGenericResult<OutputColumnDefinition> Map(string output, OutputColumnPayload column, int maxDepth, ILogger logger);
}
