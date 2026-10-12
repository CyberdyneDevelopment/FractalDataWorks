using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using Fdw.Services.Connections.Abstractions;
using Namotion.Reflection;
using NJsonSchema;
using NSwag;
using NSwag.Generation.Processors;
using NSwag.Generation.Processors.Contexts;

namespace Fdw.Services.Connections.Endpoints.OpenApi;

/// <summary>
/// Publishes every connection type's own configuration shape in the OpenAPI document, so a client sees the
/// real fields of an Sftp or Azure Blob connection rather than one SQL-shaped request.
/// </summary>
/// <remarks>
/// <para>
/// <c>POST /connections</c>: the request body becomes a <c>oneOf</c> with one variant per connection type
/// (<c>Create{Type}ConnectionRequest</c> = the shared request plus that type's <c>configuration</c>), and a
/// discriminator on <c>serviceType</c> picks the variant.
/// </para>
/// <para>
/// <c>PATCH /connections/{name}</c>: an update carries no <c>serviceType</c> -- the stored connection's type
/// decides -- so its <c>configuration</c> is a <c>oneOf</c> of the type schemas without a discriminator.
/// </para>
/// <para>
/// The schemas come from each type's <see cref="IConnectionEndpointBehavior.ConfigurationType"/>, the same
/// class the endpoints bind into, less the members every connection shares (name, health checks, discovery),
/// which are the request's own top-level fields. A connection type added later shows up with no change here.
/// </para>
/// </remarks>
public sealed class ConnectionConfigurationDocumentProcessor : IDocumentProcessor
{
    private const string ServiceTypeProperty = "serviceType";
    private const string ConfigurationProperty = "configuration";
    private const string JsonContent = "application/json";

    private static readonly TimeSpan PathMatchTimeout = TimeSpan.FromSeconds(1);

    private static readonly Regex CollectionPath = new(@"/connections$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, PathMatchTimeout);
    private static readonly Regex MemberPath = new(@"/connections/\{[^}/]+\}$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, PathMatchTimeout);

    private static readonly HashSet<string> SharedMembers = new(
        typeof(IConnectionImplementationConfiguration).GetInterfaces()
            .Append(typeof(IConnectionImplementationConfiguration))
            .SelectMany(contract => contract.GetProperties())
            .Select(property => JsonNamingPolicy.CamelCase.ConvertName(property.Name)),
        StringComparer.OrdinalIgnoreCase);

    private readonly IEnumerable<IConnectionType> _connectionTypes;

    /// <summary>Publishes every registered connection type (<see cref="ConnectionTypes"/>).</summary>
    public ConnectionConfigurationDocumentProcessor()
        : this(ConnectionTypes.All().Values)
    {
    }

    /// <summary>Publishes the given connection types.</summary>
    /// <param name="connectionTypes">The connection types whose configurations the document shows.</param>
    public ConnectionConfigurationDocumentProcessor(IEnumerable<IConnectionType> connectionTypes)
    {
        _connectionTypes = connectionTypes;
    }

    /// <inheritdoc />
    public void Process(DocumentProcessorContext context)
    {
        var settings = _connectionTypes
            .Where(type => IsBindable(type.EndpointBehavior.ConfigurationType))
            .OrderBy(type => type.Name, StringComparer.Ordinal)
            .Select(type => new TypeSettings(type.Name, SettingsSchema(context, type.EndpointBehavior.ConfigurationType)))
            .ToList();
        if (settings.Count == 0)
            return;

        foreach (var path in context.Document.Paths)
        {
            if (CollectionPath.IsMatch(path.Key) && path.Value.TryGetValue(OpenApiOperationMethod.Post, out var create))
                PublishCreate(context, create, settings);
            else if (MemberPath.IsMatch(path.Key) && path.Value.TryGetValue(OpenApiOperationMethod.Patch, out var update))
                PublishUpdate(update, settings);
        }
    }

    private static bool IsBindable(Type configurationType) =>
        !configurationType.IsAbstract
        && !configurationType.IsInterface
        && typeof(IConnectionImplementationConfiguration).IsAssignableFrom(configurationType);

    private static JsonSchema SettingsSchema(DocumentProcessorContext context, Type configurationType)
    {
        var generated = context.SchemaGenerator.GenerateWithReference<JsonSchema>(
            configurationType.ToContextualType(), context.SchemaResolver);
        var definition = generated.ActualSchema;
        RemoveSharedMembers(definition);
        return definition;
    }

    // Why the allOf chain too: a configuration that derives from a base class (Http's does) is generated as
    // allOf [base, own], and the shared members are declared on the base.
    private static void RemoveSharedMembers(JsonSchema schema)
    {
        foreach (var shared in schema.Properties.Keys.Where(SharedMembers.Contains).ToList())
            schema.Properties.Remove(shared);

        foreach (var inherited in schema.AllOf.Select(part => part.ActualSchema).Where(part => !ReferenceEquals(part, schema)))
            RemoveSharedMembers(inherited);
    }

    private static void PublishCreate(DocumentProcessorContext context, OpenApiOperation create, IReadOnlyList<TypeSettings> settings)
    {
        if (JsonBody(create) is not { Schema: { } request } media)
            return;

        var discriminator = new OpenApiDiscriminator { PropertyName = ServiceTypeProperty };
        var body = new JsonSchema { DiscriminatorObject = discriminator };
        foreach (var type in settings)
        {
            var variant = CreateVariant(request.ActualSchema, type);
            context.Document.Definitions[$"Create{type.Name}ConnectionRequest"] = variant;
            body.OneOf.Add(new JsonSchema { Reference = variant });
            discriminator.Mapping[type.Name] = new JsonSchema { Reference = variant };
        }

        media.Schema = body;
    }

    private static JsonSchema CreateVariant(JsonSchema request, TypeSettings type)
    {
        var serviceType = new JsonSchemaProperty { Type = JsonObjectType.String };
        serviceType.Enumeration.Add(type.Name);

        var own = new JsonSchema { Type = JsonObjectType.Object };
        own.Properties[ServiceTypeProperty] = serviceType;
        own.Properties[ConfigurationProperty] = new JsonSchemaProperty { Reference = type.Settings };

        var variant = new JsonSchema { Description = $"Creates a {type.Name} connection." };
        variant.AllOf.Add(new JsonSchema { Reference = request });
        variant.AllOf.Add(own);
        return variant;
    }

    private static void PublishUpdate(OpenApiOperation update, IReadOnlyList<TypeSettings> settings)
    {
        if (JsonBody(update) is not { Schema: { } request }
            || !request.ActualSchema.Properties.TryGetValue(ConfigurationProperty, out var configuration))
            return;

        foreach (var type in settings)
            configuration.OneOf.Add(new JsonSchema { Reference = type.Settings });
    }

    private static OpenApiMediaType? JsonBody(OpenApiOperation operation) =>
        operation.RequestBody is { } body && body.Content.TryGetValue(JsonContent, out var media) ? media : null;

    private sealed record TypeSettings(string Name, JsonSchema Settings);
}
