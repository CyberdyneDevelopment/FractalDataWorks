using Fdw.ServiceTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Fdw.Conventions;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Settings;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.Endpoints;

/// <summary>
/// Fills each value a connection create request leaves out from the instance setting its configuration property names
/// with <see cref="DefaultSettingAttribute"/>, read for the caller's tenant (FDW-3580), through
/// <see cref="ConfigurationCreateDefaults"/>; a connection adds its field aliases to what counts as stated.
/// </summary>
/// <remarks>
/// Why on the request body and before binding: an omitted member is only visible there. Once the body is bound, an
/// unstated port or flag is a 0 or a false, indistinguishable from one the caller sent. What the connection type implies (a port, encrypt,
/// pooling) comes from the type option (FDW-3823, Mike: "the thing you select carries it"), which a host may change at registration; a choice
/// only the caller makes ([CallerChoice]) is refused when it is left out.
/// </remarks>
public static class ConnectionCreateDefaults
{
    /// <summary>Fills the omitted defaultable members of <paramref name="body"/>.</summary>
    /// <param name="body">The create request's configuration body.</param>
    /// <param name="behavior">The connection type's endpoint behavior: its configuration type and field aliases.</param>
    /// <param name="connectionType">The connection type option, which carries the tuning defaults of its configuration.</param>
    /// <param name="settings">The settings that hold the defaults.</param>
    /// <param name="tenantId">The caller's tenant, whose settings apply.</param>
    /// <param name="connectionName">The connection being created, for the messages.</param>
    /// <param name="logger">The logger.</param>
    /// <returns>The body with every defaultable member stated, or the first refusal.</returns>
    public static IGenericResult<JsonElement> Fill(
        JsonElement body, IConnectionEndpointBehavior behavior, IServiceType connectionType, Lazy<IEffectiveSettingsProvider> settings, Guid? tenantId,
        string connectionName, ILogger logger)
    {
        var filled = JsonNode.Parse(body.GetRawText())!.AsObject();
        var result = ConfigurationCreateDefaults.Fill(
            filled, behavior.ConfigurationType, connectionType, property => IsStated(filled, property, behavior.Aliases), settings, tenantId, "connection",
            connectionName, logger);
        return result.IsFailure
            ? result.ToNewResult<JsonElement>()
            : GenericResult<JsonElement>.Success(JsonSerializer.SerializeToElement(filled));
    }

    private static bool IsStated(JsonObject body, string property, IReadOnlyDictionary<string, string> aliases)
    {
        var names = aliases.Where(alias => string.Equals(alias.Value, property, StringComparison.OrdinalIgnoreCase))
            .Select(alias => alias.Key)
            .Append(property);
        return body.Any(member => member.Value is not null && names.Contains(member.Key, StringComparer.OrdinalIgnoreCase));
    }
}
