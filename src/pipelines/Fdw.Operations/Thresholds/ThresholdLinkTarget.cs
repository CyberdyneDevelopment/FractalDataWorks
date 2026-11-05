using Fdw.Operations.Abstractions.Thresholds;
using Fdw.Operations.Configuration;
using Fdw.Services.Data.Abstractions;

namespace Fdw.Operations.Thresholds;

/// <summary>What an attach or a detach acts on (FDW-3948): the kind of subject whose link table is written, the gateway onto the store, and the threshold.</summary>
/// <param name="Kind">The kind of subject.</param>
/// <param name="Gateway">The gateway onto the configuration store.</param>
/// <param name="Threshold">The threshold, of the kind the subject is.</param>
internal sealed record ThresholdLinkTarget(IThresholdSubjectKind Kind, IConfigurationGateway Gateway, IThresholdImplementationConfiguration Threshold);
