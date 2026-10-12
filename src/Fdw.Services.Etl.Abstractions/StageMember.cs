namespace Fdw.Services.Etl.Abstractions;

/// <summary>
/// One member of a pipeline stage's body that a stage kind owns, as the type-collection listing describes it.
/// </summary>
/// <param name="Name">The member's name in the body, in camel case.</param>
/// <param name="Kind">What the member holds, for the console's picker (for example <c>Expression</c> or <c>CheckList</c>).</param>
/// <param name="IsRequired">Whether a stage of the kind must state the member.</param>
/// <param name="HelpText">A short explanation for the person filling it in.</param>
public sealed record StageMember(string Name, string Kind, bool IsRequired, string HelpText);
