using System.Collections.Generic;
using Fdw.Results;
using Fdw.Services.Connections.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.Endpoints;

/// <summary>The refusal a create or update gives when the schema discovery option it names is not one the connection type offers.</summary>
internal static class SchemaDiscoveryRefusal
{
    /// <summary>Builds the refusal, carrying the option given and the options offered so the caller can correct the request.</summary>
    /// <param name="logger">The logger the refusal is written to.</param>
    /// <param name="connectionName">The connection's name.</param>
    /// <param name="option">The option the request named; empty when it named none.</param>
    /// <param name="connectionType">The connection type's name.</param>
    /// <param name="offered">The names of the options the type offers.</param>
    /// <typeparam name="T">The result's value type.</typeparam>
    /// <returns>A <c>SchemaDiscoveryNotOffered</c> failure.</returns>
    internal static IGenericResult<T> NotOffered<T>(
        ILogger logger, string connectionName, string option, string connectionType, IEnumerable<string> offered)
        => GenericResult<T>.Failure(
            ConnectionsResultCodes.ByName("SchemaDiscoveryNotOffered"), logger,
            ResultDetails.Create("name", connectionName, "option", option, "connectionType", connectionType).With("offered", string.Join(", ", offered)));
}
