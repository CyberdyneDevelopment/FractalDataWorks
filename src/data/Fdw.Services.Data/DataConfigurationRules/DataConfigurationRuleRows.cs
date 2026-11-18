using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Fdw.Results;
using Fdw.Services.Data.Abstractions.DataConfigurationRules;
using Fdw.Services.Data.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.DataConfigurationRules;

/// <summary>Reads stored rows into rules and values, and a rule's path and method to and from the one text the table stores (FDW-6345).</summary>
internal static class DataConfigurationRuleRows
{
    /// <summary>The root every stored path starts with, as the option layer names it.</summary>
    internal const string PathSeparator = ":";

    /// <summary>The character that ends the path and starts the method.</summary>
    internal const char MethodSeparator = '.';

    /// <summary>Joins the path's names and the method into the text the table stores, such as <c>DataConfigurationRules:Comparison:In.To</c>.</summary>
    internal static string Joined(IReadOnlyList<string> path, string method) =>
        string.Join(PathSeparator, path) + MethodSeparator + method;

    /// <summary>Reads a stored value row.</summary>
    internal static IGenericResult<DataConfigurationRuleValue> ToValue(IReadOnlyDictionary<string, object?> row, string container, ILogger logger)
    {
        if (!ReadGuid(row, "Id", out var id))
            return Unreadable<DataConfigurationRuleValue>("Id", container, logger);
        if (!ReadText(row, "Kind", out var kind) || kind is null)
            return Unreadable<DataConfigurationRuleValue>("Kind", container, logger);
        if (!ReadNumber(row, "Ordinal", out var ordinal))
            return Unreadable<DataConfigurationRuleValue>("Ordinal", container, logger);

        return GenericResult<DataConfigurationRuleValue>.Success(new DataConfigurationRuleValue
        {
            Id = id,
            Kind = kind,
            Value = row.TryGetValue("Value", out var value) ? value as string : null,
            Parameter = row.TryGetValue("Parameter", out var parameter) ? parameter as string : null,
            Name = row.TryGetValue("Name", out var name) ? name as string : null,
            Abbreviation = row.TryGetValue("Abbreviation", out var abbreviation) ? abbreviation as string : null,
            Description = row.TryGetValue("Description", out var description) ? description as string : null,
            Ordinal = ordinal,
        });
    }

    /// <summary>Reads a stored rule row with the values it was given.</summary>
    internal static IGenericResult<DataConfigurationRule> ToRule(
        IReadOnlyDictionary<string, object?> row, IReadOnlyList<DataConfigurationRuleValue> values, string container, ILogger logger,
        Func<Guid, string?, bool> mayChange, Func<Guid, Guid?> changedBy, Func<Guid, string> custodianState)
    {
        if (!ReadGuid(row, "Id", out var id))
            return Unreadable<DataConfigurationRule>("Id", container, logger);

        string[] required = ["Name", "Abbreviation", "DataStore", "DataPath", "Container", "Column", "Path", "OwnerKind"];
        var texts = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var field in required)
        {
            if (!ReadText(row, field, out var text) || text is null)
                return Unreadable<DataConfigurationRule>(field, container, logger);
            texts[field] = text;
        }

        if (!ReadGuid(row, "OwnerId", out var ownerId))
            return Unreadable<DataConfigurationRule>("OwnerId", container, logger);
        if (!ReadGuid(row, "CustodianUserId", out var custodian))
            return Unreadable<DataConfigurationRule>("CustodianUserId", container, logger);
        if (!ReadFlag(row, "Active", out var active))
            return Unreadable<DataConfigurationRule>("Active", container, logger);
        if (!ReadFlag(row, "IsDeleted", out var removed))
            return Unreadable<DataConfigurationRule>("IsDeleted", container, logger);
        if (!ReadMoment(row, "ModifyDate", out var changedAt))
            return Unreadable<DataConfigurationRule>("ModifyDate", container, logger);

