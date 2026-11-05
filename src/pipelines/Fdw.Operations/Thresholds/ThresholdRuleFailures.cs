using System;
using Fdw.Operations.Abstractions.Results;
using Fdw.Results;

namespace Fdw.Operations.Thresholds;

/// <summary>The failures of reading a stored threshold (FDW-3936).</summary>
internal static class ThresholdRuleFailures
{
    /// <summary>A rule that cannot be used.</summary>
    /// <param name="threshold">The threshold.</param>
    /// <param name="rule">The rule, counted from 1.</param>
    /// <param name="reason">What is wrong with it.</param>
    /// <returns>The failure.</returns>
    public static IGenericResult RuleInvalid(string threshold, int rule, string reason) =>
        GenericResult.Failure(
            OperationsResultCodes.ByName("ThresholdRuleInvalid"),
            ResultDetails.Create().With("Threshold", threshold).With("Rule", rule).With("Reason", reason));

    /// <summary>A parameter that cannot be used.</summary>
    /// <param name="threshold">The threshold.</param>
    /// <param name="parameter">The parameter name.</param>
    /// <param name="reason">What is wrong with it.</param>
    /// <returns>The failure.</returns>
    public static IGenericResult ParameterInvalid(string threshold, string parameter, string reason) =>
        GenericResult.Failure(
            OperationsResultCodes.ByName("ThresholdParameterInvalid"),
            ResultDetails.Create().With("Threshold", threshold).With("Reason", $"'{parameter}': {reason}"));

    /// <summary>A threshold whose type is not a threshold type.</summary>
    /// <param name="threshold">The threshold.</param>
    /// <param name="type">The type name it carries.</param>
    /// <returns>The failure.</returns>
    public static IGenericResult TypeUnknown(string threshold, string type) =>
        GenericResult.Failure(
            OperationsResultCodes.ByName("ThresholdTypeUnknown"),
            ResultDetails.Create().With("Threshold", threshold).With("Type", type));

    /// <summary>A subject kind that has no link table.</summary>
    /// <param name="kind">The kind.</param>
    /// <returns>The failure.</returns>
    public static IGenericResult KindUnknown(string kind) =>
        GenericResult.Failure(OperationsResultCodes.ByName("ThresholdSubjectKindUnknown"), ResultDetails.Create().With("Kind", kind));

    /// <summary>A threshold attached to a subject of another kind than it judges.</summary>
    /// <param name="threshold">The threshold.</param>
    /// <param name="thresholdKind">The kind it judges.</param>
    /// <param name="kind">The kind it is attached to.</param>
    /// <returns>The failure.</returns>
    public static IGenericResult KindMismatch(string threshold, string thresholdKind, string kind) =>
        GenericResult.Failure(
            OperationsResultCodes.ByName("ThresholdSubjectKindMismatch"),
            ResultDetails.Create().With("Threshold", threshold).With("ThresholdKind", thresholdKind).With("Kind", kind));

    /// <summary>A threshold that does not exist.</summary>
    /// <param name="threshold">The name asked for.</param>
    /// <returns>The failure.</returns>
    public static IGenericResult NotFound(string threshold) =>
        GenericResult.Failure(OperationsResultCodes.ByName("ThresholdNotFound"), ResultDetails.Create().With("Threshold", threshold));

    /// <summary>A threshold attached to a subject that already has it.</summary>
    /// <param name="threshold">The threshold.</param>
    /// <param name="kind">The kind of subject.</param>
    /// <param name="subject">The subject's Id.</param>
    /// <returns>The failure.</returns>
    public static IGenericResult LinkExists(string threshold, string kind, Guid subject) =>
        GenericResult.Failure(
            OperationsResultCodes.ByName("ThresholdLinkExists"),
            ResultDetails.Create().With("Threshold", threshold).With("Kind", kind).With("Subject", subject.ToString("D")));

    /// <summary>A threshold detached from a subject that does not have it.</summary>
    /// <param name="threshold">The threshold.</param>
    /// <param name="kind">The kind of subject.</param>
    /// <param name="subject">The subject's Id.</param>
    /// <returns>The failure.</returns>
    public static IGenericResult LinkAbsent(string threshold, string kind, Guid subject) =>
        GenericResult.Failure(
            OperationsResultCodes.ByName("ThresholdLinkAbsent"),
            ResultDetails.Create().With("Threshold", threshold).With("Kind", kind).With("Subject", subject.ToString("D")));
}
