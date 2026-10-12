using Fdw.Roslyn.Commands.Refactoring.Results;

namespace Fdw.Roslyn.Commands.Refactoring.Tests;

/// <summary>
/// Outcome of running <see cref="InheritDocTestHarness.RunAsync"/>: the success flag, the report, and the
/// rewritten document text.
/// </summary>
/// <param name="IsSuccess">Whether the translator returned a successful result.</param>
/// <param name="Data">The result report, or <see langword="null"/> when the run failed.</param>
/// <param name="NewText">The document text after rewriting, or empty when the run failed.</param>
public sealed record HarnessRun(bool IsSuccess, ResolveInheritDocResult? Data, string NewText);
