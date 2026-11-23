using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Results;
using Fdw.Services.Calculations.Abstractions.Sets;
using Fdw.Services.Data.Clients.Models;
using Fdw.Services.Data.Endpoints.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>The base of every derived value mapper: a kind refuses what is not its own, then makes its definition.</summary>
[ExcludeFromCodeCoverage]
public abstract class DerivedValueMapperBase : TypeOptionBase<DerivedValueMapperBase>, IDerivedValueMapper
{
    /// <summary>Initializes a new instance of the <see cref="DerivedValueMapperBase"/> class.</summary>
    /// <param name="name">The kind's name, the name a derived value is stored and chosen by.</param>
    protected DerivedValueMapperBase(string name)
        : base(name)
    {
    }

    /// <inheritdoc />
    public IGenericResult<DerivedValueDefinition> Map(DerivedValuePayload value, ILogger logger)
    {
        var problem = Problem(value);
        return problem is null
            ? GenericResult<DerivedValueDefinition>.Success(Make(value))
            : GenericResult<DerivedValueDefinition>.Failure(BuiltTransformationEndpointLog.DerivedInvalid(logger, value.Name, problem));
    }

    /// <summary>Names what is wrong with the payload for this kind.</summary>
    /// <param name="value">The derived value the request states.</param>
    /// <returns>The reason, or null when the payload is right for this kind.</returns>
    public abstract string? Problem(DerivedValuePayload value);

    /// <summary>Makes the definition of a payload that has no problem.</summary>
    /// <param name="value">The derived value the request states.</param>
    /// <returns>The definition.</returns>
    public abstract DerivedValueDefinition Make(DerivedValuePayload value);
}
