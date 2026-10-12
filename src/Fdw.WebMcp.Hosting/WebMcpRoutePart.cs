namespace Fdw.WebMcp.Hosting;

/// <summary>
/// One piece of a tool's route: literal text, or a parameter filled from the agent's input.
/// </summary>
/// <param name="Text">The literal text, or the request property a parameter reads.</param>
/// <param name="IsParameter">Whether <paramref name="Text"/> names an input rather than being literal.</param>
/// <remarks>
/// The route is kept as parts rather than as a template to substitute into, so each value is
/// encoded on its way in. A value carrying '/' or '?' substituted into a template would reshape
/// the path it landed in.
/// </remarks>
public readonly record struct WebMcpRoutePart(string Text, bool IsParameter);
