using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Roslyn.Commands.Refactoring.Logging;

/// <summary>
/// Structured logging for the move commands and the verification probe.
/// </summary>
/// <remarks>
/// Every failure logged here carries the SAME number as the RoslynResultCodes entry it returns, so the
/// log line and the returned IGenericResult share a Code and trace to one condition without a second
/// lookup — the convention AegisLog established. A refusal logged as MOVE-31031 IS
/// RoslynResultCodes.ChangeCannotBeVerified; there is no separate logging number to reconcile.
///
/// The purely informational methods have no result code — nothing is returned for them — so they draw
/// from category 1 (non-error outcomes) per RESULTCODE-CATALOG.md: 5 digits, Category = Id / 10000, with
/// the 1..9999 band explicitly invalid.
///
/// Every one of these exists because its absence cost real debugging time. A move that reported success
/// having silently skipped half its files, a probe that refused without saying which project it could not
/// bind, a reference closure computed and never written — each looked identical to working, and the only
/// way to tell was to read the source afterwards. Trace covers what was selected and why, Information the
/// outcome, Warning the silent-skip cases, and Error the refusals.
/// </remarks>
[MessageLoggingTypeCode("MOVE")]
public static partial class MoveCommandLog
{
    /// <summary>Logs the start of a move, with its selector and mode.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="command">The command being run.</param>
    /// <param name="selector">What the caller asked to move.</param>
    /// <param name="dryRun">Whether this is a preview.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 12400, Level = LogLevel.Trace,
        Message = "{command} starting: selector={selector} dryRun={dryRun}")]
    public static partial IGenericMessage MoveStarting(ILogger logger, string command, string selector, bool dryRun);

    /// <summary>Logs what the selector actually matched.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="command">The command being run.</param>
    /// <param name="matched">How many documents matched.</param>
    /// <param name="skipped">How many were excluded by SkipTypes or the generated-file rule.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 12401, Level = LogLevel.Debug,
        Message = "{command} selected {matched} document(s), skipped {skipped}")]
    public static partial IGenericMessage SelectionResolved(ILogger logger, string command, int matched, int skipped);

    /// <summary>Logs a document the selector considered and rejected.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="filePath">The document that was not selected.</param>
    /// <param name="reason">Which rule rejected it.</param>
    /// <returns>The structured message.</returns>
    /// <remarks>
    /// Trace, because on a large solution every document that is not being moved passes through here.
    /// It exists so "why did it only move three files" has an answer that does not require re-reading
    /// the selection code and guessing which clause fired.
    /// </remarks>
    [MessageLogging(EventId = 12406, Level = LogLevel.Trace,
        Message = "Not selected ({reason}): {filePath}")]
    public static partial IGenericMessage SelectionRejected(ILogger logger, string reason, string? filePath);

    /// <summary>Logs documents that sit under the requested namespace but were excluded from the move.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="selector">The namespace the caller asked to move.</param>
    /// <param name="stranded">How many documents declare a namespace beneath it.</param>
    /// <returns>The structured message.</returns>
    /// <remarks>
    /// Warning, and the single most useful line here. A move of <c>Tiny.Old</c> matched that namespace
    /// exactly and left every document in <c>Tiny.Old.Sub</c> behind — 53 of 203 files on the run that
    /// exposed it — reporting success the whole way. The result is a package split in half whose only
    /// symptom is broken references somewhere else entirely. If the caller meant to take the subtree
    /// they want IncludeSubNamespaces; if they did not, this line costs them one log entry.
    /// </remarks>
    [MessageLogging(EventId = 12407, Level = LogLevel.Warning,
        Message = "{stranded} document(s) declare a namespace beneath '{selector}' and were NOT moved — IncludeSubNamespaces is off")]
    public static partial IGenericMessage SubNamespacesStranded(ILogger logger, string selector, int stranded);

    /// <summary>Logs a document skipped because it is generated.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="filePath">The generated file.</param>
    /// <returns>The structured message.</returns>
    /// <remarks>
    /// Trace rather than Debug because on a large solution this fires for hundreds of obj/ files and would
    /// drown everything else at a level anyone leaves on.
    /// </remarks>
    [MessageLogging(EventId = 12402, Level = LogLevel.Trace,
        Message = "Skipping generated document: {filePath}")]
    public static partial IGenericMessage GeneratedDocumentSkipped(ILogger logger, string filePath);

    /// <summary>Logs the reference closure the target needs, and what was written.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="required">How many references the symbol graph says are needed.</param>
    /// <param name="written">How many reached the csproj.</param>
    /// <returns>The structured message.</returns>
    /// <remarks>
    /// Both numbers, deliberately. They diverged silently once — the closure was computed and reported
    /// while the csproj kept its three seed references — and a single number would have hidden it.
    /// </remarks>
    [MessageLogging(EventId = 12403, Level = LogLevel.Information,
        Message = "Reference closure: {required} required, {written} written to the target project")]
    public static partial IGenericMessage ReferenceClosureApplied(ILogger logger, int required, int written);

    /// <summary>Logs that a project could not be verified.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="project">The project whose compilation cannot bind.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 61030, Level = LogLevel.Warning,
        Message = "Cannot verify '{project}': its compilation has no framework references, so no finding from it is meaningful")]
    public static partial IGenericMessage ProjectUnverifiable(ILogger logger, string project);

    /// <summary>Logs a refusal because an affected project's compilation cannot bind.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="command">The command being run.</param>
    /// <param name="detail">Which project, and why it could not be verified.</param>
    /// <returns>The structured message.</returns>
    /// <remarks>Mirrors <c>RoslynResultCodes.ChangeCannotBeVerified</c> (31031).</remarks>
    [MessageLogging(EventId = 31031, Level = LogLevel.Error,
        Message = "{command} refused — change cannot be verified: {detail}")]
    public static partial IGenericMessage ChangeCannotBeVerified(ILogger logger, string command, string detail);

    /// <summary>Logs a refusal because the change would not compile.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="command">The command being run.</param>
    /// <param name="detail">The first surviving problem.</param>
    /// <returns>The structured message.</returns>
    /// <remarks>Mirrors <c>RoslynResultCodes.ChangeWouldNotCompile</c> (31030).</remarks>
    [MessageLogging(EventId = 31030, Level = LogLevel.Error,
        Message = "{command} refused — change would not compile: {detail}")]
    public static partial IGenericMessage ChangeWouldNotCompile(ILogger logger, string command, string detail);

    /// <summary>Logs a refusal because the caller named a generated file.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="filePath">The generated file the selector matched.</param>
    /// <returns>The structured message.</returns>
    /// <remarks>Mirrors <c>RoslynResultCodes.SelectorMatchedGeneratedFile</c> (31032).</remarks>
    [MessageLogging(EventId = 31032, Level = LogLevel.Error,
        Message = "Refused — selector matched a build-generated file: {filePath}")]
    public static partial IGenericMessage SelectorMatchedGeneratedFile(ILogger logger, string filePath);

    /// <summary>Logs that the caller overrode verification.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="command">The command being run.</param>
    /// <param name="reason">The caller's stated reason, if any.</param>
    /// <returns>The structured message.</returns>
    /// <remarks>
    /// Warning, not Information: proceeding past an unverifiable change is legitimate but exceptional, and
    /// it should be visible afterwards in a log nobody was watching at the time.
    /// </remarks>
    [MessageLogging(EventId = 12404, Level = LogLevel.Warning,
        Message = "{command} proceeding UNVERIFIED at caller's request: {reason}")]
    public static partial IGenericMessage ProceedingUnverified(ILogger logger, string command, string reason);

    /// <summary>Logs the completed outcome of a move.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="command">The command being run.</param>
    /// <param name="changed">How many documents changed.</param>
    /// <param name="followed">How many references were followed.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 12405, Level = LogLevel.Information,
        Message = "{command} complete: {changed} document(s) changed, {followed} reference(s) followed")]
    public static partial IGenericMessage MoveComplete(ILogger logger, string command, int changed, int followed);
}
