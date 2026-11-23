using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using Fdw.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.WebMcp.Hosting;

/// <summary>
/// Checks the arguments of a tool call against the description its plan gave the agent, before the endpoint is reached.
/// </summary>
/// <remarks>
/// <para>
/// Reads the same <see cref="WebMcpToolInput"/> tree the transports render, so what an agent was told and what a call is
/// held to cannot differ: a value's type, the members a request or an object requires, each item of a list and each member of
/// an object, down to the depth the plan was built to. A refusal names every member that does not match by its path
/// (<c>Attendees[1].Email</c>).
/// </para>
/// <para>
/// What it does not refuse: a member the description does not name (the schema states no <c>additionalProperties</c>, so the
/// endpoint decides), and the type of a scalar that travels as text in the path or query string, where the endpoint's binder
/// reads <c>"5"</c> as 5 today and refusing it here would turn a call that works into a refusal.
/// </para>
/// </remarks>
public static class WebMcpArgumentValidator
{
    private static readonly IReadOnlyDictionary<string, JsonElement> NoArguments =
        new Dictionary<string, JsonElement>(StringComparer.Ordinal);

    private static readonly Dictionary<JsonValueKind, string> KindNames = new()
    {
        [JsonValueKind.String] = "string",
        [JsonValueKind.Number] = "number",
        [JsonValueKind.True] = "boolean",
        [JsonValueKind.False] = "boolean",
        [JsonValueKind.Array] = "array",
        [JsonValueKind.Object] = "object",
        [JsonValueKind.Null] = "null",
        [JsonValueKind.Undefined] = "undefined",
    };

    private static readonly Dictionary<string, Func<JsonElement, bool>> ScalarTests = new(StringComparer.Ordinal)
    {
        ["string"] = value => value.ValueKind == JsonValueKind.String,
        ["integer"] = IsInteger,
        ["number"] = value => value.ValueKind == JsonValueKind.Number,
        ["boolean"] = value => value.ValueKind is JsonValueKind.True or JsonValueKind.False,
    };

    /// <summary>Checks one call.</summary>
    /// <param name="plan">The tool's plan.</param>
    /// <param name="arguments">What the agent supplied, or <see langword="null"/> for none.</param>
    /// <param name="logger">Receives the refusal, or a trace when the call matches.</param>
    /// <returns>Success, or one refusal naming every member that does not match.</returns>
    public static IGenericResult Validate(
        WebMcpToolPlan plan,
        IReadOnlyDictionary<string, JsonElement>? arguments,
        ILogger logger)
    {
        // MCP lets a caller omit `arguments` entirely, and an omitted set IS an empty set: a tool that
        // requires a member still refuses below, naming it.
        var supplied = arguments is null ? NoArguments : arguments;
        var problems = new List<string>();

        foreach (var name in plan.RequiredProperties.Where(name => !IsSupplied(supplied, name)))
        {
            problems.Add(Missing(name));
        }

        foreach (var input in plan.Inputs)
        {
            if (!IsSupplied(supplied, input.Name) || !IsChecked(plan, input))
            {
                continue;
            }

            Check(input, Value(supplied, input.Name), input.Name, problems);
        }

        if (problems.Count > 0)
        {
            return GenericResult.Failure(WebMcpLog.ArgumentsInvalid(logger, plan.Tool.Name, string.Join("; ", problems)));
        }

        WebMcpLog.ArgumentsChecked(logger, plan.Tool.Name, supplied.Count);
        return GenericResult.Success();
    }

    /// <summary>Whether an input of the request is held to its type: everything in a JSON body, and a list or object wherever it is.</summary>
    private static bool IsChecked(WebMcpToolPlan plan, WebMcpToolInput input)
        => plan.HasBody || input.Items is not null || input.Properties is not null;

    private static void Check(WebMcpToolInput input, JsonElement value, string path, List<string> problems)
    {
        if (value.ValueKind == JsonValueKind.Null)
        {
            // A null for an optional member says nothing; a null for a required one is a value that is missing.
            if (input.Required)
            {
                problems.Add(Missing(path));
            }

            return;
        }

        if (input.Items is { } items)
        {
            CheckList(items, value, path, problems);
        }
        else if (input.Properties is { } members)
        {
            CheckObject(members, value, path, problems);
        }
        else if (!ScalarTests.TryGetValue(input.JsonType, out var matches))
        {
            problems.Add(path + ": the description names a type this host cannot check (" + input.JsonType + ")");
        }
        else if (!matches(value))
        {
            problems.Add(Mismatch(path, input.JsonType, value));
        }
    }

    private static void CheckList(WebMcpToolInput items, JsonElement value, string path, List<string> problems)
    {
        if (value.ValueKind != JsonValueKind.Array)
        {
            problems.Add(Mismatch(path, "array", value));
            return;
        }

        var index = 0;
        foreach (var item in value.EnumerateArray())
        {
            var itemPath = path + "[" + index.ToString(CultureInfo.InvariantCulture) + "]";
            if (item.ValueKind == JsonValueKind.Null)
            {
                problems.Add(Mismatch(itemPath, items.JsonType, item));
            }
            else
            {
                Check(items, item, itemPath, problems);
            }

            index++;
        }
    }

    private static void CheckObject(IReadOnlyList<WebMcpToolInput> members, JsonElement value, string path, List<string> problems)
    {
        if (value.ValueKind != JsonValueKind.Object)
        {
            problems.Add(Mismatch(path, "object", value));
            return;
        }

        foreach (var member in members)
        {
            var memberPath = path + "." + member.Name;

            // Found the way the endpoint's binder finds it: by name, ignoring case.
            var found = value.EnumerateObject()
                .Where(property => string.Equals(property.Name, member.Name, StringComparison.OrdinalIgnoreCase))
                .Select(property => property.Value)
                .Cast<JsonElement?>()
                .FirstOrDefault();

            if (found is not { } present || present.ValueKind == JsonValueKind.Null)
            {
                if (member.Required)
                {
                    problems.Add(Missing(memberPath));
                }

                continue;
            }

            Check(member, present, memberPath, problems);
        }
    }

    private static bool IsSupplied(IReadOnlyDictionary<string, JsonElement> supplied, string name)
        => supplied.Any(argument => string.Equals(argument.Key, name, StringComparison.OrdinalIgnoreCase)
            && argument.Value.ValueKind is not (JsonValueKind.Null or JsonValueKind.Undefined));

    private static JsonElement Value(IReadOnlyDictionary<string, JsonElement> supplied, string name)
        => supplied.First(argument => string.Equals(argument.Key, name, StringComparison.OrdinalIgnoreCase)).Value;

    private static bool IsInteger(JsonElement value)
        => value.ValueKind == JsonValueKind.Number
            && (value.TryGetInt64(out _) || (value.TryGetDecimal(out var number) && number == decimal.Truncate(number)));

    private static string Missing(string path) => path + ": a value is required and was not given";

    private static string Mismatch(string path, string expected, JsonElement value)
        => path + ": expected " + expected + ", got " + KindNames[value.ValueKind];
}