        var requiredPermission = row.TryGetValue("RequiredPermission", out var permission) ? permission as string : null;
        var stored = texts["Path"];
        var split = stored.LastIndexOf(MethodSeparator);
        if (split <= 0 || split == stored.Length - 1)
            return Unreadable<DataConfigurationRule>("Path", container, logger);

        return GenericResult<DataConfigurationRule>.Success(new DataConfigurationRule
        {
            Id = id,
            Name = texts["Name"],
            Abbreviation = texts["Abbreviation"],
            Description = row.TryGetValue("Description", out var description) ? description as string : null,
            Target = new DataConfigurationRuleContainer { DataStore = texts["DataStore"], DataPath = texts["DataPath"], Container = texts["Container"] },
            Column = texts["Column"],
            Path = stored[..split].Split(PathSeparator[0]),
            Method = stored[(split + 1)..],
            Values = [.. values.OrderBy(value => value.Ordinal)],
            OwnerKind = texts["OwnerKind"],
            OwnerId = ownerId,
            CustodianUserId = custodian,
            CustodianState = custodianState(custodian),
            RequiredPermission = requiredPermission,
            CanChange = mayChange(custodian, requiredPermission),
            ChangedByUserId = changedBy(id),
            Active = active,
            Removed = removed,
            ChangedAt = changedAt,
        });
    }

    /// <summary>Reads the name of a stored rule row.</summary>
    internal static bool NameOf(IReadOnlyDictionary<string, object?> row, out string name)
    {
        var read = ReadText(row, "Name", out var text);
        name = text ?? string.Empty;
        return read && text is not null;
    }

    /// <summary>Reads the id of a stored rule row.</summary>
    internal static bool IdOf(IReadOnlyDictionary<string, object?> row, out Guid id) => ReadGuid(row, "Id", out id);

    /// <summary>Reads the id of the rule a stored value row belongs to.</summary>
    internal static bool RuleIdOf(IReadOnlyDictionary<string, object?> row, out Guid ruleId) => ReadGuid(row, "DataConfigurationRuleId", out ruleId);

    private static IGenericResult<T> Unreadable<T>(string field, string container, ILogger logger) =>
        GenericResult<T>.Failure(DataOptionRulesLog.StoredRuleUnreadable(logger, field, container));

    private static bool ReadText(IReadOnlyDictionary<string, object?> row, string field, out string? text)
    {
        text = null;
        if (!row.TryGetValue(field, out var value) || value is not string stated || stated.Length == 0)
            return false;

        text = stated;
        return true;
    }

    private static bool ReadGuid(IReadOnlyDictionary<string, object?> row, string field, out Guid id)
    {
        id = default;
        return row.TryGetValue(field, out var value)
            && value switch
            {
                Guid stated => (id = stated) != default,
                string text => System.Guid.TryParse(text, out id),
                _ => false,
            };
    }

    private static bool ReadNumber(IReadOnlyDictionary<string, object?> row, string field, out int number)
    {
        number = 0;
        if (!row.TryGetValue(field, out var value) || value is null)
            return false;

        try
        {
            number = Convert.ToInt32(value, CultureInfo.InvariantCulture);
            return true;
        }
        catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException)
        {
            return false;
        }
    }

    private static bool ReadFlag(IReadOnlyDictionary<string, object?> row, string field, out bool flag)
    {
        flag = false;
        if (!row.TryGetValue(field, out var value) || value is null)
            return false;

        try
        {
            flag = Convert.ToBoolean(value, CultureInfo.InvariantCulture);
            return true;
        }
        catch (Exception ex) when (ex is FormatException or InvalidCastException)
        {
            return false;
        }
    }

    private static bool ReadMoment(IReadOnlyDictionary<string, object?> row, string field, out DateTimeOffset moment)
    {
        moment = default;
        if (!row.TryGetValue(field, out var value))
            return false;

        switch (value)
        {
            case DateTimeOffset offset:
                moment = offset;
                return true;
            case DateTime time:
                moment = new DateTimeOffset(time);
                return true;
            default:
                return false;
        }
    }
}
