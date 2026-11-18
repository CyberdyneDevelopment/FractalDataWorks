using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Fdw.Data.Options;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Data.Abstractions.DataConfigurationRules;
using Fdw.Services.Data.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.DataConfigurationRules;

/// <summary>
/// What a rule and its values are checked against before anything is written (FDW-6345): the path through the option layer, the value count
/// the kind takes, and the owner and value kinds. Each reason is the contract's own coded message, and the option layer's path failures are
/// turned into the ones that say the same thing for a rule, so a caller sees one set.
/// </summary>
internal static class DataConfigurationRuleChecks
{
    /// <summary>Resolves the path and method of a rule to the kind they name, or says why they do not.</summary>
    internal static IGenericResult<IRuleKind> Kind(ILogger logger, string rule, IReadOnlyList<string>? path, string? method)
    {
        if (path is null || path.Count == 0)
            return GenericResult<IRuleKind>.Failure(DataOptionRulesLog.PathEmpty(logger, rule));

        var text = string.Join(DataConfigurationRuleRows.PathSeparator, path);
        for (var index = 0; index < path.Count; index++)
        {
            if (string.IsNullOrWhiteSpace(path[index]))
                return GenericResult<IRuleKind>.Failure(DataOptionRulesLog.PathSegmentEmpty(logger, text, index + 1));
        }

        if (string.IsNullOrWhiteSpace(method))
            return GenericResult<IRuleKind>.Failure(DataOptionRulesLog.MethodMissing(logger, text));

        var resolved = RulePaths.Resolve(logger, path, method);
        return resolved.IsSuccess && resolved.Value is { } kind
            ? GenericResult<IRuleKind>.Success(kind)
            : GenericResult<IRuleKind>.Failure(Mapped(logger, resolved, path, text, method));
    }

    /// <summary>Checks the owner kind against the owner kinds collection.</summary>
    internal static IGenericMessage? Owner(ILogger logger, string ownerKind)
    {
        var kind = DataOptionOwnerKinds.ByName(ownerKind);
        return ReferenceEquals(kind, DataOptionOwnerKinds.NotFound)
            ? DataOptionRulesLog.OwnerKindNotFound(logger, ownerKind, string.Join(", ", DataOptionOwnerKinds.All().Select(known => known.Name)))
            : null;
    }

    /// <summary>Checks the values of a rule: one is needed, each is stated whole, and the number is one the kind takes.</summary>
    internal static List<IGenericMessage> Values(
        ILogger logger, string rule, string pathText, IRuleKind? kind, IReadOnlyList<NewDataConfigurationRuleValue>? values)
    {
        var messages = new List<IGenericMessage>();
        if (values is null || values.Count == 0)
        {
            messages.Add(DataOptionRulesLog.ValueMissing(logger, rule));
            return messages;
        }

        for (var index = 0; index < values.Count; index++)
            messages.AddRange(One(logger, rule, index + 1, values[index]));

        if (kind is not null && (values.Count < kind.MinValues || (kind.MaxValues is { } most && values.Count > most)))
            messages.Add(DataOptionRulesLog.ValueCountNotTaken(logger, rule, pathText, Takes(kind), values.Count));

        return messages;
    }

    /// <summary>Checks one value on its own: its ordinal, its kind and that its content is the one member its kind holds.</summary>
    internal static List<IGenericMessage> One(ILogger logger, string rule, int position, NewDataConfigurationRuleValue value)
    {
        var messages = new List<IGenericMessage>();
        if (value.Ordinal is null)
            messages.Add(DataOptionRulesLog.ValueOrdinalMissing(logger, rule, position));

        if (string.IsNullOrWhiteSpace(value.Kind))
        {
            messages.Add(DataOptionRulesLog.ValueKindMissing(logger, rule, position));
            return messages;
        }

        var kind = DataOptionValueKinds.ByName(value.Kind);
        if (ReferenceEquals(kind, DataOptionValueKinds.NotFound))
        {
            messages.Add(DataOptionRulesLog.ValueKindNotFound(logger, value.Kind, string.Join(", ", DataOptionValueKinds.All().Select(known => known.Name))));
            return messages;
        }

        if (value.Value is not null && value.Parameter is not null)
            messages.Add(DataOptionRulesLog.ValueLiteralAndParameter(logger, rule, position));
        else if (string.Equals(kind.Member, "parameter", StringComparison.Ordinal) && string.IsNullOrWhiteSpace(value.Parameter))
            messages.Add(DataOptionRulesLog.ParameterNameMissing(logger, rule, position));
        else if (string.Equals(kind.Member, "value", StringComparison.Ordinal) && string.IsNullOrWhiteSpace(value.Value))
            messages.Add(DataOptionRulesLog.ValueMissing(logger, rule));

        return messages;
    }

    internal static string Takes(IRuleKind kind) =>
        kind.MaxValues is not { } most
            ? kind.MinValues.ToString(CultureInfo.InvariantCulture) + " or more"
            : most == kind.MinValues
                ? most.ToString(CultureInfo.InvariantCulture)
                : kind.MinValues.ToString(CultureInfo.InvariantCulture) + " to " + most.ToString(CultureInfo.InvariantCulture);

    // The option layer's failures say what happened to the path; the contract's messages say it for the rule.
    private static IGenericMessage Mapped(ILogger logger, IGenericResult<IRuleKind> failed, IReadOnlyList<string> path, string text, string method)
    {
        var details = failed.Details!;
        switch (failed.Code?.Name)
        {
            case "DataOptionPathLevelNotFound":
                var level = details.GetValue<int>("Level");
                return level == 1
                    ? DataOptionRulesLog.PathRootNotFound(logger, details.GetValue<string>("Name")!, nameof(Fdw.Data.Options.DataConfigurationRules))
                    : DataOptionRulesLog.PathSegmentNotFound(logger, level, details.GetValue<string>("Name")!, details.GetValue<string>("Known")!);
            case "DataOptionPathEndsBeforeKind":
                return DataOptionRulesLog.PathIncomplete(logger, text, details.GetValue<string>("Name")!, details.GetValue<string>("Known")!);
            case "DataOptionPathContinuesPastKind":
                var extra = details.GetValue<string>("Extra")!;
                return DataOptionRulesLog.PathSegmentNotFound(logger, path.ToList().IndexOf(extra) + 1, extra, string.Empty);
            default:
                return DataOptionRulesLog.MethodNotFound(logger, text, method, details.GetValue<string>("KindMethod")!);
        }
    }
}
