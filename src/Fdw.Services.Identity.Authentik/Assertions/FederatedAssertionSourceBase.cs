using Fdw.Collections;
using Fdw.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Identity.Authentik.Assertions;

/// <summary>
/// Base class for federated assertion sources. The behavior — actually reading the assertion — lives
/// on the option, so adding a new carrier is a new TypeOption in its own assembly and touches no
/// existing code.
/// </summary>
public abstract class FederatedAssertionSourceBase : TypeOptionBase<int, FederatedAssertionSourceBase>, IFederatedAssertionSource
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FederatedAssertionSourceBase"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for this source.</param>
    /// <param name="name">The name of this source.</param>
    protected FederatedAssertionSourceBase(int id, string name) : base(id, name)
    {
    }

    /// <summary>
    /// Reads the assertion this source carries.
    /// </summary>
    /// <param name="configurationName">The identity configuration reading the assertion, for logging.</param>
    /// <param name="location">Where to look — an environment variable name, a file path.</param>
    /// <param name="logger">The logger that records an absent assertion.</param>
    /// <returns>
    /// The assertion, or a failure when it is absent. Absence is always a failure: a workload
    /// configured to federate has no other credential to fall back to, and continuing without one
    /// would produce an unauthenticated request whose rejection is harder to diagnose than this.
    /// </returns>
    public abstract IGenericResult<string> Read(string configurationName, string location, ILogger logger);
}
