using Fdw.Collections;

namespace Fdw.Services.Etl.Execution.Claims;

/// <summary>Whether a run holds its claim or waits for it (FDW-6629).</summary>
public interface IPipelineRunClaimState : ITypeOption<int, PipelineRunClaimStateBase>
{
}
