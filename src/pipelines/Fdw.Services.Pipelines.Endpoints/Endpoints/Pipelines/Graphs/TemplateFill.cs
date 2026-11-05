using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Fdw.Results;
using Fdw.Services.Etl.Pipelines.Loads;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Makes the payload of a new pipeline from a template and the values given for its parameters (FDW-3379).
/// </summary>
/// <remarks>
/// <para>
/// A template is an ordinary pipeline whose declared parameters are the places to fill. Each parameter that is given a value is replaced everywhere its <c>{{Name}}</c>
/// appears (in settings, scripts, names and nested lists) and is removed from the new pipeline's parameters. A parameter that is not given stays a run parameter,
/// and so does every <c>{{Name}}</c> that refers to it.
/// </para>
/// <para>
/// The values are read and placed by the engine's own rules (<see cref="PipelineRunParameterValues"/> and <see cref="PipelineRunParameterReferences"/>): a name the
/// template does not declare, a name given twice and a text that is not a value of the declared type are refused naming the parameter, and a text value holds only
/// the characters a run parameter's value may. It works on the payload, so the result goes through the same read, check and save as a pipeline typed in the builder.
/// The new pipeline is not a template, and it records the template it was made from and that template's version.
/// </para>
/// </remarks>
public static class TemplateFill
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    /// <summary>Makes the new pipeline's payload.</summary>
    /// <param name="template">The template as the API states it.</param>
    /// <param name="templateVersion">The version of the template, the time it was last changed; the new pipeline records it.</param>
    /// <param name="name">The name of the new pipeline.</param>
    /// <param name="values">The name and text of each value given.</param>
    /// <param name="logger">The logger a refusal is written to.</param>
    /// <returns>The payload of the new pipeline, or the refusal.</returns>
    public static IGenericResult<GraphPayload> Apply(
        GraphPayload template, DateTimeOffset templateVersion, string name, IReadOnlyList<KeyValuePair<string, string>> values, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(logger);

        // Why only the parameters that are given: a parameter nobody gives stays a run parameter, so it must not be asked for, defaulted or refused here.
        var given = new HashSet<string>(values.Select(pair => pair.Key), StringComparer.OrdinalIgnoreCase);
        IEnumerable<IPipelineRunParameter> declared = [.. template.Parameters.Where(parameter => given.Contains(parameter.Name)).Select(parameter => new TemplateParameter(parameter))];
        var typed = PipelineRunParameterValues.Resolve(declared, values, logger);
        if (typed.IsFailure || typed.Value is null)
        {
            return GraphRefusal.From<GraphPayload>(typed);
        }

        var root = JsonSerializer.SerializeToNode(template, Web);
        var filled = Fill(root, template.Name, typed.Value, logger);
        if (filled.IsFailure)
        {
            return GraphRefusal.From<GraphPayload>(filled);
        }

        if (root.Deserialize<GraphPayload>(Web) is not { } made)
        {
            return GraphRefusal.Of<GraphPayload>($"template '{template.Name}' could not be read back after its values were filled in");
        }

        made.Name = name;
        made.IsTemplate = false;
        made.TemplateName = template.Name;
        made.TemplateVersion = templateVersion;
        made.Parameters = [.. made.Parameters.Where(parameter => !typed.Value.ContainsKey(parameter.Name))];
        return GenericResult<GraphPayload>.Success(made);
    }

    // Replaces the references in every text under the node, in place; stops at the first refusal.
    private static IGenericResult Fill(JsonNode? node, string owner, IReadOnlyDictionary<string, object?> values, ILogger logger)
    {
        switch (node)
        {
            case JsonObject properties:
                foreach (var key in properties.Select(pair => pair.Key).ToList())
                {
                    var replaced = FillChild(properties[key], owner, values, logger);
                    if (replaced.IsFailure)
                    {
                        return replaced;
                    }

                    properties[key] = replaced.Value;
                }

                return GenericResult.Success();
            case JsonArray items:
                for (var index = 0; index < items.Count; index++)
                {
                    var replaced = FillChild(items[index], owner, values, logger);
                    if (replaced.IsFailure)
                    {
                        return replaced;
                    }

                    items[index] = replaced.Value;
                }

                return GenericResult.Success();
            default:
                return GenericResult.Success();
        }
    }

    // A text is replaced by its filled text; an object or array is filled in place and kept.
    private static IGenericResult<JsonNode?> FillChild(JsonNode? child, string owner, IReadOnlyDictionary<string, object?> values, ILogger logger)
    {
        if (child is JsonValue value && value.TryGetValue<string>(out var text))
        {
            var resolved = PipelineRunParameterReferences.ResolveGiven(owner, text, values, logger);
            return resolved.IsFailure || resolved.Value is null
                ? resolved.ToNewResult<JsonNode?>()
                : GenericResult<JsonNode?>.Success(JsonValue.Create(resolved.Value));
        }

        var filled = Fill(child, owner, values, logger);
        return filled.IsFailure ? filled.ToNewResult<JsonNode?>() : GenericResult<JsonNode?>.Success(child?.DeepClone());
    }
}
