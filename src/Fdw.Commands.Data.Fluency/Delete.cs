namespace Fdw.Commands.Data.Extensions;

/// <summary>
/// Fluent builder for delete commands.
/// </summary>
/// <remarks>
/// <para>
/// Provides a fluent API for creating delete commands:
/// <code>
/// // Simple delete by ID
/// var cmd = Delete.From("Customers")
///     .DataStore("CRM")
///     .Path("sales")
///     .Where("Id", customerId)
///     .Build();
///
/// // Delete with complex filter
/// var cmd = Delete.From("Customers")
///     .DataStore("CRM")
///     .Path("sales")
///     .BeginAndGroup()
///         .Where("Status", "Inactive")
///         .Where("LastLogin", new LessThanOperator(), cutoffDate)
///     .EndGroup()
///     .Build();
/// </code>
/// </para>
/// </remarks>
public static class Delete
{
    /// <summary>
    /// Starts building a delete command for the specified container.
    /// </summary>
    /// <param name="containerName">The container name.</param>
    /// <returns>A builder for delete commands.</returns>
    public static DeleteCommandBuilder From(string containerName)
    {
        return new DeleteCommandBuilder(containerName);
    }
}