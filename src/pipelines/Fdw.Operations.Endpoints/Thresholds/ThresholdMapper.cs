using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Fdw.Operations.Abstractions.Thresholds;
using Fdw.Operations.Configuration;
using Fdw.Results;

namespace Fdw.Operations.Endpoints.Thresholds;

/// <summary>
/// Maps a threshold between the payload of the routes and the configuration the provider stores (FDW-3948). Criteria travel in the filter form the data routes take and are
/// stored as filter group and condition rows; the data type of a condition is the type of the value the threshold type reports for its field. Nothing is assumed: a payload
/// that does not state what a stored threshold needs is refused, naming it, and so is stored data that is not what it should be.
/// </summary>
public static class ThresholdMapper
{
    private const string AllOperator = "All";
    private const string AnyOperator = "Any";

    /// <summary>Builds the configuration to store from a payload.</summary>
    /// <param name="payload">The payload.</param>
    /// <param name="id">The threshold's identifier: new for a create, the stored one for a replace.</param>
    /// <returns>The configuration; a refusal naming what the payload does not state.</returns>
    public static IGenericResult<ThresholdImplementationConfiguration> ToConfiguration(ThresholdPayload payload, Guid id)
    {
        ArgumentNullException.ThrowIfNull(payload);
        if (payload.IsEnabled is not { } isEnabled)
        {
            return ThresholdRefusal.Of<ThresholdImplementationConfiguration>(payload.Name, "it does not state isEnabled");
        }

        if (payload.FiresOnEntry is not { } firesOnEntry)
        {
            return ThresholdRefusal.Of<ThresholdImplementationConfiguration>(payload.Name, "it does not state firesOnEntry");
        }

        var type = ThresholdTypes.ByName(payload.Type);
        if (ReferenceEquals(type, ThresholdTypes.NotFound))
        {
            return ThresholdRefusal.Of<ThresholdImplementationConfiguration>(payload.Name, $"'{payload.Type}' is not a threshold type");
        }

        var rules = new List<ThresholdRuleConfiguration>();
        for (var i = 0; i < payload.Rules.Count; i++)
        {
            var rule = Rule(payload.Name, type, payload.Rules[i], i + 1);
            if (rule.IsFailure || rule.Value is null)
            {
                return rule.ToNewResult<ThresholdImplementationConfiguration>();
            }

            rules.Add(rule.Value);
        }

        var parameters = new List<ThresholdParameterConfiguration>();
        foreach (var parameter in payload.Parameters)
        {
            var rows = ParameterRows(payload.Name, type, parameter);
            if (rows.IsFailure || rows.Value is null)
            {
                return rows.ToNewResult<ThresholdImplementationConfiguration>();
            }

            parameters.AddRange(rows.Value);
        }

        return GenericResult<ThresholdImplementationConfiguration>.Success(new ThresholdImplementationConfiguration
        {
            Id = id,
            Name = payload.Name,
            Implementation = type.Name,
            Description = payload.Description,
            IsEnabled = isEnabled,
            FiresOnEntry = firesOnEntry,
            SubjectKind = type.SubjectKind,
            Parameters = parameters,
            Rules = rules,
        });
    }

    /// <summary>Builds the payload that answers a read from a stored threshold.</summary>
    /// <param name="stored">The stored threshold.</param>
    /// <returns>The payload; a refusal when a rule holds rows that do not make criteria.</returns>
    public static IGenericResult<ThresholdPayload> ToPayload(IThresholdImplementationConfiguration stored)
    {
        ArgumentNullException.ThrowIfNull(stored);
        var type = ThresholdTypes.ByName(stored.Implementation);
        if (ReferenceEquals(type, ThresholdTypes.NotFound))
        {
            return ThresholdRefusal.Of<ThresholdPayload>(stored.Name, $"it is stored with the type '{stored.Implementation}', which is not a threshold type");
        }

        var parameters = new List<ThresholdParameterPayload>();
        foreach (var group in stored.Parameters.GroupBy(parameter => parameter.Name, StringComparer.Ordinal).OrderBy(group => group.Key, StringComparer.Ordinal))
        {
            var declared = type.Parameters.FirstOrDefault(candidate => string.Equals(candidate.Name, group.Key, StringComparison.Ordinal));
            if (declared is null)
            {
                return ThresholdRefusal.Of<ThresholdPayload>(stored.Name, $"it stores the parameter '{group.Key}', which the type '{type.Name}' does not declare");
            }

            parameters.Add(ParameterPayload(declared, group));
        }

        var rules = new List<ThresholdRulePayload>();
        foreach (var rule in stored.Rules.OrderBy(rule => rule.Ordinal))
        {
            var read = RuleOf(stored.Name, rule);
            if (read.IsFailure || read.Value is null)
            {
                return read.ToNewResult<ThresholdPayload>();
            }

            rules.Add(read.Value);
        }

        return GenericResult<ThresholdPayload>.Success(new ThresholdPayload
        {
            Name = stored.Name,
            Description = stored.Description,
            Type = stored.Implementation,
            IsEnabled = stored.IsEnabled,
            FiresOnEntry = stored.FiresOnEntry,
            Parameters = parameters,
            Rules = rules,
        });
    }

