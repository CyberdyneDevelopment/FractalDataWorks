using Fdw.Operations.Abstractions.Results;
using Fdw.Results;

namespace Fdw.Operations.Abstractions.Thresholds;

/// <summary>The failures a consequence answers when it cannot act (FDW-3936).</summary>
internal static class EscalationFailures
{
    /// <summary>The host registered no service for the consequence.</summary>
    /// <param name="firing">The rule that matched.</param>
    /// <param name="escalation">The consequence.</param>
    /// <param name="service">The service it needed.</param>
    /// <returns>The failure.</returns>
    public static IGenericResult ServiceMissing(EscalationFiring firing, string escalation, string service) =>
        GenericResult.Failure(
            OperationsResultCodes.ByName("ThresholdServiceMissing"),
            ResultDetails.Create().With("Service", service).With("Escalation", escalation).With("Threshold", firing.ThresholdName));

    /// <summary>The consequence acts on a run and the subject gave none.</summary>
    /// <param name="firing">The rule that matched.</param>
    /// <param name="escalation">The consequence.</param>
    /// <returns>The failure.</returns>
    public static IGenericResult RunMissing(EscalationFiring firing, string escalation) =>
        GenericResult.Failure(
            OperationsResultCodes.ByName("ThresholdRunMissing"),
            ResultDetails.Create().With("Escalation", escalation).With("Threshold", firing.ThresholdName));

    /// <summary>The consequence needs a policy and the rule names none.</summary>
    /// <param name="firing">The rule that matched.</param>
    /// <param name="escalation">The consequence.</param>
    /// <returns>The failure.</returns>
    public static IGenericResult PolicyMissing(EscalationFiring firing, string escalation) =>
        GenericResult.Failure(
            OperationsResultCodes.ByName("ThresholdPolicyMissing"),
            ResultDetails.Create().With("Escalation", escalation).With("Threshold", firing.ThresholdName));
}
