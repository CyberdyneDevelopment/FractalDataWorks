using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Endpoints;
using Fdw.Services.Connections.Endpoints.OpenApi;
using Fdw.Services.Connections.MsSql;
using Namotion.Reflection;
using NJsonSchema;
using NJsonSchema.Generation;
using NSwag;
using NSwag.Generation;
using NSwag.Generation.AspNetCore;
using NSwag.Generation.Processors.Contexts;

namespace Fdw.Services.Connections.Tests.EndpointBehavior;

/// <summary>
/// The OpenAPI document publishes one configuration schema per connection type, so a client sees each
/// type's real fields.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ConnectionConfigurationDocumentProcessorTests
{
    private const string CollectionPath = "/api/v1/connections";
    private const string MemberPath = "/api/v1/connections/{name}";

    [Fact]
    [Trait("Priority", "P1")]
    public void CreateBodyIsOneVariantPerConnectionTypeChosenByServiceType()
    {
        var document = Process();

        var body = RequestSchema(document, CollectionPath, OpenApiOperationMethod.Post);
        body.OneOf.Count.ShouldBe(3);
        var discriminator = body.DiscriminatorObject.ShouldNotBeNull();
        discriminator.PropertyName.ShouldBe("serviceType");
        discriminator.Mapping.Keys.OrderBy(key => key, StringComparer.Ordinal)
            .ShouldBe(new[] { "AzureBlob", "MsSql", "Sftp" });
        document.Definitions.ContainsKey("CreateSftpConnectionRequest").ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void UpdateConfigurationIsOneOfTheTypeSchemas()
    {
        var document = Process();

        var request = RequestSchema(document, MemberPath, OpenApiOperationMethod.Patch).ActualSchema;
        // The property's own nullable/any alternatives stay in the oneOf; what the processor adds is one reference per type schema.
        var alternatives = PropertyOf(request, "configuration").ShouldNotBeNull().OneOf;
        new[] { "AzureBlobTestConfiguration", "MsSqlConnectionConfiguration", "SftpTestConfiguration" }
            .Count(name => alternatives.Any(alternative => ReferenceEquals(alternative.Reference, document.Definitions[name])))
            .ShouldBe(3);
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void EachTypeSchemaShowsItsOwnFieldsAndNotTheSharedOnes()
    {
        var document = Process();

        // Snapshot: the fields each connection type publishes (sorted), less the shared members.
        var snapshot = string.Join(
            " | ",
            new[] { "AzureBlob", "Sftp" }.Select(type => $"{type}: {string.Join(", ", SettingsFields(document, type))}"));

        snapshot.ShouldBe(
            "AzureBlob: accountName, connectionStringSecretName, containerName, metadata | "
            + "Sftp: host, port, secretKeyName, secretManagerName, username");

        var msSql = SettingsFields(document, "MsSql");
        msSql.ShouldContain("server");
        msSql.ShouldContain("additionalProperties");
        msSql.ShouldContain("encrypt");
        msSql.ShouldNotContain("name");
        msSql.ShouldNotContain("healthCheckEnabled");
    }

    private static OpenApiDocument Process()
    {
        var settings = new AspNetCoreOpenApiDocumentGeneratorSettings
        {
            SchemaSettings = new SystemTextJsonSchemaGeneratorSettings
            {
                SerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web),
            },
        };
        var document = new OpenApiDocument();
        var resolver = new OpenApiSchemaResolver(document, settings.SchemaSettings);
        var generator = new OpenApiSchemaGenerator(settings);

        var create = generator.GenerateWithReference<JsonSchema>(typeof(CreateConnectionRequest).ToContextualType(), resolver);
        var update = generator.GenerateWithReference<JsonSchema>(typeof(UpdateConnectionRequest).ToContextualType(), resolver);
        document.Paths[CollectionPath] = new OpenApiPathItem { [OpenApiOperationMethod.Post] = WithBody(create) };
        document.Paths[MemberPath] = new OpenApiPathItem { [OpenApiOperationMethod.Patch] = WithBody(update) };

        var types = new[]
        {
            ConnectionType("Sftp", typeof(SftpTestConfiguration)),
            ConnectionType("AzureBlob", typeof(AzureBlobTestConfiguration)),
            ConnectionType("MsSql", typeof(MsSqlConnectionConfiguration)),
        };

        var context = new DocumentProcessorContext(document, [], [], resolver, generator, settings);
        new ConnectionConfigurationDocumentProcessor(types).Process(context);
        return document;
    }

    private static IConnectionType ConnectionType(string name, Type configurationType)
    {
        var type = new Mock<IConnectionType>();
        type.SetupGet(t => t.Name).Returns(name);
        type.SetupGet(t => t.EndpointBehavior).Returns(new ConnectionEndpointBehavior(configurationType));
        return type.Object;
    }

    private static OpenApiOperation WithBody(JsonSchema schema)
    {
        var operation = new OpenApiOperation { RequestBody = new OpenApiRequestBody() };
        operation.RequestBody.Content["application/json"] = new OpenApiMediaType { Schema = schema };
        return operation;
    }

    private static JsonSchemaProperty? PropertyOf(JsonSchema schema, string name) =>
        schema.Properties.TryGetValue(name, out var own)
            ? own
            : schema.AllOf.Select(part => part.ActualSchema).Where(part => !ReferenceEquals(part, schema))
                .Select(part => PropertyOf(part, name)).FirstOrDefault(found => found is not null);

    private static JsonSchema RequestSchema(OpenApiDocument document, string path, string method) =>
        document.Paths[path][method].RequestBody!.Content["application/json"].Schema!;

    // The fields a type's create variant publishes under `configuration`, sorted.
    private static List<string> SettingsFields(OpenApiDocument document, string type)
    {
        var variant = document.Definitions[$"Create{type}ConnectionRequest"];
        var own = variant.AllOf.Single(part => part.Properties.ContainsKey("configuration"));
        var settings = own.Properties["configuration"].ActualSchema;
        return settings.ActualProperties.Keys.OrderBy(key => key, StringComparer.Ordinal).ToList();
    }
}
