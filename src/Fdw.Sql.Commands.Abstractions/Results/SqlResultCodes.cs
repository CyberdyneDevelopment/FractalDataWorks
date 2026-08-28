using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Sql.Commands.Abstractions.Results;

/// <summary>Result codes used across SQL command translators.</summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public static class SqlResultCodes
{
    public static readonly IResultCode CommandCannotBeNull        = new SqlResultCode(20000, nameof(CommandCannotBeNull),        ResultSeverities.ByName("Error"), "Command cannot be null.");
    public static readonly IResultCode CommandExecutionFailed     = new SqlResultCode(70001, nameof(CommandExecutionFailed),     ResultSeverities.ByName("Error"), "Command execution failed.");
    public static readonly IResultCode CommandExecutionCancelled  = new SqlResultCode(10010, nameof(CommandExecutionCancelled),  ResultSeverities.ByName("Error"), "Command execution was cancelled.");
    public static readonly IResultCode TranslatorNotFound         = new SqlResultCode(60002, nameof(TranslatorNotFound),         ResultSeverities.ByName("Error"), "Translator not registered for command type.");
    public static readonly IResultCode NoSqlProjectLoaded         = new SqlResultCode(40000, nameof(NoSqlProjectLoaded),         ResultSeverities.ByName("Error"), "No SQL project loaded. Call load_sqlproject first.");
    public static readonly IResultCode NotYetImplemented          = new SqlResultCode(90005, nameof(NotYetImplemented),          ResultSeverities.ByName("Error"), "Translator is registered but not yet implemented.");
    public static readonly IResultCode ProjectNameRequired        = new SqlResultCode(21000, nameof(ProjectNameRequired),        ResultSeverities.ByName("Error"), "ProjectName is required.");
    public static readonly IResultCode FilePathRequired           = new SqlResultCode(21001, nameof(FilePathRequired),           ResultSeverities.ByName("Error"), "FilePath is required.");
    public static readonly IResultCode ObjectNameRequired         = new SqlResultCode(21002, nameof(ObjectNameRequired),         ResultSeverities.ByName("Error"), "ObjectName is required.");
    public static readonly IResultCode PatternRequired            = new SqlResultCode(21003, nameof(PatternRequired),            ResultSeverities.ByName("Error"), "Pattern is required.");
    public static readonly IResultCode SnapshotNameRequired       = new SqlResultCode(21004, nameof(SnapshotNameRequired),       ResultSeverities.ByName("Error"), "Snapshot name is required.");
    public static readonly IResultCode SnapshotIdRequired         = new SqlResultCode(21005, nameof(SnapshotIdRequired),         ResultSeverities.ByName("Error"), "Snapshot ID is required.");
    public static readonly IResultCode ScriptNotFound             = new SqlResultCode(30000, nameof(ScriptNotFound),             ResultSeverities.ByName("Error"), "Script not found in workspace.");
    public static readonly IResultCode ObjectNotFound             = new SqlResultCode(31000, nameof(ObjectNotFound),             ResultSeverities.ByName("Error"), "Object not found.");
}
