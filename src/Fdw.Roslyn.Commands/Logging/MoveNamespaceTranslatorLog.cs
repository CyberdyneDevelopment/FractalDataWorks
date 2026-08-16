using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Roslyn.Commands.Logging;

/// <summary>
/// MessageLogging for <see cref="Refactoring.Translators.MoveNamespaceTranslator"/>.
/// </summary>
[MessageLoggingTypeCode("RCMD")]
public static partial class MoveNamespaceTranslatorLog
{
    /// <summary>Trace: namespace move starting.</summary>
    [MessageLogging(EventId = 11132, Level = LogLevel.Trace,
        Message = "MoveNamespaceTranslator moving '{oldNamespace}' to '{newNamespace}' (dryRun={dryRun})")]
    public static partial IGenericMessage Moving(ILogger logger, string oldNamespace, string newNamespace, bool dryRun);

    /// <summary>Error: the command argument was null.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.CommandCannotBeNull</c> (21000).</remarks>
    [MessageLogging(EventId = 21000, Level = LogLevel.Error,
        Message = "MoveNamespaceTranslator: command was null")]
    public static partial IGenericMessage CommandCannotBeNull(ILogger logger);

    /// <summary>Error: OldNamespace or NewNamespace was not supplied.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.NamespaceRequired</c> (21007).</remarks>
    [MessageLogging(EventId = 21007, Level = LogLevel.Error,
        Message = "MoveNamespaceTranslator: OldNamespace and NewNamespace are both required")]
    public static partial IGenericMessage NamespaceRequired(ILogger logger);

    /// <summary>Error: OldNamespace and NewNamespace are identical.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.TargetSameAsCurrent</c> (41001).</remarks>
    [MessageLogging(EventId = 41001, Level = LogLevel.Error,
        Message = "MoveNamespaceTranslator: namespace '{namespaceName}' is already the target")]
    public static partial IGenericMessage TargetSameAsCurrent(ILogger logger, string namespaceName);

    /// <summary>Error: the loaded solution has no test projects, so the rewrite would be incomplete by construction.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.TestProjectsNotLoaded</c> (31021).</remarks>
    [MessageLogging(EventId = 31021, Level = LogLevel.Error,
        Message = "MoveNamespaceTranslator: no test projects are loaded in the solution, refusing solution-wide rewrite")]
    public static partial IGenericMessage TestProjectsNotLoaded(ILogger logger);

    /// <summary>Error: no types matched the old namespace.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.NoTypesMatchedSelector</c> (31023).</remarks>
    [MessageLogging(EventId = 31023, Level = LogLevel.Error,
        Message = "MoveNamespaceTranslator: no types matched namespace '{oldNamespace}'")]
    public static partial IGenericMessage NoTypesMatchedSelector(ILogger logger, string oldNamespace);

    /// <summary>Error: the change could not be verified (test projects unavailable) and AcceptUnverified was not set.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.ChangeCannotBeVerified</c> (31031).</remarks>
    [MessageLogging(EventId = 31031, Level = LogLevel.Error,
        Message = "MoveNamespaceTranslator: change to '{newNamespace}' could not be verified for {unverifiableCount} project(s)")]
    public static partial IGenericMessage ChangeCannotBeVerified(ILogger logger, string newNamespace, int unverifiableCount);

    /// <summary>Error: the probe found the rewrite would not compile.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.ChangeWouldNotCompile</c> (31030).</remarks>
    [MessageLogging(EventId = 31030, Level = LogLevel.Error,
        Message = "MoveNamespaceTranslator: change to '{newNamespace}' would not compile — {collisionCount} collision(s), {unresolvedCount} unresolved reference(s)")]
    public static partial IGenericMessage ChangeWouldNotCompile(ILogger logger, string newNamespace, int collisionCount, int unresolvedCount);

    /// <summary>Information: the namespace move completed.</summary>
    [MessageLogging(EventId = 11133, Level = LogLevel.Information,
        Message = "MoveNamespaceTranslator moved '{oldNamespace}' to '{newNamespace}': {referenceCount} reference(s) across {fileCount} file(s) (dryRun={dryRun})")]
    public static partial IGenericMessage Moved(ILogger logger, string oldNamespace, string newNamespace, int referenceCount, int fileCount, bool dryRun);
}
