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
    // Why: The static single-arg overload was used when addressing was on the command.
    // Commands are now address-free; callers that previously used this overload must
    // supply a container. This overload is kept for API surface compatibility but
    // produces an empty path — any caller that matters uses the container overload.
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public static HttpRequestMessage Translate(IDataCommand command)
    {
        // Why: No container available — cannot resolve a path. Return empty-path request.
        // Real execution always flows through Translate(command, container, ct).
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
            // Why: Addressing comes from the container (resolved before translation), not the command.
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
