using System;
using System.Net.Http.Headers;
using Fdw.Results;
using Fdw.Services.Data.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Refuses a write target a container states that no destination could send to: where the rows go, with which HTTP method
/// and as which content type (FDW-1757).
/// </summary>
/// <remarks>
/// The target is stated on any container and never refused by connection type: that is invisible here. A destination that
/// needs a part the container leaves out fails naming it when it runs. Nothing stated is rewritten and nothing unstated is
/// defaulted.
/// </remarks>
public static class DataStoreContainerWriteTarget
{
    /// <summary>
    /// Refuses a write method other than POST or PUT, an endpoint that is not a path after the connection's address, and a
    /// content type that is not a media type.
    /// </summary>
    /// <param name="containerName">The container, for the refusal.</param>
    /// <param name="endpoint">The path the rows are sent to, or null when not stated.</param>
    /// <param name="method">The HTTP method, or null when not stated.</param>
    /// <param name="contentType">The content type, or null when not stated.</param>
    /// <param name="logger">The logger a refusal is written to.</param>
    /// <returns>The refusal, a validation result whose code carries the reason for the caller; null when every part stated is one a destination can use.</returns>
    public static IGenericResult? Refusal(string containerName, string? endpoint, string? method, string? contentType, ILogger logger)
    {
        if (method is not null && !IsPostOrPut(method))
        {
            return GenericResult.Failure(
                DataServiceResultCodes.ByName("ContainerWriteMethodInvalid"), logger,
                ResultDetails.Create("container", containerName, "method", method));
        }

        if (endpoint is not null && !IsPathAfterTheAddress(endpoint))
        {
            return GenericResult.Failure(
                DataServiceResultCodes.ByName("ContainerWriteEndpointInvalid"), logger,
                ResultDetails.Create("container", containerName, "endpoint", endpoint));
        }

        return contentType is not null && !MediaTypeHeaderValue.TryParse(contentType, out _)
            ? GenericResult.Failure(
                DataServiceResultCodes.ByName("ContainerWriteContentTypeInvalid"), logger,
                ResultDetails.Create("container", containerName, "contentType", contentType))
            : null;
    }

    private static bool IsPostOrPut(string method)
        => string.Equals(method, "POST", StringComparison.OrdinalIgnoreCase)
            || string.Equals(method, "PUT", StringComparison.OrdinalIgnoreCase);

    // A full web address would replace the connection's own address and send the rows to another host, so only a relative path is taken.
    private static bool IsPathAfterTheAddress(string endpoint)
        => !string.IsNullOrWhiteSpace(endpoint)
            && !endpoint.StartsWith("//", StringComparison.Ordinal)
            && Uri.TryCreate(endpoint, UriKind.Relative, out var path)
            && !path.IsAbsoluteUri;
}
