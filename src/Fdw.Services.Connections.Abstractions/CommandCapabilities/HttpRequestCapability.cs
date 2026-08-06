using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.Abstractions.CommandCapabilities;

/// <summary>
/// HTTP request capability — sends an HTTP request through the connection.
/// Used by HTTP connection types (REST, OData, GraphQL, SOAP).
/// </summary>
/// <remarks>
/// Configuration keys:
/// <list type="bullet">
///   <item><c>Url</c> — relative or absolute URL (required).</item>
///   <item><c>Method</c> — HTTP method: GET, POST, PUT, DELETE, PATCH.</item>
///   <item><c>Headers</c> — JSON array of <c>{"Key":"…","Value":"…"}</c> header entries.</item>
///   <item><c>Body</c> — request body for POST/PUT/PATCH methods.</item>
/// </list>
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(CommandCapabilityTypes), "HttpRequest", RestrictToCurrentCompilation = true)]
public sealed class HttpRequestCapability : CommandCapabilityTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HttpRequestCapability"/> class.
    /// </summary>
    public HttpRequestCapability()
        : base(
            id: 7,
            name: "HttpRequest",
            displayName: "HTTP Request",
            configurationFields:
            [
                new ConfigurationFieldDescriptor(
                    Key: "Url",
                    Label: "URL",
                    Placeholder: "/api/v1/customers",
                    InputKind: ConfigurationFieldKinds.Text,
                    IsRequired: true),
                new ConfigurationFieldDescriptor(
                    Key: "Method",
                    Label: "Method",
                    Placeholder: string.Empty,
                    InputKind: ConfigurationFieldKinds.Select,
                    SelectOptions: ["GET:GET", "POST:POST", "PUT:PUT", "DELETE:DELETE", "PATCH:PATCH"],
                    IsRequired: true),
                new ConfigurationFieldDescriptor(
                    Key: "Headers",
                    Label: "Headers",
                    Placeholder: "Additional request headers",
                    InputKind: ConfigurationFieldKinds.KeyValueList),
                new ConfigurationFieldDescriptor(
                    Key: "Body",
                    Label: "Body",
                    Placeholder: "Request body (JSON, XML, etc.)",
                    InputKind: ConfigurationFieldKinds.Textarea),
            ])
    {
    }
}
