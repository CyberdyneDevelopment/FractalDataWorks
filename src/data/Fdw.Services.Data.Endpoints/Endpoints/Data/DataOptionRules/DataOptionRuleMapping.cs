using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Data.Abstractions.DataConfigurationRules;
using Fdw.Services.Data.Clients.Models;
using Fdw.Services.Data.Endpoints.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// The mapping of the data option rule routes (FDW-6489): a request body becomes the service's types, and a service rule becomes the contract's payload with the names the
/// directory gives. A name the directory cannot give fails the answer with the directory's own code: it is never an empty text.
/// </summary>
internal static class DataOptionRuleMapping
{
    /// <summary>The container the route names.</summary>
    public static DataConfigurationRuleContainer Target(DataOptionContainerRequest request) => new()
    {
        DataStore = request.Name,
        DataPath = request.PathName,
        Container = request.ContainerName,
    };

    /// <summary>The rule the route names.</summary>
    public static DataConfigurationRuleKey Key(DataOptionRuleKeyRequest request) => new() { Target = Target(request), RuleId = request.RuleId };

    /// <summary>A body field that names a target must be the route's; a body that leaves it out takes the route's.</summary>
    public static IGenericResult Matches(ILogger logger, NewDataOptionRuleBody body, DataOptionContainerRequest route)
    {
        (string Field, string? Body, string Route)[] fields =
        [
            ("data store", body.DataStore, route.Name),
            ("path", body.DataPath, route.PathName),
            ("container", body.Container, route.ContainerName),
        ];
        foreach (var (field, bodyValue, routeValue) in fields)
        {
            if (bodyValue is not null && !string.Equals(bodyValue, routeValue, StringComparison.OrdinalIgnoreCase))
                return GenericResult.Failure(DataOptionRulesEndpointLog.RuleBodyTargetDiffers(logger, field, bodyValue, routeValue));
        }

        return GenericResult.Success();
    }

    /// <summary>The rule to add.</summary>
    public static NewDataConfigurationRule NewRule(AddDataOptionRuleRequest request, NewDataOptionRuleBody body) => new()
    {
        Target = Target(request),
        Name = body.Name,
        Abbreviation = body.Abbreviation,
        Description = body.Description,
        Column = body.Column,
        Path = body.Path,
        Method = body.Method,
        OwnerKind = body.Owner?.Kind,
        OwnerId = body.Owner?.Id,
        CustodianUserId = body.CustodianUserId,
        RequiredPermission = body.RequiredPermission,
        Active = body.Active,
        Values = body.Values?.Select(NewValue).ToList(),
    };

    /// <summary>The change to a rule.</summary>
    public static DataConfigurationRuleChange RuleChange(ChangeDataOptionRuleRequest request, DataOptionRuleChangeBody body) => new()
    {
        Key = Key(request),
        Name = body.Name,
        Abbreviation = body.Abbreviation,
        Description = body.Description,
        Column = body.Column,
        Path = body.Path,
        Method = body.Method,
        OwnerKind = body.Owner?.Kind,
        OwnerId = body.Owner?.Id,
        CustodianUserId = body.CustodianUserId,
        RequiredPermission = body.RequiredPermission,
        Clear = body.Clear,
    };

    /// <summary>The values to add.</summary>
    public static IReadOnlyList<NewDataConfigurationRuleValue> NewValues(DataOptionValuesBody body) => [.. (body.Values ?? []).Select(NewValue)];

    /// <summary>The change to a value.</summary>
    public static DataConfigurationRuleValueChange ValueChange(ChangeDataOptionValueRequest request, DataOptionValueChangeBody body) => new()
    {
        Key = Key(request),
        ValueId = request.ValueId,
        Kind = body.Kind,
        Value = body.Value,
        Parameter = body.Parameter,
        Name = body.Name,
        Abbreviation = body.Abbreviation,
        Description = body.Description,
        Ordinal = body.Ordinal,
        Clear = body.Clear,
    };

