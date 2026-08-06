using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Aui;
using Fdw.Aui.Models;

namespace Fdw.UI.WebMcp;

/// <summary>
/// Projects the AUI layer's tool metadata onto the WebMCP UI layer.
/// </summary>
/// <remarks>
/// The framework already describes agent-callable UI actions once, as an <see cref="AuiTool"/>
/// paired with an <see cref="IAuiAction"/>. These extensions publish that same declaration to an
/// in-browser agent, so adding WebMCP support to a page that already exposes AUI metadata does not
/// mean re-describing its tools.
/// </remarks>
public static class AuiToolExtensions
{
    /// <summary>
    /// Converts an AUI tool declaration and its executing action into a page-scoped WebMCP tool.
    /// </summary>
    /// <param name="tool">The AUI tool metadata.</param>
    /// <param name="action">The action that executes the tool.</param>
    /// <param name="userId">The user whose context the action runs in.</param>
    /// <returns>The equivalent <see cref="WebMcpUiTool"/>.</returns>
    public static WebMcpUiTool ToWebMcpTool(this AuiTool tool, IAuiAction action, Guid userId)
    {
        ArgumentNullException.ThrowIfNull(tool);
        ArgumentNullException.ThrowIfNull(action);

        return new WebMcpUiTool
        {
            Name = tool.Name,
            Description = tool.Description,
            InputSchema = tool.InputSchema,
            RequiresConfirmation = tool.RequiresConfirmation,
            Execute = (arguments, cancellationToken) => Execute(action, userId, arguments, cancellationToken),
        };
    }

    private static async Task<string> Execute(
        IAuiAction action,
        Guid userId,
        JsonElement arguments,
        CancellationToken cancellationToken)
    {
        var result = await action.Execute(userId, ToParameters(arguments), cancellationToken).ConfigureAwait(false);

        if (result.IsSuccess)
            return new JsonObject { ["success"] = true }.ToJsonString();

        // Why no ?? fallback on CurrentMessage: a failed result with no message is a defect in the
        // action, and saying so is more useful than inventing "Unknown error" and hiding it.
        return new JsonObject
        {
            ["success"] = false,
            ["error"] = result.CurrentMessage is { } message
                ? message
                : $"Action '{action.Name}' failed without reporting a message.",
        }.ToJsonString();
    }

    /// <summary>
    /// Flattens an agent's JSON arguments into the loosely-typed parameter bag
    /// <see cref="IAuiAction.Execute"/> expects.
    /// </summary>
    /// <remarks>
    /// Primitives map to their CLR equivalents. Nested objects and arrays are passed through as
    /// their raw JSON text, because the parameter bag has no structured representation for them
    /// and discarding them would silently drop arguments the agent supplied.
    /// </remarks>
    private static Dictionary<string, object> ToParameters(JsonElement arguments)
    {
        var parameters = new Dictionary<string, object>(StringComparer.Ordinal);

        if (arguments.ValueKind != JsonValueKind.Object)
            return parameters;

        foreach (var property in arguments.EnumerateObject())
        {
            var value = ToParameterValue(property.Value);
            if (value is not null)
                parameters[property.Name] = value;
        }

        return parameters;
    }

    // Why a dispatch table rather than a switch: FDW018 steers enum dispatch onto a data-driven
    // lookup instead of a switch, and JsonValueKind belongs to the BCL, so it cannot carry
    // [TypeOption]s the way a TypeCollection would. A frozen table is the same idea — behaviour
    // hangs off the value — for an enum this solution does not own.
    // Why null for Null/Undefined: the parameter bag is non-nullable, so a JSON null has no
    // representation. The caller drops the key, letting the action apply its own
    // required/optional rules instead of receiving a sentinel it would have to special-case.
    private static readonly FrozenDictionary<JsonValueKind, Func<JsonElement, object?>> ValueReaders =
        new Dictionary<JsonValueKind, Func<JsonElement, object?>>
        {
            [JsonValueKind.String] = element => element.GetString(),
            [JsonValueKind.True] = _ => true,
            [JsonValueKind.False] = _ => false,
            [JsonValueKind.Number] = ToNumber,
            [JsonValueKind.Null] = _ => null,
            [JsonValueKind.Undefined] = _ => null,
        }.ToFrozenDictionary();

    // Why GetRawText for anything unlisted (objects, arrays): the parameter bag has no structured
    // representation for them, and discarding them would silently drop arguments the agent sent.
    private static object? ToParameterValue(JsonElement element) =>
        ValueReaders.TryGetValue(element.ValueKind, out var reader)
            ? reader(element)
            : element.GetRawText();

    private static object ToNumber(JsonElement element) =>
        element.TryGetInt64(out var integer)
            ? integer
            : element.GetDouble();
}
