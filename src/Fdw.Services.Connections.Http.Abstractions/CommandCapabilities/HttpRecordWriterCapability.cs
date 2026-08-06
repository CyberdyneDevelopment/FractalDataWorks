using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Abstractions.CommandCapabilities;

namespace Fdw.Services.Connections.Http.Abstractions.CommandCapabilities;

/// <summary>
/// HTTP record writer capability — writes a serialized batch of records to an HTTP endpoint.
/// Used by HTTP connection types that support write operations via <see cref="IHttpRecordWriterConnection"/>.
/// </summary>
/// <remarks>
/// Configuration keys:
/// <list type="bullet">
///   <item><c>Endpoint</c> — path relative to the connection's base URL to POST/PUT records (required).</item>
///   <item><c>Method</c> — HTTP method to use: PUT or POST (required).</item>
///   <item><c>ContentType</c> — Content-Type header value; e.g. <c>application/json</c>.</item>
/// </list>
/// Serialization format is driven by the container's configured <c>Format</c> (Json, Xml, Delimited, etc.)
/// via <c>RecordWriterTypes.ByName(format).Create(context)</c> — no per-format branching in the connector.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(CommandCapabilityTypes), "HttpRecordWriter", RestrictToCurrentCompilation = true)]
public sealed class HttpRecordWriterCapability : CommandCapabilityTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HttpRecordWriterCapability"/> class.
    /// </summary>
    public HttpRecordWriterCapability()
        : base(
            id: 12,
            name: "HttpRecordWriter",
            displayName: "HTTP Record Writer",
            configurationFields:
            [
                new ConfigurationFieldDescriptor(
                    Key: "Endpoint",
                    Label: "Endpoint Path",
                    Placeholder: "/api/v1/ingest",
                    InputKind: ConfigurationFieldKinds.Text,
                    IsRequired: true),
                new ConfigurationFieldDescriptor(
                    Key: "Method",
                    Label: "HTTP Method",
                    Placeholder: "PUT",
                    InputKind: ConfigurationFieldKinds.Select,
                    SelectOptions: ["PUT:PUT", "POST:POST"],
                    IsRequired: true),
                new ConfigurationFieldDescriptor(
                    Key: "ContentType",
                    Label: "Content-Type Header",
                    Placeholder: "application/json",
                    InputKind: ConfigurationFieldKinds.Text,
                    IsRequired: true),
            ])
    {
    }
}