    /// <summary>The payload of each rule, with the names of its owner, custodian and changer, read once for the whole page.</summary>
    public static async Task<IGenericResult<IReadOnlyList<DataOptionRulePayload>>> Payloads(
        IReadOnlyList<DataConfigurationRule> rules, IDataConfigurationRuleNames names, CancellationToken cancellationToken)
    {
        var userIds = rules.Select(rule => rule.CustodianUserId).Concat(rules.Where(rule => rule.ChangedByUserId is not null).Select(rule => rule.ChangedByUserId!.Value)).Distinct().ToList();
        var users = await names.Users(userIds, cancellationToken).ConfigureAwait(false);
        if (!users.IsSuccess || users.Value is null)
            return users.ToNewResult<IReadOnlyList<DataOptionRulePayload>>();

        var owners = new Dictionary<string, IReadOnlyDictionary<Guid, string>>(StringComparer.Ordinal);
        foreach (var kind in rules.Select(rule => rule.OwnerKind).Distinct(StringComparer.Ordinal))
        {
            var named = await names.Owners(kind, rules.Where(rule => string.Equals(rule.OwnerKind, kind, StringComparison.Ordinal)).Select(rule => rule.OwnerId), cancellationToken).ConfigureAwait(false);
            if (!named.IsSuccess || named.Value is null)
                return named.ToNewResult<IReadOnlyList<DataOptionRulePayload>>();

            owners[kind] = named.Value;
        }

        return GenericResult<IReadOnlyList<DataOptionRulePayload>>.Success([.. rules.Select(rule => Payload(rule, users.Value, owners[rule.OwnerKind]))]);
    }

    /// <summary>The payload of one rule.</summary>
    public static async Task<IGenericResult<DataOptionRulePayload>> Payload(DataConfigurationRule rule, IDataConfigurationRuleNames names, CancellationToken cancellationToken)
    {
        var payloads = await Payloads([rule], names, cancellationToken).ConfigureAwait(false);
        return payloads.IsSuccess && payloads.Value is { Count: 1 } one
            ? GenericResult<DataOptionRulePayload>.Success(one[0])
            : payloads.ToNewResult<DataOptionRulePayload>();
    }

    private static DataOptionRulePayload Payload(DataConfigurationRule rule, IReadOnlyDictionary<Guid, string> users, IReadOnlyDictionary<Guid, string> owners) => new()
    {
        Id = rule.Id,
        Name = rule.Name,
        Abbreviation = rule.Abbreviation,
        Description = rule.Description,
        DataStore = rule.Target.DataStore,
        DataPath = rule.Target.DataPath,
        Container = rule.Target.Container,
        Column = rule.Column,
        Path = [.. rule.Path],
        Method = rule.Method,
        Values = [.. rule.Values.Select(value => new DataOptionValuePayload
        {
            Id = value.Id,
            Name = value.Name,
            Abbreviation = value.Abbreviation,
            Kind = value.Kind,
            Value = value.Value,
            Parameter = value.Parameter,
            Description = value.Description,
            Ordinal = value.Ordinal,
        })],
        Owner = new DataOptionOwnerPayload { Kind = rule.OwnerKind, Id = rule.OwnerId, Name = owners[rule.OwnerId] },
        CustodianUserId = rule.CustodianUserId,
        CustodianName = users[rule.CustodianUserId],
        CustodianState = rule.CustodianState,
        RequiredPermission = rule.RequiredPermission,
        Active = rule.Active,
        Removed = rule.Removed,
        ChangedAt = rule.ChangedAt,
        ChangedByUserId = rule.ChangedByUserId,
        ChangedBy = rule.ChangedByUserId is { } changer ? users[changer] : null,
        CanChange = rule.CanChange,
    };

    /// <summary>The payload of one recorded change: the kind and, where it changes one member, that member with its old and new text, each named through the catalog.</summary>
    public static DataOptionRuleChangePayload Change(DataConfigurationRuleHistoryEntry entry) => new()
    {
        At = entry.At,
        ByUserId = entry.ByUserId,
        ByName = entry.ByName,
        RuleId = entry.RuleId,
        RuleName = entry.RuleName,
        Kind = entry.Kind.Name,
        KindDisplayName = entry.Kind.DisplayName,
        Changes = entry.Kind is { MemberOf: { } owner, Member: { } member } && DataConfigurationRuleCatalog.Member(owner, member) is { } known
            ? [new DataOptionMemberChangePayload { Member = known.Name, MemberDisplayName = known.DisplayName, Old = entry.OldValue, New = entry.NewValue }]
            : [],
    };

    private static NewDataConfigurationRuleValue NewValue(DataOptionValueBody value) => new()
    {
        Kind = value.Kind,
        Value = value.Value,
        Parameter = value.Parameter,
        Name = value.Name,
        Abbreviation = value.Abbreviation,
        Description = value.Description,
        Ordinal = value.Ordinal,
    };
}
