using Fdw.Services.Etl.Abstractions.OptionTypes;

namespace Fdw.Services.Etl.Execution.Claims;

/// <summary>One key a pipeline run claims (FDW-6629).</summary>
/// <param name="Type">The kind of key.</param>
/// <param name="ClaimKey">The text the claim procedures compare; built by <see cref="PipelineRunClaimKeys"/>.</param>
/// <param name="Cap">How many runs of the pipeline may be claimed at once; stated on a pipeline cap key and on no other.</param>
public sealed record PipelineRunClaimKey(IClaimKeyType Type, string ClaimKey, int? Cap);
