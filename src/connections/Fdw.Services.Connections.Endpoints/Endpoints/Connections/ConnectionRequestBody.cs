using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Fdw.Services.Connections.Endpoints;

/// <summary>
/// Turns a create or update request into the JSON object a connection type's
/// endpoint behavior reads: the deprecated flat fields the caller set,
/// with the request's <c>Configuration</c> laid over them.
/// </summary>
/// <remarks>
/// <para>
/// The flat fields are written under their own names (<c>server</c>, <c>baseUrl</c>, <c>authentication</c>,
/// ...). A field that matches a configuration property binds as it is; one that does not is mapped by the
/// connection type's own aliases (<c>baseUrl</c> -&gt; <c>Root</c>), and anything the type has no use for is
/// ignored. There is no per-type mapping here.
/// </para>
/// <para>
/// Why a create field is written only when it differs from the request's own initial value: the flat
/// request is shaped around SQL Server (a port of 1433, <c>encrypt</c> on), and writing those for every
/// type would override a type's own defaults -- a PostgreSql connection that sent no port would get 1433.
/// </para>
/// </remarks>
public static class ConnectionRequestBody
{
    /// <summary>The JSON a create binds from.</summary>
    /// <param name="request">The create request.</param>
    public static JsonElement ForCreate(CreateConnectionRequest request)
    {
        var body = new JsonObject();
        SetText(body, "server", request.Server);
        if (request.Port != new CreateConnectionRequest().Port)
            body["port"] = request.Port;
        SetText(body, "database", request.Database);
        SetText(body, "authenticationType", request.AuthenticationType);
        SetEntries(body, "authentication", request.Authentication);
        if (request.TrustServerCertificate)
            body["trustServerCertificate"] = true;
        if (!request.Encrypt)
            body["encrypt"] = false;
        SetText(body, "baseUrl", request.BaseUrl);
        SetText(body, "protocol", request.Protocol);
        if (request.TimeoutSeconds is { } timeout)
            body["timeoutSeconds"] = timeout;
        SetEntries(body, "headers", request.Headers);
        SetText(body, "securityType", request.SecurityType);
        SetEntries(body, "security", request.Security);
        if (request.UseMtls)
            body["useMtls"] = true;
        SetText(body, "defaultValuePolicy", request.DefaultValuePolicy);

        return Overlay(body, request.Configuration);
    }

    /// <summary>The JSON an update merges from: only the members the caller sent.</summary>
    /// <param name="request">The update request. Null members mean no change.</param>
    public static JsonElement ForUpdate(UpdateConnectionRequest request)
    {
        var body = new JsonObject();
        SetText(body, "server", request.Server);
        if (request.Port is { } port)
            body["port"] = port;
        SetText(body, "database", request.Database);
        SetText(body, "authenticationType", request.AuthenticationType);
        SetEntries(body, "authentication", request.Authentication);
        if (request.TrustServerCertificate is { } trust)
            body["trustServerCertificate"] = trust;
        if (request.Encrypt is { } encrypt)
            body["encrypt"] = encrypt;
        SetText(body, "baseUrl", request.BaseUrl);
        SetText(body, "protocol", request.Protocol);
        if (request.TimeoutSeconds is { } timeout)
            body["timeoutSeconds"] = timeout;
        SetEntries(body, "headers", request.Headers);
        SetText(body, "securityType", request.SecurityType);
        SetEntries(body, "security", request.Security);
        if (request.UseMtls is { } useMtls)
            body["useMtls"] = useMtls;
        SetText(body, "defaultValuePolicy", request.DefaultValuePolicy);

        return Overlay(body, request.Configuration);
    }

    // Why the typed configuration wins: a caller that sends both is sending the new shape on purpose.
    private static JsonElement Overlay(JsonObject flat, JsonElement? configuration)
    {
        if (configuration is { ValueKind: JsonValueKind.Object } typed)
        {
            foreach (var member in typed.EnumerateObject())
            {
                var flatName = flat.Select(existing => existing.Key)
                    .FirstOrDefault(key => string.Equals(key, member.Name, System.StringComparison.OrdinalIgnoreCase));
                if (flatName is not null)
                    flat.Remove(flatName);
                flat[member.Name] = JsonNode.Parse(member.Value.GetRawText());
            }
        }
        else if (configuration is { ValueKind: not JsonValueKind.Null and not JsonValueKind.Undefined } other)
        {
            // Not an object: hand it over as it is, so the behavior refuses it with its own message.
            return other;
        }

        return JsonSerializer.SerializeToElement(flat);
    }

    private static void SetText(JsonObject body, string name, string? value)
    {
        if (!string.IsNullOrEmpty(value))
            body[name] = value;
    }

    private static void SetEntries(JsonObject body, string name, IDictionary<string, string?>? entries)
    {
        if (entries is null || entries.Count == 0)
            return;

        var shown = new JsonObject();
        foreach (var entry in entries)
            shown[entry.Key] = entry.Value;
        body[name] = shown;
    }
}
