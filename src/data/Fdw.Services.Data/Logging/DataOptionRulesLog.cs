using Fdw.Messages;
using Fdw.MessageLogging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Logging;

/// <summary>
/// MessageLogging for the data option rules and values (FDW-6339): one method and code per failure reason, the code is the number.
/// Stage 1 (FDW-6348) declares them so the contract's codes are fixed before stage 2 returns them.
/// </summary>
[MessageLoggingTypeCode("DATA")]
public static partial class DataOptionRulesLog
{
    /// <summary>RuleNameMissing (FDW-6339). Declared by stage 1 so its code is fixed; stage 2 returns it.</summary>
    [MessageLogging(EventId = 21680, Level = LogLevel.Warning, Message = "A data option rule on column '{column}' of container '{container}' was refused: it states no name",
        ClientMessage = "The rule needs a name.")]
    public static partial IGenericMessage RuleNameMissing(ILogger logger, string container, string column);

    /// <summary>RuleAbbreviationMissing (FDW-6339). Declared by stage 1 so its code is fixed; stage 2 returns it.</summary>
    [MessageLogging(EventId = 21681, Level = LogLevel.Warning, Message = "Data option rule '{rule}' was refused: it states no abbreviation",
        ClientMessage = "The rule needs an abbreviation.")]
    public static partial IGenericMessage RuleAbbreviationMissing(ILogger logger, string rule);

    /// <summary>RuleColumnMissing (FDW-6339). Declared by stage 1 so its code is fixed; stage 2 returns it.</summary>
    [MessageLogging(EventId = 21682, Level = LogLevel.Warning, Message = "Data option rule '{rule}' of container '{container}' was refused: it names no column",
        ClientMessage = "The rule must name the column it is about.")]
    public static partial IGenericMessage RuleColumnMissing(ILogger logger, string rule, string container);

    /// <summary>RuleBasisMismatch (FDW-6339). Declared by stage 1 so its code is fixed; stage 2 returns it.</summary>
    [MessageLogging(EventId = 21683, Level = LogLevel.Warning, Message = "Data option rule '{rule}' was refused: it states it is based on '{stated}' but the route is '{route}'",
        ClientMessage = "The rule's data store, path and container must be the route's: it states {stated}, the route is {route}.")]
    public static partial IGenericMessage RuleBasisMismatch(ILogger logger, string rule, string stated, string route);

    /// <summary>PathEmpty (FDW-6339). Declared by stage 1 so its code is fixed; stage 2 returns it.</summary>
    [MessageLogging(EventId = 21684, Level = LogLevel.Warning, Message = "Data option rule '{rule}' was refused: it names no path",
        ClientMessage = "The rule must name a path, from DataConfigurationRules down to a leaf.")]
    public static partial IGenericMessage PathEmpty(ILogger logger, string rule);

    /// <summary>PathSegmentEmpty (FDW-6339). Declared by stage 1 so its code is fixed; stage 2 returns it.</summary>
    [MessageLogging(EventId = 21685, Level = LogLevel.Warning, Message = "Data option path '{path}' was refused: level {levelNumber} is empty",
        ClientMessage = "Level {levelNumber} of the path {path} is empty.")]
    public static partial IGenericMessage PathSegmentEmpty(ILogger logger, string path, int levelNumber);

    /// <summary>PathIncomplete (FDW-6339). Declared by stage 1 so its code is fixed; stage 2 returns it.</summary>
    [MessageLogging(EventId = 21686, Level = LogLevel.Warning, Message = "Data option path '{path}' was refused: it stops at '{stoppedAt}', which has names below it ({below})",
        ClientMessage = "The path {path} stops at {stoppedAt}; choose one of: {below}.")]
    public static partial IGenericMessage PathIncomplete(ILogger logger, string path, string stoppedAt, string below);

    /// <summary>MethodMissing (FDW-6339). Declared by stage 1 so its code is fixed; stage 2 returns it.</summary>
    [MessageLogging(EventId = 21687, Level = LogLevel.Warning, Message = "Data option path '{path}' was refused: it names no method after the period",
        ClientMessage = "The path {path} must end in a period and the method, such as .To.")]
    public static partial IGenericMessage MethodMissing(ILogger logger, string path);

