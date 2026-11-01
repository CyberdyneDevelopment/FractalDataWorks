namespace Fdw.Services.Etl;

/// <summary>
/// A pipeline implementation whose runs can be made to wait for approval (FDW-1937): a reader asks for the fact without asking what the
/// engine is, as it does for <see cref="IEnablablePipelineConfiguration"/>.
/// </summary>
public interface IApprovalGatedPipelineConfiguration
{
    /// <summary>Gets a value indicating whether a run waits until someone who holds <c>pipelines:approve</c> approves it.</summary>
    bool RequireApprovalToRun { get; }
}
