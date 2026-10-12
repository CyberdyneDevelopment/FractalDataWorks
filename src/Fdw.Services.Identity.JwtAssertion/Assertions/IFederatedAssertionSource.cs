using Fdw.Collections;
using Fdw.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Identity.JwtAssertion.Assertions;

/// <summary>
/// Interface for the ways a federated assertion can reach this process.
/// </summary>
/// <remarks>
/// <see cref="Read"/> is declared here, not only on the base class, because <c>ByName</c> hands back
/// this interface — behavior a caller cannot invoke through the type the lookup returns would force
/// every call site to downcast, which is the dispatch this collection exists to avoid.
/// </remarks>
public interface IFederatedAssertionSource : ITypeOption<int, FederatedAssertionSourceBase>
{
    /// <summary>
    /// Reads the assertion this source carries.
    /// </summary>
    /// <param name="configurationName">The identity configuration reading the assertion, for logging.</param>
    /// <param name="location">Where to look — an environment variable name, a file path.</param>
    /// <param name="logger">The logger that records an absent assertion.</param>
    /// <returns>The assertion, or a failure when it is absent.</returns>
    IGenericResult<string> Read(string configurationName, string location, ILogger logger);
}
