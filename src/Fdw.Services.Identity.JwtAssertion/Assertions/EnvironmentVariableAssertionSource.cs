using System;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.Identity.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Identity.JwtAssertion.Assertions;

/// <summary>
/// Reads a federated assertion from an environment variable. This is how a CI CI delivers a job's
/// <c>id_tokens</c>, which is the motivating case for federated identity.
/// </summary>
[TypeOption(typeof(FederatedAssertionSources), "EnvironmentVariable")]
public sealed class EnvironmentVariableAssertionSource : FederatedAssertionSourceBase
{
    /// <summary>Initializes a new instance of the <see cref="EnvironmentVariableAssertionSource"/> class.</summary>
    public EnvironmentVariableAssertionSource() : base(1, "EnvironmentVariable")
    {
    }

    /// <inheritdoc/>
    public override IGenericResult<string> Read(string configurationName, string location, ILogger logger)
        => string.IsNullOrWhiteSpace(location) || Environment.GetEnvironmentVariable(location) is not { Length: > 0 } assertion
            ? GenericResult<string>.Failure(IdentityLog.AssertionNotAvailable(logger, configurationName, Name, location))
            : GenericResult<string>.Success(assertion);
}