    /// <summary>ValueMissing (FDW-6339). Declared by stage 1 so its code is fixed; stage 2 returns it.</summary>
    [MessageLogging(EventId = 21688, Level = LogLevel.Warning, Message = "Data option rule '{rule}' was refused: it states no value",
        ClientMessage = "The rule needs a value.")]
    public static partial IGenericMessage ValueMissing(ILogger logger, string rule);

    /// <summary>ValueCountNotTaken (FDW-6339). Declared by stage 1 so its code is fixed; stage 2 returns it.</summary>
    [MessageLogging(EventId = 21689, Level = LogLevel.Warning, Message = "Data option rule '{rule}' was refused: its leaf '{path}' takes {takes} value(s) and the rule states {count}",
        ClientMessage = "The rule's leaf {path} takes {takes}; the rule states {count} values.")]
    public static partial IGenericMessage ValueCountNotTaken(ILogger logger, string rule, string path, string takes, int count);

    /// <summary>ValueKindMissing (FDW-6339). Declared by stage 1 so its code is fixed; stage 2 returns it.</summary>
    [MessageLogging(EventId = 21690, Level = LogLevel.Warning, Message = "Data option rule '{rule}' was refused: value {ordinal} states no kind",
        ClientMessage = "Value {ordinal} must say whether it is a literal or a parameter.")]
    public static partial IGenericMessage ValueKindMissing(ILogger logger, string rule, int ordinal);

    /// <summary>ValueLiteralAndParameter (FDW-6339). Declared by stage 1 so its code is fixed; stage 2 returns it.</summary>
    [MessageLogging(EventId = 21691, Level = LogLevel.Warning, Message = "Data option rule '{rule}' was refused: value {ordinal} states both a literal and a parameter",
        ClientMessage = "Value {ordinal} is a literal or a parameter, never both.")]
    public static partial IGenericMessage ValueLiteralAndParameter(ILogger logger, string rule, int ordinal);

    /// <summary>ParameterNameMissing (FDW-6339). Declared by stage 1 so its code is fixed; stage 2 returns it.</summary>
    [MessageLogging(EventId = 21692, Level = LogLevel.Warning, Message = "Data option rule '{rule}' was refused: value {ordinal} is a parameter with no name",
        ClientMessage = "Value {ordinal} is a parameter and must name it.")]
    public static partial IGenericMessage ParameterNameMissing(ILogger logger, string rule, int ordinal);

    /// <summary>OwnerMissing (FDW-6339). Declared by stage 1 so its code is fixed; stage 2 returns it.</summary>
    [MessageLogging(EventId = 21696, Level = LogLevel.Warning, Message = "Data option rule '{rule}' was refused: it names no owner",
        ClientMessage = "The rule must name the organization or workspace it belongs to.")]
    public static partial IGenericMessage OwnerMissing(ILogger logger, string rule);

    /// <summary>CustodianMissing (FDW-6339). Declared by stage 1 so its code is fixed; stage 2 returns it.</summary>
    [MessageLogging(EventId = 21697, Level = LogLevel.Warning, Message = "Data option rule '{rule}' was refused: it names no custodian",
        ClientMessage = "The rule must name its custodian.")]
    public static partial IGenericMessage CustodianMissing(ILogger logger, string rule);

    /// <summary>ActiveMissing (FDW-6339). Declared by stage 1 so its code is fixed; stage 2 returns it.</summary>
    [MessageLogging(EventId = 21698, Level = LogLevel.Warning, Message = "Data option rule '{rule}' was refused: it does not say whether it is switched on",
        ClientMessage = "The rule must say whether it is switched on.")]
    public static partial IGenericMessage ActiveMissing(ILogger logger, string rule);

