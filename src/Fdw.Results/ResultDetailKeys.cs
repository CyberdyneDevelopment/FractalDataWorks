namespace Fdw.Results;

/// <summary>
/// Names of result-detail entries that one assembly writes and another reads.
/// </summary>
/// <remarks>
/// A client captures what a downstream host said about its own failure; the endpoint that answers the caller reads it
/// back. Both name the entry through these constants, so the two cannot drift apart.
/// </remarks>
public static class ResultDetailKeys
{
    /// <summary>
    /// The <c>referenceId</c> a downstream host put in its problem body. The endpoint that relays the failure reports it as
    /// its own <c>referenceId</c>, so one id traces the call through every host it crossed.
    /// </summary>
    public const string UpstreamReferenceId = "upstreamReferenceId";

    /// <summary>
    /// The <c>code</c> a downstream host put in its problem body (for example <c>SERVICES-61001</c>), relayed as
    /// <c>upstreamCode</c> beside the relaying host's own code.
    /// </summary>
    public const string UpstreamCode = "upstreamCode";

    /// <summary>
    /// The id of the execution a downstream host closed when it refused a trigger, as its answer states it. A relaying host reports it beside the reason, as
    /// <c>executionId</c>, so the caller can read the record of the run that was not taken.
    /// </summary>
    public const string ExecutionId = "executionId";
}