    /// <summary>Builds the summary a list shows from a stored threshold.</summary>
    /// <param name="item">The stored threshold.</param>
    /// <returns>The summary.</returns>
    public static ThresholdSummaryResponse ToSummary(IThresholdImplementationConfiguration item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return new ThresholdSummaryResponse
        {
            Id = item.Id,
            Name = item.Name,
            Description = item.Description,
            Type = item.Implementation,
            SubjectKind = item.SubjectKind,
            IsEnabled = item.IsEnabled,
            FiresOnEntry = item.FiresOnEntry,
            RuleCount = item.Rules.Count,
        };
    }

    private static IGenericResult<List<ThresholdParameterConfiguration>> ParameterRows(string threshold, IThreshold type, ThresholdParameterPayload parameter)
    {
        var declared = type.Parameters.FirstOrDefault(candidate => string.Equals(candidate.Name, parameter.Name, StringComparison.Ordinal));
        if (declared is null)
        {
            return ThresholdRefusal.Of<List<ThresholdParameterConfiguration>>(threshold, $"the type '{type.Name}' declares no parameter '{parameter.Name}'");
        }

        var values = declared.IsList
            ? parameter.Value.ValueKind == JsonValueKind.Array ? parameter.Value.EnumerateArray().ToList() : null
            : parameter.Value.ValueKind == JsonValueKind.Array ? null : new List<JsonElement> { parameter.Value };
        if (values is null || values.Count == 0)
        {
            return ThresholdRefusal.Of<List<ThresholdParameterConfiguration>>(threshold, declared.IsList
                ? $"the parameter '{declared.Name}' takes a list and states none"
                : $"the parameter '{declared.Name}' takes one value");
        }

        var dataType = DataTypeOf(declared.ValueType);
        if (dataType is null)
        {
            return ThresholdRefusal.Of<ThresholdParameterConfiguration>(threshold, $"the parameter '{declared.Name}' is a {declared.ValueType.Name}, which a stored parameter cannot hold").ToNewResult<List<ThresholdParameterConfiguration>>();
        }

        var rows = new List<ThresholdParameterConfiguration>();
        foreach (var value in values)
        {
            var text = TextOf(value);
            if (text is null)
            {
                return ThresholdRefusal.Of<List<ThresholdParameterConfiguration>>(threshold, $"the parameter '{declared.Name}' has a value that is not a text, number or true/false");
            }

            rows.Add(new ThresholdParameterConfiguration
            {
                Id = Guid.CreateVersion7(),
                Name = declared.Name,
                Value = text,
                DataType = dataType,
                Ordinal = rows.Count + 1,
            });
        }

        return GenericResult<List<ThresholdParameterConfiguration>>.Success(rows);
    }

    // A parameter the type declares as a list is an array whatever its length; any other is the one value it has.
    private static ThresholdParameterPayload ParameterPayload(ThresholdParameterDeclaration declared, IGrouping<string, ThresholdParameterConfiguration> rows)
    {
        var values = rows.OrderBy(row => row.Ordinal).Select(row => ScalarNode(row.DataType, row.Value)).ToList();
        return new ThresholdParameterPayload
        {
            Name = declared.Name,
            Value = JsonSerializer.SerializeToElement<JsonNode?>(declared.IsList ? new JsonArray([.. values]) : values[0]),
        };
    }

