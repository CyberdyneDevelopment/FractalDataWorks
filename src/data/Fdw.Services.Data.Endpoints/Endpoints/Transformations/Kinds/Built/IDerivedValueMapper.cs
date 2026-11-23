using Fdw.Collections;
using Fdw.Results;
using Fdw.Services.Calculations.Abstractions.Sets;
using Fdw.Services.Data.Clients.Models;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>Turns the derived value a request states, of one kind, into its definition.</summary>
public interface IDerivedValueMapper : ITypeOption<int, DerivedValueMapperBase>
{
    /// <summary>Maps the payload, or refuses what does not belong to this kind.</summary>
    /// <param name="value">The derived value the request states.</param>
    /// <param name="logger">The logger a refusal is written to.</param>
    /// <returns>The definition, or the refusal.</returns>
    IGenericResult<DerivedValueDefinition> Map(DerivedValuePayload value, ILogger logger);
}
