using System.Collections.Generic;
using Fdw.Collections;

namespace Fdw.Commands.Development.Abstractions;

/// <summary>
/// Represents a command that operates on development artifacts (code, projects, solutions).
/// This is the base interface for language-specific commands (Roslyn, JavaScript, Java, Go, etc.).
/// </summary>
public interface IDevelopmentCommand : ITypeOption<int, DevelopmentCommandBase>
{
    /// <summary>
    /// Gets the command category (Analysis, Compilation, Formatting, etc.).
    /// </summary>
    IDevelopmentCommandCategory CommandCategory { get; }

    /// <summary>
    /// Gets the parameters for this command.
    /// </summary>
    IReadOnlyList<DevelopmentCommandParameter> Parameters { get; }
}
