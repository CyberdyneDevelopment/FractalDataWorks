using System;
using Fdw.Collections;

namespace Fdw.Commands.Development.Abstractions;

/// <summary>
/// Represents a category of development commands (Analysis, Compilation, Formatting, etc.).
/// Categories are shared across all language implementations.
/// </summary>
public interface IDevelopmentCommandCategory : ITypeOption<int, DevelopmentCommandCategoryBase>
{
}
