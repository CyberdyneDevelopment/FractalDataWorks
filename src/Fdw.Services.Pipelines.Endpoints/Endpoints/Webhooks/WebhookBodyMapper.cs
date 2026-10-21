using System.Collections.Generic;
using System.Text.Json;
using Fdw.Results;
using Fdw.Services.Etl.Webhooks;
using Fdw.Services.Pipelines.Webhooks;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Reads the values of a call to a webhook trigger out of its JSON body, by the mappings the trigger holds (FDW-1884).
/// </summary>
/// <remarks>
/// Why a path that finds nothing supplies nothing, and is not an error: whether a parameter is required, and what type it is, is decided
/// by the pipeline's own rules when the run starts (<c>PipelineRunParameterValues</c>), which refuse a missing required value by name.
/// A value that is a JSON array is given as its JSON text, which is how a list parameter is supplied; a string is given as its text; a
/// number or a true or false as written.
/// </remarks>
public static class WebhookBodyMapper
{
    /// <summary>Reads the values.</summary>
    /// <param name="body">The text of the body; empty means the call carried none.</param>
    /// <param name="maps">The trigger's mappings.</param>
    /// <returns>The name and text of each value found, in the order of the mappings; or the refusal when the body is not JSON.</returns>
    public static IGenericResult<IReadOnlyList<KeyValuePair<string, string>>> Map(string body, IEnumerable<WebhookTriggerParameterMapConfiguration> maps)
    {
        var supplied = new List<KeyValuePair<string, string>>();
        using var enumerator = maps.GetEnumerator();
        if (!enumerator.MoveNext() || string.IsNullOrWhiteSpace(body))
        {
            return GenericResult<IReadOnlyList<KeyValuePair<string, string>>>.Success(supplied);
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(body);
        }
        catch (JsonException ex)
        {
            return WebhookCallRefusal.Of<IReadOnlyList<KeyValuePair<string, string>>>($"the body is not JSON ({ex.Message})");
        }

        using (document)
        {
            do
            {
                if (Find(document.RootElement, enumerator.Current.BodyPath) is { } text)
                {
                    supplied.Add(new KeyValuePair<string, string>(enumerator.Current.ParameterName, text));
                }
            }
            while (enumerator.MoveNext());
        }

        return GenericResult<IReadOnlyList<KeyValuePair<string, string>>>.Success(supplied);
    }

    private static string? Find(JsonElement root, string path)
    {
        var current = root;
        foreach (var name in path.Split('.'))
        {
            if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(name, out current))
            {
                return null;
            }
        }

        return current.ValueKind switch
        {
            JsonValueKind.Null or JsonValueKind.Undefined => null,
            JsonValueKind.String => current.GetString(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => current.GetRawText(),
        };
    }
}
