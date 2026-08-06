using System.Diagnostics.CodeAnalysis;
using Fdw.Commands.Abstractions;

namespace Fdw.Commands.Data;

/// <summary>
/// Simple category for data commands.
/// This is a temporary implementation until full category system is implemented.
/// </summary>
#pragma warning disable TC001 // Type inherits from TypeCollection base but missing TypeOption attribute
[ExcludeFromCodeCoverage] // Internal singleton configuration - no testable behavior
internal sealed class DataCommandCategory : CommandCategoryBase
#pragma warning restore TC001
{
    /// <summary>
    /// Gets the singleton Query category.
    /// </summary>
    public static readonly DataCommandCategory Query = new(1, "Query", requiresTransaction: false, isMutation: false, isCacheable: true);

    /// <summary>
    /// Gets the singleton Insert category.
    /// </summary>
    public static readonly DataCommandCategory Insert = new(2, "Insert", requiresTransaction: true, isMutation: true, isCacheable: false);

    /// <summary>
    /// Gets the singleton Update category.
    /// </summary>
    public static readonly DataCommandCategory Update = new(3, "Update", requiresTransaction: true, isMutation: true, isCacheable: false);

    /// <summary>
    /// Gets the singleton Delete category.
    /// </summary>
    public static readonly DataCommandCategory Delete = new(4, "Delete", requiresTransaction: true, isMutation: true, isCacheable: false);

    private DataCommandCategory(int id, string name, bool requiresTransaction, bool isMutation, bool isCacheable)
        : base(id, name, requiresTransaction, !isMutation, isCacheable, isMutation, 50)
    {
    }
}
