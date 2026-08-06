using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;

namespace Fdw.ContractParity.Tests;

/// <summary>
/// Reflection-based contract-parity guard: every field a client request DTO serializes must have a
/// matching field (by JSON name, honoring [JsonPropertyName]) on its server endpoint counterpart.
/// Without this, the UI silently sends fields the server contract lacks (RC-B) — they bind to null
/// and are dropped, or, when the discriminator itself drifts, the create throws (HTTP 500).
/// </summary>
public class ContractParityTests
{
    // Client request DTO  ->  server endpoint request DTO.
    public static IEnumerable<object[]> Pairs()
    {
        yield return Pair(
            typeof(Fdw.Services.Scheduling.Clients.Abstractions.CreateScheduleClientRequest),
            typeof(Fdw.Services.Scheduling.Endpoints.CreateScheduleRequest),
            // Client discriminator field name vs server's; both carry the scheduler type.
            ignoreClient: new[] { "Name" });

        yield return Pair(
            typeof(Fdw.Services.Data.Clients.Models.CreateDataSetPayload),
            typeof(Fdw.Services.Data.Endpoints.CreateDataSetRequest));

        yield return Pair(
            typeof(Fdw.Services.Quality.Clients.Models.CreateQualityRulePayload),
            typeof(Fdw.Services.Quality.Endpoints.CreateQualityRuleRequest),
            // Client expresses the rule body as a single Expression; server splits it across typed
            // fields (Pattern/MinValue/...). Only assert the shared identity fields are present.
            ignoreClient: new[] { "Expression" });

        // Wave-2: connection update — ServiceType is a client-side routing discriminator only.
        yield return Pair(
            typeof(Fdw.Services.Connections.Clients.Models.UpdateConnectionClientRequest),
            typeof(Fdw.Services.Connections.Endpoints.UpdateConnectionRequest),
            ignoreClient: new[] { "ServiceType" });

        // Wave-2: DataStore create — Paths are child records added via a separate endpoint after
        // the parent DataStore is persisted; they are not part of the initial create contract.
        yield return Pair(
            typeof(Fdw.Services.Data.Clients.Models.CreateDataStoreWithPathsRequest),
            typeof(Fdw.Services.Data.Endpoints.CreateDataStoreRequest),
            ignoreClient: new[] { "Paths" });

        // Wave-2: DataSet update — Name and KeyFields are server-only (route-bound / managed internally).
        yield return Pair(
            typeof(Fdw.Services.Data.Clients.Models.UpdateDataSetPayload),
            typeof(Fdw.Services.Data.Endpoints.UpdateDataSetRequest));

        // Wave-2: user update — Name is route-bound on the server side.
        yield return Pair(
            typeof(Fdw.Services.Users.Clients.Models.UpdateUserPayload),
            typeof(Fdw.Services.Users.Endpoints.UpdateUserRequest));
    }

    [Theory]
    [MemberData(nameof(Pairs))]
    public void EveryClientField_HasServerCounterpart(Type clientType, Type serverType, string[] ignoreClient)
    {
        var serverNames = JsonNames(serverType);

        var missing = JsonNames(clientType)
            .Where(n => !ignoreClient.Contains(n.PropertyName, StringComparer.Ordinal))
            .Where(n => !serverNames.Any(s => string.Equals(s.JsonName, n.JsonName, StringComparison.OrdinalIgnoreCase)))
            .Select(n => $"{n.PropertyName} (json:'{n.JsonName}')")
            .ToList();

        missing.ShouldBeEmpty(
            $"{clientType.Name} sends fields the server {serverType.Name} contract lacks: {string.Join(", ", missing)}");
    }

    private static object[] Pair(Type client, Type server, string[]? ignoreClient = null)
        => new object[] { client, server, ignoreClient ?? Array.Empty<string>() };

    private static IReadOnlyList<(string PropertyName, string JsonName)> JsonNames(Type type)
        => type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite)
            .Select(p => (
                p.Name,
                p.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? p.Name))
            .ToList();
}
