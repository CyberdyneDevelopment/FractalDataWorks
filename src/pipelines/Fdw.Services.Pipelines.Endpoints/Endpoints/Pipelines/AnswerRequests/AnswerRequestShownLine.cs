namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>One thing a person is shown with an answer request (FDW-6142): what it is (a title, a body, an option) and its text.</summary>
/// <param name="Name">What the line is.</param>
/// <param name="Text">The text shown.</param>
public sealed record AnswerRequestShownLine(string Name, string Text);
