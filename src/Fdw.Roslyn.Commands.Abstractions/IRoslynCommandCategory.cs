using Fdw.Commands.Development.Abstractions;

namespace Fdw.Roslyn.Commands.Abstractions;

/// <summary>
/// Represents a category of Roslyn commands.
/// Extends <see cref="IDevelopmentCommandCategory"/> for C# specific categorization.
/// </summary>
public interface IRoslynCommandCategory : IDevelopmentCommandCategory
{
}
