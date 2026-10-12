namespace Fdw.Commands.Data.Extensions;

/// <summary>
/// Fluent builder for update commands.
/// </summary>
/// <remarks>
/// <para>
/// Provides a fluent API for creating update commands:
/// <code>
/// // Update with entity
/// var cmd = Update.In&lt;Customer&gt;("Customers")
///     .DataStore("CRM")
///     .Path("sales")
///     .Where("Id", customerId)
///     .Value(updatedCustomer);
///
/// // Update with filter (updates matching records with provided entity data)
/// var cmd = Update.In&lt;Customer&gt;("Customers")
///     .DataStore("CRM")
///     .Path("sales")
///     .BeginOrGroup()
///         .Where("Status", "Inactive")
///         .Where("Status", "Pending")
///     .EndGroup()
///     .Value(updatedCustomer);
/// </code>
/// </para>
/// </remarks>
public static class Update
{
    /// <summary>
    /// Starts building an update command for the specified container.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="containerName">The container name.</param>
    /// <returns>A builder for update commands.</returns>
    public static UpdateCommandBuilder<T> In<T>(string containerName)
    {
        return new UpdateCommandBuilder<T>(containerName);
    }
}