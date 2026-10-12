namespace Fdw.Web.Http.Abstractions.EndPoints;

/// <summary>
/// The <see cref="IStreamingResponse"/> instance attached to an endpoint's metadata.
/// </summary>
public sealed class StreamingResponseMetadata : IStreamingResponse
{
    /// <summary>Gets the shared instance. The marker carries no state, so one is enough.</summary>
    public static StreamingResponseMetadata Instance { get; } = new();

    private StreamingResponseMetadata()
    {
    }
}