    /// <summary>RemovedMissing (FDW-6339). Declared by stage 1 so its code is fixed; stage 2 returns it.</summary>
    [MessageLogging(EventId = 21700, Level = LogLevel.Warning, Message = "A list of the data option rules of container '{container}' was refused: it does not say whether to list removed rules or current ones",
        ClientMessage = "Say whether to list removed rules or current ones (removed=true or removed=false).")]
    public static partial IGenericMessage RemovedMissing(ILogger logger, string container);

    /// <summary>FieldTooLong (FDW-6339). Declared by stage 1 so its code is fixed; stage 2 returns it.</summary>
    [MessageLogging(EventId = 21701, Level = LogLevel.Warning, Message = "A data option {field} of {length} characters was refused: it takes at most {maxLength}",
        ClientMessage = "The {field} takes at most {maxLength} characters; it has {length}.")]
    public static partial IGenericMessage FieldTooLong(ILogger logger, string field, int maxLength, int length);

    /// <summary>ParameterNotGiven (FDW-6339). Declared by stage 1 so its code is fixed; stage 2 returns it.</summary>
    [MessageLogging(EventId = 21702, Level = LogLevel.Warning, Message = "Data option rule '{rule}' could not be applied: the run gives no parameter '{parameter}'",
        ClientMessage = "The run must give the parameter {parameter} that rule {rule} names.")]
    public static partial IGenericMessage ParameterNotGiven(ILogger logger, string rule, string parameter);

    /// <summary>ParameterNotInScope (FDW-6339). Declared by stage 1 so its code is fixed; stage 2 returns it.</summary>
    [MessageLogging(EventId = 21703, Level = LogLevel.Warning, Message = "Data option rule '{rule}' could not be applied: it names parameter '{parameter}' and this read takes no parameters",
        ClientMessage = "Rule {rule} names the parameter {parameter}, and this read takes no parameters.")]
    public static partial IGenericMessage ParameterNotInScope(ILogger logger, string rule, string parameter);

    /// <summary>FieldNotClearable (FDW-6339). Declared by stage 1 so its code is fixed; stage 2 returns it.</summary>
    [MessageLogging(EventId = 21704, Level = LogLevel.Warning, Message = "A change was refused: '{field}' cannot be cleared",
        ClientMessage = "{field} cannot be cleared: it is required, or it is not a field of this.")]
    public static partial IGenericMessage FieldNotClearable(ILogger logger, string field);

    /// <summary>OwnerKindMissing (FDW-6339). Declared by stage 1 so its code is fixed; stage 2 returns it.</summary>
    [MessageLogging(EventId = 21705, Level = LogLevel.Warning, Message = "A list of data option owners was refused: it names no kind of owner",
        ClientMessage = "Say which kind of owner to list.")]
    public static partial IGenericMessage OwnerKindMissing(ILogger logger);

    /// <summary>RuleNotFound (FDW-6339). Declared by stage 1 so its code is fixed; stage 2 returns it.</summary>
    [MessageLogging(EventId = 31680, Level = LogLevel.Warning, Message = "No current data option rule '{ruleId}' in container '{container}'",
        ClientMessage = "There is no rule {ruleId} in {container}.")]
    public static partial IGenericMessage RuleNotFound(ILogger logger, string ruleId, string container);

    /// <summary>ValueNotFound (FDW-6339). Declared by stage 1 so its code is fixed; stage 2 returns it.</summary>
    [MessageLogging(EventId = 31681, Level = LogLevel.Warning, Message = "No value '{valueId}' in data option rule '{rule}'",
        ClientMessage = "Rule {rule} has no value {valueId}.")]
    public static partial IGenericMessage ValueNotFound(ILogger logger, string valueId, string rule);

    /// <summary>PathRootNotFound (FDW-6339). Declared by stage 1 so its code is fixed; stage 2 returns it.</summary>
    [MessageLogging(EventId = 31682, Level = LogLevel.Warning, Message = "Data option path root '{name}' was not found (roots: {roots})",
        ClientMessage = "{name} is not a data option root; the roots are: {roots}.")]
    public static partial IGenericMessage PathRootNotFound(ILogger logger, string name, string roots);

