using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Fdw.Results;
using Fdw.Web.RestEndpoints.ApiReference.Logging;
using Fdw.Web.RestEndpoints.ApiReference.Results;
using Fdw.Web.RestEndpoints.OpenApi;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Fdw.Web.RestEndpoints.ApiReference;

/// <summary>
/// One request's copy of the OpenAPI document, cut down to what one API reference shows one caller.
/// </summary>
/// <remarks>
/// <para>
/// Worked on as JSON rather than as NSwag's object model: the document is generated once and parsed
/// again for every request, so each caller's cuts land on their own copy and never on the one the
/// next caller starts from.
/// </para>
/// <para>
/// What may be called is read from the stamps <see cref="AuthorizationMetadataOperationProcessor"/>
/// writes at generation. An operation without them fails the cut: shown anyway, it would reach a
/// caller nobody checked may see it.
/// </para>
/// </remarks>
public sealed class ApiReferenceDocument
{
    private const string SchemaReferencePrefix = "#/components/schemas/";

    private static readonly HashSet<string> OperationMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        "get", "put", "post", "delete", "options", "head", "patch", "trace",
    };

    // A path parameter is written {name}; the shape of a template is what is left once every name is set aside.
    private static readonly Regex Placeholder = new(@"\{[^}/]*\}", RegexOptions.Compiled, TimeSpan.FromSeconds(1));

    private readonly JsonObject _root;
    private readonly JsonObject _paths;

    private ApiReferenceDocument(JsonObject root, JsonObject paths)
    {
        _root = root;
        _paths = paths;
    }

    /// <summary>Gets the paths the document still holds.</summary>
    public IReadOnlyList<string> Paths => [.. _paths.Select(entry => entry.Key)];

    /// <summary>Gets the number of operations the document still holds.</summary>
    public int OperationCount => _paths.Sum(entry => entry.Value is JsonObject item ? item.Count(IsOperation) : 0);

    /// <summary>Parses a generated document into a copy that can be cut.</summary>
    /// <param name="json">The generated document.</param>
    /// <param name="logger">The logger.</param>
    /// <returns>The copy, or a failure when the JSON is not an OpenAPI document with paths.</returns>
    public static IGenericResult<ApiReferenceDocument> Parse(string json, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(json);

        JsonNode? node;
        try
        {
            node = JsonNode.Parse(json);
        }
        catch (JsonException exception)
        {
            return Unreadable<ApiReferenceDocument>(logger, "it is not JSON: " + exception.Message);
        }

        return node is JsonObject root && root["paths"] is JsonObject paths
            ? GenericResult<ApiReferenceDocument>.Success(new ApiReferenceDocument(root, paths))
            : Unreadable<ApiReferenceDocument>(logger, "it has no paths object");
    }

    /// <summary>Takes out every path the reference does not cover.</summary>
    /// <param name="keep">Whether a path belongs to the reference.</param>
    /// <param name="logger">The logger.</param>
    /// <returns>The number of paths taken out.</returns>
    public int KeepPaths(Func<string, bool> keep, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(keep);

        var outside = _paths.Select(entry => entry.Key).Where(path => !keep(path)).ToList();
        foreach (var path in outside)
            _paths.Remove(path);

        ApiReferenceLog.PathsScoped(logger, outside.Count, _paths.Count);
        return outside.Count;
    }

    /// <summary>Takes out every operation the caller may not call, and every path left with none.</summary>
    /// <param name="caller">Who the document is for.</param>
    /// <param name="logger">The logger.</param>
    /// <returns>The number of operations taken out, or a failure when an operation has no record of who may call it.</returns>
    public IGenericResult<int> KeepAdmitted(ApiReferenceCaller caller, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(caller);

        var removed = 0;
        foreach (var (path, node) in _paths.ToList())
        {
            if (node is not JsonObject item)
                return Unreadable<int>(logger, "path '" + path + "' is not an object");

            var cut = CutPath(path, item, caller, logger);
            if (cut.IsFailure)
                return cut;

            removed += cut.Value;
            if (!item.Any(IsOperation))
                _paths.Remove(path);
        }

        ApiReferenceLog.OperationsFiltered(logger, caller.Kind, OperationCount, removed);
        return GenericResult<int>.Success(removed);
    }

    /// <summary>Finds one operation, to use as the template for others.</summary>
    /// <param name="path">The operation's path.</param>
    /// <param name="method">The operation's HTTP method.</param>
    /// <param name="logger">The logger.</param>
    /// <returns>The operation, or a failure when the document does not hold it.</returns>
    public IGenericResult<JsonObject> Operation(string path, string method, ILogger logger)
        => _paths[path] is JsonObject item && item[method.ToLowerInvariant()] is JsonObject operation
            ? GenericResult<JsonObject>.Success(operation)
            : Unreadable<JsonObject>(logger, "it has no " + method.ToUpperInvariant() + " " + path);

    /// <summary>Adds an operation, creating its path when the document has none.</summary>
    /// <param name="path">The operation's path.</param>
    /// <param name="method">The operation's HTTP method.</param>
    /// <param name="operation">The operation. It must not belong to any document yet.</param>
    public void AddOperation(string path, string method, JsonObject operation)
    {
        ArgumentNullException.ThrowIfNull(operation);

        if (_paths[path] is not JsonObject item)
        {
            item = new JsonObject();
            _paths[path] = item;
        }

        item[method.ToLowerInvariant()] = operation;
    }

    /// <summary>Takes a path and all its operations out.</summary>
    /// <param name="path">The path.</param>
    public void RemovePath(string path) => _paths.Remove(path);

    /// <summary>Names the server this document describes: the origin the reader fetched it from.</summary>
    /// <param name="request">The request that fetched the document, as the host's forwarded-header middleware left it.</param>
    /// <param name="logger">The logger a refusal is written to.</param>
    /// <returns>Success, or a refusal when the request names no host to build the URL from.</returns>
    /// <remarks>
    /// Scheme, host and path base are read off the request, so what the host trusts from a front end
    /// (<c>X-Forwarded-Proto</c>, <c>X-Forwarded-Host</c>) decides them, and no host name is written down or read from
    /// configuration. The URL is the origin only: every path in the document already begins with the API's own prefix. Any
    /// <c>servers</c> the generator wrote is replaced, since it could only have guessed.
    /// </remarks>
    public IGenericResult SetServer(HttpRequest request, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!request.Host.HasValue)
        {
            return GenericResult.Failure(
                ApiReferenceResultCodes.ByName("DocumentUnreadable"), logger,
                ResultDetails.Create("reason", "the request that fetched it names no host to build its server URL from"));
        }

        _root["servers"] = new JsonArray(new JsonObject
        {
            ["url"] = request.Scheme + "://" + request.Host.ToUriComponent() + request.PathBase.ToUriComponent(),
        });
        return GenericResult.Success();
    }

    /// <summary>Makes every path template in the document one path: two spellings of a template become one.</summary>
    /// <param name="logger">The logger a refusal is written to.</param>
    /// <returns>The number of paths folded into another, or a refusal when two templates cannot be told apart.</returns>
    /// <remarks>
    /// OpenAPI treats templated paths that differ only in their parameter names as the same path, and a reader that ignores
    /// case rejects a document that holds two. The generator names a path parameter from the request type when an endpoint
    /// has one and from the route text when it has none, so one template can come out as <c>{FieldMappingId}</c> and
    /// <c>{fieldMappingId}</c>. Those differ only in case: they are merged under the camel-case spelling, and each operation's
    /// path parameters are renamed to match, so every parameter is declared once per template. Templates whose parameters
    /// differ by more than case (<c>{IdOrName}</c> and <c>{Name}</c>) are refused, as is an operation declared under two
    /// spellings: those are routes that need fixing, not something to guess a name for.
    /// </remarks>
    public IGenericResult<int> UnifyPathTemplates(ILogger logger)
    {
        var entries = _paths.ToList();
        var colliding = entries.GroupBy(entry => Shape(entry.Key), StringComparer.Ordinal).Where(group => group.Count() > 1).ToList();
        if (colliding.Count == 0)
            return GenericResult<int>.Success(0);

        if (colliding.FirstOrDefault(group => group.Select(entry => ParameterNames(entry.Key)).Distinct(StringComparer.Ordinal).Count() > 1) is { } apart)
        {
            return Unreadable<int>(
                logger,
                "paths " + string.Join(", ", apart.Select(entry => "'" + entry.Key + "'"))
                + " are one template under different parameter names; give every operation on a template one parameter name");
        }

        var spellings = colliding.SelectMany(group => group.Select(entry => entry.Key)).ToHashSet(StringComparer.Ordinal);
        _paths.Clear();
        return Fold(entries, spellings, logger);
    }

    private IGenericResult<int> Fold(List<KeyValuePair<string, JsonNode?>> entries, HashSet<string> spellings, ILogger logger)
    {
        var kept = new Dictionary<string, JsonObject>(StringComparer.Ordinal);
        var folded = 0;
        foreach (var (path, node) in entries)
        {
            if (node is not JsonObject item)
                return Unreadable<int>(logger, "path '" + path + "' is not an object");

            var template = spellings.Contains(path) ? CamelCase(path) : path;
            if (spellings.Contains(path))
                RenamePathParameters(item);

            if (kept.TryAdd(template, item))
            {
                _paths[template] = item;
                continue;
            }

            var merged = Merge(kept[template], item, template, logger);
            if (merged.IsFailure)
                return merged;

            folded++;
        }

        return GenericResult<int>.Success(folded);
    }

    private static IGenericResult<int> Merge(JsonObject target, JsonObject source, string template, ILogger logger)
    {
        foreach (var (name, value) in source.ToList())
        {
            source.Remove(name);
            if (!target.ContainsKey(name))
            {
                target[name] = value;
                continue;
            }

            if (OperationMethods.Contains(name))
                return Unreadable<int>(logger, name.ToUpperInvariant() + " " + template + " is declared under two spellings of the same path");

            if (!JsonNode.DeepEquals(target[name], value))
                return Unreadable<int>(logger, "path " + template + " declares '" + name + "' two different ways under two spellings of it");
        }

        return GenericResult<int>.Success(0);
    }

    private static string Shape(string path) => Placeholder.Replace(path, "{}");

    private static string ParameterNames(string path)
        => string.Join('/', Placeholder.Matches(path).Select(match => match.Value.ToLowerInvariant()));

    private static string CamelCase(string path)
        => Placeholder.Replace(path, match => "{" + LowerFirst(match.Value[1..^1]) + "}");

    private static string LowerFirst(string name) => name.Length == 0 ? name : char.ToLowerInvariant(name[0]) + name[1..];

    private static void RenamePathParameters(JsonObject item)
    {
        RenamePathParameters(item["parameters"]);
        foreach (var (_, operation) in item.Where(IsOperation))
        {
            if (operation is JsonObject described)
                RenamePathParameters(described["parameters"]);
        }
    }

    private static void RenamePathParameters(JsonNode? parameters)
    {
        if (parameters is not JsonArray list)
            return;

        foreach (var parameter in list.OfType<JsonObject>().Where(described => string.Equals(TextOf(described["in"]), "path", StringComparison.Ordinal)))
        {
            if (TextOf(parameter["name"]) is { } name)
                parameter["name"] = LowerFirst(name);
        }
    }

    /// <summary>Sets the title the reference page shows.</summary>
    /// <param name="title">The title.</param>
    public void SetTitle(string title)
    {
        if (_root["info"] is not JsonObject info)
        {
            info = new JsonObject();
            _root["info"] = info;
        }

        info["title"] = title;
    }

    /// <summary>Sets the description of every HTTP bearer security scheme.</summary>
    /// <param name="description">What the reader should enter.</param>
    public void DescribeBearerSchemes(string description)
    {
        if (_root["components"] is not JsonObject components || components["securitySchemes"] is not JsonObject schemes)
            return;

        foreach (var (_, node) in schemes)
        {
            if (node is JsonObject scheme
                && scheme["scheme"] is JsonValue value
                && value.TryGetValue(out string? kind)
                && string.Equals(kind, "bearer", StringComparison.OrdinalIgnoreCase))
            {
                scheme["description"] = description;
            }
        }
    }

    /// <summary>Replaces the document's tag list, which the reference page groups operations by.</summary>
    /// <param name="tags">Each tag's name and, when it has one, its description.</param>
    public void ReplaceTags(IReadOnlyList<KeyValuePair<string, string?>> tags)
    {
        ArgumentNullException.ThrowIfNull(tags);

        var list = new JsonArray();
        foreach (var (name, description) in tags)
        {
            var tag = new JsonObject { ["name"] = name };
            if (!string.IsNullOrWhiteSpace(description))
                tag["description"] = description;
            list.Add(tag);
        }

        _root["tags"] = list;
    }

    /// <summary>Takes out every listed tag no remaining operation carries.</summary>
    /// <remarks>The reference page draws a heading for every listed tag, so an unused one is an empty section.</remarks>
    public void PruneTags()
    {
        if (_root["tags"] is not JsonArray tags)
            return;

        var used = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (_, node) in _paths)
        {
            if (node is not JsonObject item)
                continue;

            foreach (var (_, operation) in item.Where(IsOperation))
            {
                if (operation is JsonObject described && described["tags"] is JsonArray operationTags)
                    used.UnionWith(operationTags.OfType<JsonValue>().Select(TextOf).OfType<string>());
            }
        }

        var unused = tags.Where(tag => tag is not JsonObject listed || TextOf(listed["name"]) is not { } name || !used.Contains(name)).ToList();
        foreach (var tag in unused)
            tags.Remove(tag);
    }

    /// <summary>Takes out every schema no remaining operation reaches.</summary>
    /// <param name="logger">The logger.</param>
    /// <returns>The number of schemas taken out.</returns>
    /// <remarks>
    /// Without this a reference cut to twelve operations still carried every model in the API, which
    /// the page lists on its own: a caller who may query one data set was shown the shape of every
    /// administrative request (FDW-919).
    /// </remarks>
    public int PruneSchemas(ILogger logger)
    {
        if (_root["components"] is not JsonObject components || components["schemas"] is not JsonObject schemas)
            return 0;

        var reachable = new HashSet<string>(StringComparer.Ordinal);
        var pending = new Queue<string>();

        foreach (var (_, node) in _root.Where(entry => !string.Equals(entry.Key, "components", StringComparison.Ordinal)))
            CollectReferences(node, reachable, pending);

        foreach (var (_, node) in components.Where(entry => !string.Equals(entry.Key, "schemas", StringComparison.Ordinal)))
            CollectReferences(node, reachable, pending);

        while (pending.TryDequeue(out var name))
            CollectReferences(schemas[name], reachable, pending);

        var unused = schemas.Select(entry => entry.Key).Where(name => !reachable.Contains(name)).ToList();
        foreach (var name in unused)
            schemas.Remove(name);

        ApiReferenceLog.SchemasPruned(logger, unused.Count, schemas.Count);
        return unused.Count;
    }

    /// <summary>Takes out the stamps that say who may call each operation.</summary>
    /// <remarks>They are for cutting the document, not for its reader.</remarks>
    public void RemoveAccessStamps()
    {
        foreach (var (_, node) in _paths)
        {
            if (node is not JsonObject item)
                continue;

            foreach (var (_, operation) in item.Where(IsOperation))
            {
                if (operation is not JsonObject stamped)
                    continue;

                stamped.Remove(AuthorizationMetadataOperationProcessor.AnonymousExtension);
                stamped.Remove(AuthorizationMetadataOperationProcessor.PoliciesExtension);
            }
        }
    }

    /// <summary>Writes the document.</summary>
    /// <returns>The document's JSON.</returns>
    public string ToJson() => _root.ToJsonString();

    private static bool IsOperation(KeyValuePair<string, JsonNode?> entry) => OperationMethods.Contains(entry.Key);

    private static IGenericResult<int> CutPath(string path, JsonObject item, ApiReferenceCaller caller, ILogger logger)
    {
        var removed = 0;
        foreach (var (method, node) in item.Where(IsOperation).ToList())
        {
            var access = AccessOf(path, method, node, logger);
            if (access.IsFailure)
                return access.ToNewResult<int>();

            if (caller.Refusal(access.Value.IsAnonymous, access.Value.Policies) is not { } reason)
                continue;

            item.Remove(method);
            removed++;
            ApiReferenceLog.OperationRemoved(logger, method.ToUpperInvariant(), path, reason);
        }

        return GenericResult<int>.Success(removed);
    }

    private static IGenericResult<(bool IsAnonymous, IReadOnlyList<string> Policies)> AccessOf(
        string path, string method, JsonNode? node, ILogger logger)
    {
        if (node is JsonObject operation
            && operation[AuthorizationMetadataOperationProcessor.AnonymousExtension] is JsonValue anonymous
            && anonymous.TryGetValue(out bool isAnonymous)
            && operation[AuthorizationMetadataOperationProcessor.PoliciesExtension] is JsonArray policies)
        {
            IReadOnlyList<string> names = [.. policies.OfType<JsonValue>().Select(PolicyName).OfType<string>()];
            return GenericResult<(bool IsAnonymous, IReadOnlyList<string> Policies)>.Success((isAnonymous, names));
        }

        return Unreadable<(bool IsAnonymous, IReadOnlyList<string> Policies)>(
            logger, method.ToUpperInvariant() + " " + path + " has no record of who may call it");
    }

    private static string? PolicyName(JsonValue value) => value.TryGetValue(out string? name) ? name : null;

    private static string? TextOf(JsonNode? node) => node is JsonValue value && value.TryGetValue(out string? text) ? text : null;

    private static void CollectReferences(JsonNode? node, HashSet<string> reachable, Queue<string> pending)
    {
        if (node is JsonArray array)
        {
            foreach (var child in array)
                CollectReferences(child, reachable, pending);
            return;
        }

        if (node is not JsonObject obj)
            return;

        foreach (var (key, child) in obj)
        {
            if (string.Equals(key, "$ref", StringComparison.Ordinal) && SchemaName(child) is { } name)
            {
                if (reachable.Add(name))
                    pending.Enqueue(name);
                continue;
            }

            CollectReferences(child, reachable, pending);
        }
    }

    // A reference is a JSON pointer; '/' and '~' in a schema name are written ~1 and ~0.
    private static string? SchemaName(JsonNode? reference)
        => reference is JsonValue value
            && value.TryGetValue(out string? pointer)
            && pointer.StartsWith(SchemaReferencePrefix, StringComparison.Ordinal)
                ? pointer[SchemaReferencePrefix.Length..]
                    .Replace("~1", "/", StringComparison.Ordinal)
                    .Replace("~0", "~", StringComparison.Ordinal)
                : null;

    private static IGenericResult<T> Unreadable<T>(ILogger logger, string reason)
        => GenericResult<T>.Failure(
            ApiReferenceResultCodes.ByName("DocumentUnreadable"), logger, ResultDetails.Create("reason", reason));
}
