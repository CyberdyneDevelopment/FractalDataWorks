using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.Endpoints;

/// <summary>
/// Builds the connection detail response from a connection type's own projection of its configuration
/// (<see cref="IConnectionEndpointBehavior.Project"/>). Works for every connection type; there is no
/// per-type mapping.
/// </summary>
/// <remarks>
/// <see cref="ConnectionDetailDto.Configuration"/> carries the projection as it is. The deprecated flat
/// fields (Server, Database, BaseUrl, Authentication, ...) are filled from it for existing clients: each
/// reads the configuration property of the same name, or the one the connection type aliases that field to
/// (<c>baseUrl</c> -&gt; <c>Root</c>). A type without that setting reports it empty or unset, as before.
/// </remarks>
public static class ConnectionDetailMapper
{
    /// <summary>Maps a connection configuration to its detail response.</summary>
    /// <param name="connectionType">The connection's type.</param>
    /// <param name="connection">The connection's configuration.</param>
    /// <param name="logger">The caller's logger, for the failure messages.</param>
    /// <remarks>
    /// <c>CreatedAt</c>/<c>UpdatedAt</c> are the connection's own <c>CreateDate</c>/<c>ModifyDate</c>. A caller that has just
    /// saved the connection states the save's time itself, as the instance it saved has not been read back. Leaves
    /// <c>SchemaDiscovery</c> for the caller: it depends on which operation is mapping, not on the connection type.
    /// </remarks>
    public static IGenericResult<ConnectionDetailDto> Map(
        IConnectionType connectionType, IConnectionImplementationConfiguration connection, ILogger logger)
    {
        var behavior = connectionType.EndpointBehavior;
        var projected = behavior.Project(connection, logger);
        if (projected.IsFailure)
            return projected.ToNewResult<ConnectionDetailDto>();

        var shown = projected.Value!.AsObject();
        var legacy = new LegacyFields(shown, behavior.Aliases);

        return GenericResult<ConnectionDetailDto>.Success(new ConnectionDetailDto
        {
            Id = connection.Id,
            Name = connection.Name,
            ServiceType = connectionType.Name,
            Server = legacy.Text("server"),
            Port = legacy.Number("port"),
            Database = legacy.Text("database"),
            AuthenticationType = legacy.OptionalText("authenticationType"),
            Authentication = legacy.Entries("authentication"),
            TrustServerCertificate = legacy.Flag("trustServerCertificate"),
            Encrypt = legacy.Flag("encrypt"),
            BaseUrl = legacy.OptionalText("baseUrl"),
            Protocol = legacy.OptionalText("protocol"),
            TimeoutSeconds = legacy.OptionalNumber("timeoutSeconds"),
            SecurityType = legacy.OptionalText("securityType"),
            UseMtls = legacy.OptionalFlag("useMtls"),
            DefaultValuePolicy = legacy.OptionalText("defaultValuePolicy"),
            HealthCheckEnabled = connection.HealthCheckEnabled,
            HealthCheckOnStartup = connection.HealthCheckOnStartup,
            HealthCheckIntervalSeconds = connection.HealthCheckIntervalSeconds,
            IsActive = true,
            CreatedAt = connection.CreateDate,
            UpdatedAt = connection.ModifyDate,
            Configuration = shown,
            Credentials = CredentialStatus(connectionType, connection),
        });
    }

    // Which credential fields have a stored credential; never a value. Undetermined (null) when the configuration does
    // not say which authentication method it uses.
    private static List<ConnectionCredentialStatus>? CredentialStatus(
        IConnectionType connectionType, IConnectionImplementationConfiguration connection)
    {
        var slots = connectionType.CredentialSlots(connection);
        if (slots.IsFailure)
            return null;

        return slots.Value!.Credentials
            .Select(credential => new ConnectionCredentialStatus
            {
                Field = credential.Field,
                Label = credential.Label,
                Secret = credential.Secret,
                IsSet = slots.Value.IsSet(credential.Field),
            })
            .ToList();
    }

    /// <summary>Reads a deprecated flat field from a projection, through the connection type's aliases.</summary>
    private sealed class LegacyFields(JsonObject shown, IReadOnlyDictionary<string, string> aliases)
    {
        // Why string.Empty for a missing text field: Server and Database are required on the response, and a
        // type with no such setting (an Http or file-system connection) has always reported them empty.
        public string Text(string field) => OptionalText(field) is { } text ? text : string.Empty;

        public string? OptionalText(string field) =>
            Value(field) is JsonValue value && value.TryGetValue<string>(out var text) ? text : null;

        public int Number(string field) => OptionalNumber(field) is { } number ? number : 0;

        public int? OptionalNumber(string field) =>
            Value(field) is JsonValue value && value.TryGetValue<int>(out var number) ? number : null;

        public bool Flag(string field) => OptionalFlag(field) is true;

        public bool? OptionalFlag(string field) =>
            Value(field) is JsonValue value && value.TryGetValue<bool>(out var flag) ? flag : null;

        public Dictionary<string, string?> Entries(string field)
        {
            var entries = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            if (Value(field) is JsonObject shownEntries)
            {
                foreach (var entry in shownEntries)
                    entries[entry.Key] = entry.Value is JsonValue text && text.TryGetValue<string>(out var value) ? value : null;
            }

            return entries;
        }

        private JsonNode? Value(string field)
        {
            var property = aliases.FirstOrDefault(alias => string.Equals(alias.Key, field, StringComparison.OrdinalIgnoreCase)).Value;
            var name = property is null ? field : JsonNamingPolicy.CamelCase.ConvertName(property);
            var key = shown.Select(member => member.Key)
                .FirstOrDefault(candidate => string.Equals(candidate, name, StringComparison.OrdinalIgnoreCase));
            return key is null ? null : shown[key];
        }
    }
}