    // A stored value as the JSON scalar of its data type; text when it does not read as that type, so the stored value is shown as it is.
    private static JsonValue? ScalarNode(string dataType, string? text) => text is null ? null : dataType switch
    {
        "Int64" when long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var whole) => JsonValue.Create(whole),
        "Double" when double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) => JsonValue.Create(number),
        "Boolean" when bool.TryParse(text, out var flag) => JsonValue.Create(flag),
        _ => JsonValue.Create(text),
    };

    private static IGenericResult<ThresholdRuleConfiguration> Rule(string threshold, IThreshold type, ThresholdRulePayload payload, int ordinal)
    {
        if (payload.Criteria.ValueKind != JsonValueKind.Object)
        {
            return ThresholdRefusal.Of<ThresholdRuleConfiguration>(threshold, $"rule {ordinal} does not state criteria as a filter object");
        }

        var groups = new List<ThresholdFilterGroupConfiguration>();
        var root = Group(threshold, type, payload.Criteria, null, 1, ordinal, groups);
        if (root.IsFailure)
        {
            return root.ToNewResult<ThresholdRuleConfiguration>();
        }

        return GenericResult<ThresholdRuleConfiguration>.Success(new ThresholdRuleConfiguration
        {
            Id = Guid.CreateVersion7(),
            Ordinal = ordinal,
            Escalation = payload.Escalation,
            EscalationPolicy = payload.EscalationPolicy,
            FilterGroups = groups,
            Notifications = [.. payload.NotificationRules.Select((name, index) => new ThresholdRuleNotificationConfiguration
            {
                Id = Guid.CreateVersion7(),
                NotificationRuleName = name,
                Ordinal = index + 1,
            })],
        });
    }

    // A node that is a condition on its own is the root: it is wrapped in an All group, since the stored criteria have one root group.
    private static IGenericResult Group(string threshold, IThreshold type, JsonElement node, Guid? parent, int place, int rule, List<ThresholdFilterGroupConfiguration> groups)
    {
        var isGroup = node.TryGetProperty("nodes", out var children) && children.ValueKind == JsonValueKind.Array;
        var group = new ThresholdFilterGroupConfiguration
        {
            Id = Guid.CreateVersion7(),
            ParentGroupId = parent,
            Operator = isGroup ? OperatorOf(node) : AllOperator,
            Ordinal = place,
        };
        if (group.Operator.Length == 0)
        {
            return ThresholdRefusal.Of<bool>(threshold, $"rule {rule} has a group whose logic is neither And nor Or");
        }

        groups.Add(group);
        foreach (var item in isGroup ? children.EnumerateArray().ToList() : new List<JsonElement> { node })
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                return ThresholdRefusal.Of<bool>(threshold, $"rule {rule} has a criteria node that is not an object");
            }

            if (isGroup && item.TryGetProperty("nodes", out _))
            {
                var nested = Group(threshold, type, item, group.Id, groups.Count(candidate => candidate.ParentGroupId == group.Id) + 1, rule, groups);
                if (nested.IsFailure)
                {
                    return nested;
                }

                continue;
            }

            var condition = Condition(threshold, type, item, group.Conditions.Count + 1, rule);
            if (condition.IsFailure || condition.Value is null)
            {
                return condition;
            }

            group.Conditions.Add(condition.Value);
        }

        return GenericResult.Success();
    }

    private static string OperatorOf(JsonElement group)
    {
        var logic = group.TryGetProperty("logic", out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
        return string.Equals(logic, "And", StringComparison.OrdinalIgnoreCase) ? AllOperator
            : string.Equals(logic, "Or", StringComparison.OrdinalIgnoreCase) ? AnyOperator
            : string.Empty;
    }

    private static IGenericResult<ThresholdFilterConditionConfiguration> Condition(string threshold, IThreshold type, JsonElement node, int place, int rule)
    {
        var field = node.TryGetProperty("field", out var fieldName) && fieldName.ValueKind == JsonValueKind.String ? fieldName.GetString() : null;
        if (string.IsNullOrWhiteSpace(field))
        {
            return ThresholdRefusal.Of<ThresholdFilterConditionConfiguration>(threshold, $"rule {rule} has a condition that names no field");
        }

        var reported = type.Fields.FirstOrDefault(candidate => string.Equals(candidate.Name, field, StringComparison.OrdinalIgnoreCase));
        if (reported is null)
        {
            return ThresholdRefusal.Of<ThresholdFilterConditionConfiguration>(threshold, $"rule {rule} names '{field}', which the type '{type.Name}' does not report");
        }

        var dataType = DataTypeOf(reported.ValueType);
        if (dataType is null)
        {
            return ThresholdRefusal.Of<ThresholdFilterConditionConfiguration>(threshold, $"rule {rule}: the value '{reported.Name}' is a {reported.ValueType.Name}, which a stored condition cannot hold");
        }

        var comparison = node.TryGetProperty("operator", out var operatorName) && operatorName.ValueKind == JsonValueKind.String ? operatorName.GetString() : null;
        if (string.IsNullOrWhiteSpace(comparison))
        {
            return ThresholdRefusal.Of<ThresholdFilterConditionConfiguration>(threshold, $"rule {rule} has a condition on '{field}' that names no operator");
        }

        if (!reported.Operators.Contains(comparison, StringComparer.Ordinal))
        {
            return ThresholdRefusal.Of<ThresholdFilterConditionConfiguration>(threshold, $"rule {rule} compares '{field}' with '{comparison}', which a {reported.ValueType.Name} value does not take; it takes {string.Join(", ", reported.Operators)}");
        }

        return GenericResult<ThresholdFilterConditionConfiguration>.Success(new ThresholdFilterConditionConfiguration
        {
            Id = Guid.CreateVersion7(),
            FieldName = reported.Name,
            Operator = comparison,
            Value = node.TryGetProperty("value", out var scalar) ? TextOf(scalar) : null,
            DataType = dataType,
            Ordinal = place,
        });
    }

    // The data type a stored condition or parameter states for a value of a .NET type: the six the reader accepts. Whole numbers are stored as Int64 and fractions as Double; any other type has none.
    private static string? DataTypeOf(Type type) => Type.GetTypeCode(type) switch
    {
        TypeCode.Byte or TypeCode.SByte or TypeCode.Int16 or TypeCode.UInt16 or TypeCode.Int32 or TypeCode.UInt32 or TypeCode.Int64 => "Int64",
        TypeCode.Single or TypeCode.Double or TypeCode.Decimal => "Double",
        TypeCode.Boolean => "Boolean",
        TypeCode.String => "String",
        _ when type == typeof(TimeSpan) => "TimeSpan",
        _ when type == typeof(DateTimeOffset) => "DateTimeOffset",
        _ => null,
    };

    // A scalar as the invariant text the store keeps; anything that is not a scalar has no text, and the reader refuses a condition whose value is missing.
    private static string? TextOf(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.String => value.GetString(),
        JsonValueKind.Number => value.GetRawText(),
        JsonValueKind.True => bool.TrueString,
        JsonValueKind.False => bool.FalseString,
        _ => null,
    };

    private static IGenericResult<ThresholdRulePayload> RuleOf(string threshold, ThresholdRuleConfiguration rule)
    {
        var roots = rule.FilterGroups.Where(group => group.ParentGroupId is null).ToList();
        if (roots.Count != 1)
        {
            return ThresholdRefusal.Of<ThresholdRulePayload>(threshold, $"rule {rule.Ordinal} has {roots.Count} root filter groups; criteria need exactly one");
        }

        return GenericResult<ThresholdRulePayload>.Success(new ThresholdRulePayload
        {
            Criteria = JsonSerializer.SerializeToElement(GroupNode(roots[0], rule)),
            Escalation = rule.Escalation,
            EscalationPolicy = rule.EscalationPolicy,
            NotificationRules = [.. rule.Notifications.OrderBy(notification => notification.Ordinal).Select(notification => notification.NotificationRuleName)],
        });
    }

    private static JsonObject GroupNode(ThresholdFilterGroupConfiguration group, ThresholdRuleConfiguration rule)
    {
        var nodes = new JsonArray();
        foreach (var condition in group.Conditions.OrderBy(condition => condition.Ordinal))
        {
            nodes.Add(ConditionNode(condition));
        }

        foreach (var child in rule.FilterGroups.Where(candidate => candidate.ParentGroupId == group.Id).OrderBy(candidate => candidate.Ordinal))
        {
            nodes.Add(GroupNode(child, rule));
        }

        return new JsonObject
        {
            ["logic"] = string.Equals(group.Operator, AnyOperator, StringComparison.Ordinal) ? "Or" : "And",
            ["nodes"] = nodes,
        };
    }

    private static JsonObject ConditionNode(ThresholdFilterConditionConfiguration condition)
    {
        var node = new JsonObject { ["field"] = condition.FieldName, ["operator"] = condition.Operator };
        if (condition.Value is not null)
        {
            node["value"] = ScalarNode(condition.DataType, condition.Value);
        }

        return node;
    }
}
