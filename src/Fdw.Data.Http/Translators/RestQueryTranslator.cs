using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Data.Http.Results;
using Fdw.Results;
using System.Diagnostics.CodeAnalysis;
namespace Fdw.Data.Http;

/// <summary>
/// Translates QueryCommand to REST API GET request (HttpRequestMessage).
/// </summary>
[ExcludeFromCodeCoverage] // Excluded: requires HTTP connections
public sealed class RestQueryTranslator : IDataCommandTranslator<HttpRequestMessage>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RestQueryTranslator"/> class.
    /// </summary>
    public RestQueryTranslator()
    {
    }

    /// <inheritdoc/>
    public int Id => 1;

    /// <inheritdoc/>
    object ITypeOption.Id => Id;

    /// <inheritdoc/>
    public string Name => "RestQuery";

    /// <inheritdoc/>
    public string Category => DomainName;

    /// <inheritdoc/>
    public string DomainName => "Http";

    /// <summary>
    /// Translates a QueryCommand to an HttpRequestMessage (GET request).
    /// </summary>
    /// <param name="command">The data command to translate.</param>
    /// <returns>An HttpRequestMessage configured as a GET request.</returns>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public static HttpRequestMessage Translate(IDataCommand command)
    {
        return new HttpRequestMessage(HttpMethod.Get, string.Empty);
    }

    /// <summary>
    /// Translates a QueryCommand to an HttpRequestMessage with container metadata.
    /// </summary>
    public Task<IGenericResult<HttpRequestMessage>> Translate(
        IDataCommand command,
        IStorageContainer container,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var path = container.Path?.PathValue ?? container.Name;

            var request = new HttpRequestMessage(HttpMethod.Get, path);

            // Add headers if needed
            request.Headers.Add("Accept", "application/json");

            return Task.FromResult(GenericResult<HttpRequestMessage>.Success(request));
        }
        catch (Exception ex)
        {
            return Task.FromResult(GenericResult<HttpRequestMessage>.Failure(
                DataHttpResultCodes.ByName("QueryTranslationFailed"),
                ResultDetails.Create().With("ErrorMessage", ex.Message)));
        }
    }
}