    /// <summary>PathSegmentNotFound (FDW-6339). Declared by stage 1 so its code is fixed; stage 2 returns it.</summary>
    [MessageLogging(EventId = 31683, Level = LogLevel.Warning, Message = "Data option path level {levelNumber}: '{name}' was not found (names at that level: {known})",
        ClientMessage = "Level {levelNumber} of the path has no {name}; the names there are: {known}.")]
    public static partial IGenericMessage PathSegmentNotFound(ILogger logger, int levelNumber, string name, string known);

    /// <summary>MethodNotFound (FDW-6339). Declared by stage 1 so its code is fixed; stage 2 returns it.</summary>
    [MessageLogging(EventId = 31684, Level = LogLevel.Warning, Message = "Data option path '{path}' names method '{method}'; its leaf's method is '{leafMethod}'",
        ClientMessage = "The leaf of {path} has no method {method}; its method is {leafMethod}.")]
    public static partial IGenericMessage MethodNotFound(ILogger logger, string path, string method, string leafMethod);

    /// <summary>ValueKindNotFound (FDW-6339). Declared by stage 1 so its code is fixed; stage 2 returns it.</summary>
    [MessageLogging(EventId = 31686, Level = LogLevel.Warning, Message = "Data option value kind '{kind}' was not found (kinds: {known})",
        ClientMessage = "{kind} is not a kind of value; the kinds are: {known}.")]
    public static partial IGenericMessage ValueKindNotFound(ILogger logger, string kind, string known);

    /// <summary>OwnerKindNotFound (FDW-6339). Declared by stage 1 so its code is fixed; stage 2 returns it.</summary>
    [MessageLogging(EventId = 31687, Level = LogLevel.Warning, Message = "Data option owner kind '{kind}' was not found (kinds: {known})",
        ClientMessage = "{kind} is not a kind of owner; the kinds are: {known}.")]
    public static partial IGenericMessage OwnerKindNotFound(ILogger logger, string kind, string known);

    /// <summary>OwnerNotFound (FDW-6339). Declared by stage 1 so its code is fixed; stage 2 returns it.</summary>
    [MessageLogging(EventId = 31688, Level = LogLevel.Warning, Message = "Data option owner {kind} '{ownerId}' was not found",
        ClientMessage = "There is no {kind} {ownerId}.")]
    public static partial IGenericMessage OwnerNotFound(ILogger logger, string kind, string ownerId);

    /// <summary>ColumnNotFound (FDW-6339). Declared by stage 1 so its code is fixed; stage 2 returns it.</summary>
    [MessageLogging(EventId = 31689, Level = LogLevel.Warning, Message = "Container '{container}' declares no column '{column}'",
        ClientMessage = "{container} has no column {column}.")]
    public static partial IGenericMessage ColumnNotFound(ILogger logger, string column, string container);

    /// <summary>CustodianNotFound (FDW-6339). Declared by stage 1 so its code is fixed; stage 2 returns it.</summary>
    [MessageLogging(EventId = 31690, Level = LogLevel.Warning, Message = "Data option custodian '{userId}' was not found among the holders of datastores:write",
        ClientMessage = "{userId} does not hold datastores:write and cannot be a custodian.")]
    public static partial IGenericMessage CustodianNotFound(ILogger logger, string userId);

    /// <summary>RuleNameTaken (FDW-6339). Declared by stage 1 so its code is fixed; stage 2 returns it.</summary>
    [MessageLogging(EventId = 41681, Level = LogLevel.Warning, Message = "Data option rule name '{name}' is already used on column '{column}'",
        ClientMessage = "Column {column} already has a rule named {name}.")]
    public static partial IGenericMessage RuleNameTaken(ILogger logger, string name, string column);

    /// <summary>RuleAbbreviationTaken (FDW-6339). Declared by stage 1 so its code is fixed; stage 2 returns it.</summary>
    [MessageLogging(EventId = 41682, Level = LogLevel.Warning, Message = "Data option rule abbreviation '{abbreviation}' is already used on column '{column}'",
        ClientMessage = "Column {column} already has a rule abbreviated {abbreviation}.")]
    public static partial IGenericMessage RuleAbbreviationTaken(ILogger logger, string abbreviation, string column);

