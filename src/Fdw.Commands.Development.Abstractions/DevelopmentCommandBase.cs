using System;
using System.Collections.Generic;
using Fdw.Collections;

namespace Fdw.Commands.Development.Abstractions;

/// <summary>
/// Base class for development commands.
/// Commands are stateless data objects that describe an operation to perform.
/// </summary>
public abstract class DevelopmentCommandBase : TypeOptionBase<int, DevelopmentCommandBase>, IDevelopmentCommand
{
    /// <summary>
    /// Gets the command category.
    /// </summary>
    public IDevelopmentCommandCategory CommandCategory { get; }

    /// <summary>
    /// Gets the parameters for this command.
    /// </summary>
    public abstract IReadOnlyList<DevelopmentCommandParameter> Parameters { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DevelopmentCommandBase"/> class.
    /// </summary>
    /// <param name="name">The name of the command.</param>
    /// <param name="category">The category of the command.</param>
    /// <param name="description">The description of the command.</param>
    protected DevelopmentCommandBase(string name, IDevelopmentCommandCategory category, string description)
        : base(GenerateIdFromName(name), name, name, name, description, "DevelopmentCommand")
    {
        CommandCategory = category ?? throw new ArgumentNullException(nameof(category));
    }

    /// <summary>
    /// Generates a deterministic ID from a command name using FNV-1a hash.
    /// </summary>
    private static int GenerateIdFromName(string name)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentNullException(nameof(name));

        unchecked
        {
            const int FnvPrime = 0x01000193;
            const int FnvOffsetBasis = (int)0x811C9DC5;

            int hash = FnvOffsetBasis;
            foreach (char c in name)
            {
                hash ^= c;
                hash *= FnvPrime;
            }
            return hash & 0x7FFFFFFF;
        }
    }
}
