using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using Fdw.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.Abstractions;

/// <summary>
/// How a connection type reads, updates and shows its configuration through the connection endpoints.
/// Speaks JSON and configuration types only -- no endpoint request or response types -- so it lives
/// here, beside <see cref="IConnectionType"/>, which exposes it as <see cref="IConnectionType.EndpointBehavior"/>.
/// </summary>
/// <remarks>
/// <para>
/// The members here take a configuration as input, so they stay non-generic. The member that produces a
/// configuration from JSON, <see cref="IConnectionEndpointBehavior{TConfiguration}.Bind"/>, is on the
/// covariant producer interface, so an endpoint holds one
/// <c>IConnectionEndpointBehavior&lt;IConnectionImplementationConfiguration&gt;</c> view of every type.
/// </para>
/// <para>
/// Secrets: a property marked <see cref="ConnectionSecretAttribute"/>, and every entry of a string
/// dictionary property, is shown as <see cref="ConnectionSecretMask.Value"/> unless the connection type
/// says which entries are safe to show. Sending the mask back in a merge keeps the stored value.
/// </para>
/// </remarks>
public interface IConnectionEndpointBehavior
{
    /// <summary>Gets the configuration class a body binds into: the connection type's own configuration.</summary>
    Type ConfigurationType { get; }

    /// <summary>
    /// Gets the deprecated flat request field names this type still accepts, mapped to the configuration
    /// property each one sets (for example <c>baseUrl</c> -&gt; <c>Root</c>). A field whose name already
    /// matches a property needs no alias.
    /// </summary>
    IReadOnlyDictionary<string, string> Aliases { get; }

    /// <summary>
    /// Applies a partial JSON object to a stored configuration: members present in
    /// <paramref name="patch"/> replace, nested objects and dictionaries merge, and a value equal to
    /// <see cref="ConnectionSecretMask.Value"/> keeps what is stored.
    /// </summary>
    /// <param name="patch">The changed members, as a JSON object.</param>
    /// <param name="existing">The configuration as stored.</param>
    /// <param name="logger">The caller's logger, for the failure messages.</param>
    IGenericResult<IConnectionImplementationConfiguration> Merge(
        JsonElement patch, IConnectionImplementationConfiguration existing, ILogger logger);

    /// <summary>
    /// Shows a configuration as JSON for a detail response: this type's own settings only (the members
    /// every connection shares, such as its name and health-check settings, are left out), with secrets
    /// masked.
    /// </summary>
    /// <param name="configuration">The configuration to show.</param>
    /// <param name="logger">The caller's logger, for the failure messages.</param>
    IGenericResult<JsonNode> Project(IConnectionImplementationConfiguration configuration, ILogger logger);
}

/// <summary>
/// The producer half of <see cref="IConnectionEndpointBehavior"/>: builds a
/// <typeparamref name="TConfiguration"/> from JSON. Covariant, so every connection type's behavior is also an
/// <c>IConnectionEndpointBehavior&lt;IConnectionImplementationConfiguration&gt;</c>.
/// </summary>
/// <typeparam name="TConfiguration">The connection type's configuration class.</typeparam>
public interface IConnectionEndpointBehavior<out TConfiguration> : IConnectionEndpointBehavior
    where TConfiguration : IConnectionImplementationConfiguration
{
    /// <summary>
    /// Reads a create body into this type's configuration. A member left out, or sent as null, takes the
    /// configuration's own default.
    /// </summary>
    /// <param name="configuration">The type's settings, as a JSON object.</param>
    /// <param name="logger">The caller's logger, for the failure messages.</param>
    IGenericResult<TConfiguration> Bind(JsonElement configuration, ILogger logger);
}