    /// <summary>ValueTaken (FDW-6339). Declared by stage 1 so its code is fixed; stage 2 returns it.</summary>
    [MessageLogging(EventId = 41683, Level = LogLevel.Warning, Message = "Data option rule '{rule}' already holds the value '{value}'",
        ClientMessage = "Rule {rule} already holds {value}.")]
    public static partial IGenericMessage ValueTaken(ILogger logger, string rule, string value);

    /// <summary>RuleRemoved (FDW-6339). Declared by stage 1 so its code is fixed; stage 2 returns it.</summary>
    [MessageLogging(EventId = 41685, Level = LogLevel.Warning, Message = "Data option rule '{rule}' is removed; restore it before changing it",
        ClientMessage = "Rule {rule} is removed; restore it first.")]
    public static partial IGenericMessage RuleRemoved(ILogger logger, string rule);

    /// <summary>RuleNotRemoved (FDW-6339). Declared by stage 1 so its code is fixed; stage 2 returns it.</summary>
    [MessageLogging(EventId = 41686, Level = LogLevel.Warning, Message = "Data option rule '{rule}' is not removed, so it cannot be restored",
        ClientMessage = "Rule {rule} is not removed.")]
    public static partial IGenericMessage RuleNotRemoved(ILogger logger, string rule);

    /// <summary>RuleChangeForbidden (FDW-6339). Declared by stage 1 so its code is fixed; stage 2 returns it.</summary>
    [MessageLogging(EventId = 51680, Level = LogLevel.Warning, Message = "User '{userId}' may not change data option rule '{rule}': not its custodian and not a holder of '{permission}'",
        ClientMessage = "Only the rule's custodian or a holder of {permission} may change rule {rule}.")]
    public static partial IGenericMessage RuleChangeForbidden(ILogger logger, string userId, string rule, string permission);

    /// <summary>PermissionNotHeld (FDW-6339). Declared by stage 1 so its code is fixed; stage 2 returns it.</summary>
    [MessageLogging(EventId = 51681, Level = LogLevel.Warning, Message = "User '{userId}' may not set '{permission}' as a data option rule's permission: the user does not hold it",
        ClientMessage = "You can only require a permission you hold; you do not hold {permission}.")]
    public static partial IGenericMessage PermissionNotHeld(ILogger logger, string userId, string permission);

    /// <summary>OwnerNotVisible (FDW-6339). Declared by stage 1 so its code is fixed; stage 2 returns it.</summary>
    [MessageLogging(EventId = 51682, Level = LogLevel.Warning, Message = "User '{userId}' cannot see the data option owner {ownerKind} '{ownerId}'",
        ClientMessage = "You cannot see {ownerKind} {ownerId}.")]
    public static partial IGenericMessage OwnerNotVisible(ILogger logger, string userId, string ownerKind, string ownerId);

    /// <summary>RuleSettingNotSet (FDW-6339, FDW-6543). One of the four settings that name the rule tables is not set; the message names it.</summary>
    [MessageLogging(EventId = 121680, Level = LogLevel.Error, Message = "The data configuration rule tables are not named: the setting '{setting}' is not set",
        ClientMessage = "Data configuration rules are not set up here: the setting {setting} is not set.")]
    public static partial IGenericMessage RuleSettingNotSet(ILogger logger, string setting);

    /// <summary>OptionsStoreNotTransactional (FDW-6339). Declared by stage 1 so its code is fixed; stage 2 returns it.</summary>
    [MessageLogging(EventId = 121681, Level = LogLevel.Error, Message = "Data option rules cannot be written to data store '{dataStore}': its connection cannot hold the read, close and insert of a versioned save in one transaction",
        ClientMessage = "Data option rules cannot be changed in {dataStore}: it cannot retire the old version and write the new one together.")]
    public static partial IGenericMessage OptionsStoreNotTransactional(ILogger logger, string dataStore);

