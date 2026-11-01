using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Fdw.Results;
using Fdw.WebMcp.Hosting;
using Microsoft.Extensions.Logging;

namespace Fdw.WebMcp.Mcp;

/// <summary>
/// Turns an agent's arguments into the HTTP call a tool's plan describes.
/// </summary>
/// <remarks>
/// The rules are not restated here. Which values travel in the path, which in the query string and
/// which in the body, and which value the tool fixes for itself, are all read off
/// <see cref="WebMcpToolPlan"/> -- the same plan the browser script is rendered from. That is what
/// keeps the two transports from making different calls out of the same input.
/// </remarks>
public static class WebMcpRequestBuilder
{
    private static readonly IReadOnlyDictionary<string, JsonElement> NoArguments =
        new Dictionary<string, JsonElement>(StringComparer.Ordinal);


    /// <summary>Builds the call for one tool invocation.</summary>
    /// <param name="plan">The tool's plan.</param>
    /// <param name="arguments">What the agent supplied, or <see langword="null"/> for none.</param>
    /// <param name="logger">Receives a message when a required value is absent.</param>
    /// <returns>The call, or a failure naming the value the URL cannot be built without.</returns>
    public static IGenericResult<WebMcpToolRequest> Build(
        WebMcpToolPlan plan,
        IReadOnlyDictionary<string, JsonElement>? arguments,
        ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(logger);

        // MCP lets a caller omit `arguments` entirely, and an omitted set IS an empty set -- it is
        // not a missing value being stood in for. A tool that needs a value still refuses below,
        // naming it, rather than proceeding on a default.
        var supplied = arguments is null ? NoArguments : arguments;
        var url = new System.Text.StringBuilder();

        foreach (var part in plan.RouteParts)
        {
            if (!part.IsParameter)
            {
                url.Append(part.Text);
                continue;
            }

            // Refused rather than sent with a hole in it. The browser script would put the literal
            // string "undefined" in the URL here and take the 404 -- worth not copying.
            if (!supplied.TryGetValue(part.Text, out var value) || value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            {
                return GenericResult<WebMcpToolRequest>.Failure(
                    McpBridgeLog.RequiredArgumentMissing(logger, plan.Tool.Name, part.Text));
            }

            url.Append(Uri.EscapeDataString(AsText(value)));
        }

        AppendQuery(url, plan, supplied);

        return GenericResult<WebMcpToolRequest>.Success(
            new WebMcpToolRequest(plan.Tool.HttpMethod, url.ToString(), Body(plan, supplied)));
    }

    /// <summary>Appends the query string a bodyless tool carries its remaining inputs in.</summary>
    private static void AppendQuery(
        System.Text.StringBuilder url,
        WebMcpToolPlan plan,
        IReadOnlyDictionary<string, JsonElement> supplied)
    {
        // Only values the agent actually supplied are sent: an omitted filter must widen the result,
        // not narrow it to the empty string.
        var pairs = plan.QueryProperties
            .Where(name => supplied.TryGetValue(name, out var value)
                && value.ValueKind is not (JsonValueKind.Null or JsonValueKind.Undefined)
                && AsText(value).Length > 0)
            .Select(name => Uri.EscapeDataString(name) + "=" + Uri.EscapeDataString(AsText(supplied[name])))
            .ToList();

        if (pairs.Count > 0)
        {
            url.Append('?').Append(string.Join("&", pairs));
        }
    }

    /// <summary>The JSON body, or null for a verb that carries none.</summary>
    private static string? Body(WebMcpToolPlan plan, IReadOnlyDictionary<string, JsonElement> supplied)
    {
        if (!plan.HasBody)
        {
            return null;
        }

        var body = new JsonObject();

        foreach (var (name, value) in supplied)
        {
            // Path values are already in the URL. An endpoint receiving the same value twice can
            // disagree with itself about which one binds.
            if (!plan.PathProperties.Contains(name, StringComparer.Ordinal))
            {
                body[name] = JsonNode.Parse(value.GetRawText());
            }
        }

        // Written last so the tool's own identity decides this value, even if the agent sent the
        // same key.
        if (plan.Tool is { PresetFieldName: { } field, PresetValue: { } preset })
        {
            body[field] = preset;
        }

        return body.ToJsonString();
    }

    /// <summary>A JSON value as the text a URL or query string carries.</summary>
    /// <remarks>
    /// Strings travel unquoted; everything else travels as its JSON text, which matches what the
    /// browser script sends for the same input.
    /// </remarks>
    [SuppressMessage("Fdw.Analyzers", "FDW018:Switch on enum suggests a TypeCollection lookup",
        Justification = "JsonValueKind is a BCL enum belonging to System.Text.Json. There is no TypeCollection for it, and inventing one to wrap four JSON primitives would add a registration surface with no second implementation behind it.")]
    private static string AsText(JsonElement value)
        => value.ValueKind switch
        {
            // Not JsonElement.ToString(): that renders a boolean as "True", and the browser sends
            // "true". Not `?? ""` either -- the kind check already guarantees a string is there.
            JsonValueKind.String => value.GetString()!,
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => value.GetRawText(),
        };
}
