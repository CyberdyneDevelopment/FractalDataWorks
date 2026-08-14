namespace Fdw.TUI.Management.Services;

/// <summary>
/// One failed assertion, named the way the suite names it.
/// </summary>
/// <param name="Request">The request whose assertion failed.</param>
/// <param name="Assertion">The assertion that failed.</param>
/// <param name="Detail">What the runner reported.</param>
public sealed record NewmanFailure(string Request, string Assertion, string Detail);