    /// <summary>Traces a write the rule service made (FDW-6345).</summary>
    [MessageLogging(EventId = 11680, Level = LogLevel.Trace, Message = "Data option rule '{rule}' in container '{container}': {operation}")]
    public static partial IGenericMessage Written(ILogger logger, string operation, string rule, string container);

    /// <summary>ValueOrdinalMissing (FDW-6345): a value that states no place among the rule's values.</summary>
    [MessageLogging(EventId = 21708, Level = LogLevel.Warning, Message = "Data option rule '{rule}' was refused: value {position} states no ordinal",
        ClientMessage = "Value {position} must state its ordinal, from 1.")]
    public static partial IGenericMessage ValueOrdinalMissing(ILogger logger, string rule, int position);

    /// <summary>RuleTablesUnreadable (FDW-6345): the tables of the rules could not be read.</summary>
    [MessageLogging(EventId = 71680, Level = LogLevel.Warning, Message = "The data option rule table '{container}' of data store '{dataStore}' could not be read",
        ClientMessage = "The data option rules of {dataStore} could not be read.")]
    public static partial IGenericMessage RuleTablesUnreadable(ILogger logger, string container, string dataStore);

    /// <summary>RuleTablesRefusedWrite (FDW-6345): the tables of the rules refused a write.</summary>
    [MessageLogging(EventId = 71681, Level = LogLevel.Warning, Message = "The data option rule table '{container}' of data store '{dataStore}' refused the write '{operation}'",
        ClientMessage = "The data option rules of {dataStore} could not be changed.")]
    public static partial IGenericMessage RuleTablesRefusedWrite(ILogger logger, string operation, string container, string dataStore);

    /// <summary>StoredRuleUnreadable (FDW-6345): a stored rule row that does not read as a rule.</summary>
    [MessageLogging(EventId = 91680, Level = LogLevel.Error, Message = "A stored data option rule in container '{container}' cannot be read: field '{field}' is missing or not of its type",
        ClientMessage = "A stored data option rule could not be read.")]
    public static partial IGenericMessage StoredRuleUnreadable(ILogger logger, string field, string container);

    /// <summary>ChangeKindUnknown (FDW-6571): a recorded change of a rule whose kind the catalog does not know, so it cannot be named.</summary>
    [MessageLogging(EventId = 91681, Level = LogLevel.Error, Message = "The administrative audit holds a change '{changeKind}' of data option rule '{ruleId}' that the rule change catalog does not know",
        ClientMessage = "A recorded change of a rule could not be named.")]
    public static partial IGenericMessage ChangeKindUnknown(ILogger logger, string changeKind, string ruleId);

    /// <summary>LimitNotAWholeNumber (FDW-6575): a rule table field declares a maximum length that is not a whole number, so no limit can be stated for it.</summary>
    [MessageLogging(EventId = 91682, Level = LogLevel.Error, Message = "Field '{field}' of container '{container}' declares a maximum length that is not a whole number",
        ClientMessage = "The length limits of the data option rules could not be read.")]
    public static partial IGenericMessage LimitNotAWholeNumber(ILogger logger, string field, string container);

    /// <summary>Traces the registration of the rule service (FDW-6345).</summary>
    [MessageLogging(EventId = 11681, Level = LogLevel.Trace, Message = "Registered the data configuration rules service")]
    public static partial IGenericMessage Registered(ILogger logger);

    /// <summary>CallerNotIdentified (FDW-6346): a rule operation by a caller who is not a signed-in person.</summary>
    [MessageLogging(EventId = 51683, Level = LogLevel.Warning, Message = "The data option rule operation '{operation}' was refused: the caller is not a signed-in person",
        ClientMessage = "Sign in to work with data option rules.")]
    public static partial IGenericMessage CallerNotIdentified(ILogger logger, string operation);

    /// <summary>UserNotFound (FDW-6346): a user a rule names has no account name.</summary>
    [MessageLogging(EventId = 31691, Level = LogLevel.Warning, Message = "Data option user '{userId}' has no account or no name",
        ClientMessage = "There is no named user {userId}.")]
    public static partial IGenericMessage UserNotFound(ILogger logger, string userId);
}
