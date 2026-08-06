using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Data.OData.Results;
using Fdw.Results;

namespace Fdw.Data.OData;

/// <summary>
/// Translates InsertCommand to REST POST request with JSON body.
/// </summary>
/// <remarks>
/// <para>
/// Builds HTTP POST requests for creating new resources:
/// <list type="bullet">
/// <item>Method: POST</item>
/// <item>Path: Container name (e.g., "/api/Customers")</item>
/// <item>Body: JSON serialization of entity data</item>
/// <item>Content-Type: application/json</item>
/// </list>
/// </para>
/// <para>
/// JSON serialization is performed using System.Text.Json.
/// </para>
/// </remarks>
[TypeOption(typeof(ODataCommandTranslators), "ODataInsert", RestrictToCurrentCompilation = true)]
public sealed class ODataInsertTranslator : ODataCommandTranslatorBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ODataInsertTranslator"/> class.
    /// </summary>
    public ODataInsertTranslator()
        : base("ODataInsert")
    {
    }

    /// <summary>
    /// Translates an InsertCommand to a REST POST request.
    /// </summary>
    /// <param name="command">The data command to translate.</param>
    /// <param name="container">The container with schema metadata.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the HttpRequestMessage.</returns>
    public override Task<IGenericResult<HttpRequestMessage>> Translate(
        IDataCommand command,
        IStorageContainer container,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (container == null)
            {
                return Task.FromResult<IGenericResult<HttpRequestMessage>>(
                    GenericResult<HttpRequestMessage>.Failure(
                        ODataResultCodes.ByName("ContainerNull")));
            }

            // Get entity data from metadata
            if (command.Metadata == null || !command.Metadata.TryGetValue("Data", out var dataObj) || dataObj == null)
            {
                return Task.FromResult<IGenericResult<HttpRequestMessage>>(
                    GenericResult<HttpRequestMessage>.Failure(
                        ODataResultCodes.ByName("InsertDataRequired")));
            }

            // Build relative path from container name
            var relativePath = container.Name.StartsWith('/')
                ? container.Name
                : $"/{container.Name}";

            // Serialize data to JSON (simple System.Text.Json serialization)
            var jsonBody = System.Text.Json.JsonSerializer.Serialize(dataObj);

            // Get HTTP POST request with JSON body
            var request = new HttpRequestMessage(HttpMethod.Post, relativePath)
            {
                Content = new StringContent(jsonBody, Encoding.UTF8, "application/json")
            };

            return Task.FromResult<IGenericResult<HttpRequestMessage>>(
                GenericResult<HttpRequestMessage>.Success(request));
        }
        catch (Exception ex)
        {
            return Task.FromResult<IGenericResult<HttpRequestMessage>>(
                GenericResult<HttpRequestMessage>.Failure(
                    ODataResultCodes.ByName("InsertTranslationFailed"),
                    ResultDetails.Create().With("ErrorMessage", ex.Message)));
        }
    }
}
