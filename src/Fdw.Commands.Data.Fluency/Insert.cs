using System;
using System.Collections.Generic;
using Fdw.Commands.Data;

namespace Fdw.Commands.Data.Extensions;

/// <summary>
/// Fluent builder for insert commands.
/// </summary>
/// <remarks>
/// <para>
/// Provides a fluent API for creating insert commands:
/// <code>
/// // Single insert with full path specification
/// var cmd = Insert.Into&lt;Customer&gt;("Customers")
///     .DataStore("CRM")
///     .Path("sales")
///     .Value(customer);
///
/// // Batch insert (batched multi-row VALUES)
/// var cmd = Insert.IntoMany&lt;Customer&gt;("Customers")
///     .DataStore("CRM")
///     .Path("sales")
///     .Values(customers);
///
/// // Bulk insert (SqlBulkCopy)
/// var cmd = Insert.Bulk&lt;Customer&gt;("Customers")
///     .DataStore("CRM")
///     .Path("sales")
///     .Values(largeCustomerList);
/// </code>
/// </para>
/// </remarks>
public static class Insert
{
    /// <summary>
    /// Starts building an insert command for a single entity.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="containerName">The container name.</param>
    /// <returns>A builder for single-entity inserts.</returns>
    public static InsertSingleBuilder<T> Into<T>(string containerName)
    {
        return new InsertSingleBuilder<T>(containerName);
    }

    /// <summary>
    /// Starts building a batch insert command for multiple entities.
    /// Uses ACID-compliant batched multi-row INSERT statements.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="containerName">The container name.</param>
    /// <returns>A builder for batch inserts.</returns>
    public static InsertBatchBuilder<T> IntoMany<T>(string containerName)
    {
        return new InsertBatchBuilder<T>(containerName);
    }

    /// <summary>
    /// Starts building a bulk insert command for large datasets.
    /// Uses database-specific bulk mechanisms (SqlBulkCopy, etc.).
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="containerName">The container name.</param>
    /// <returns>A builder for bulk inserts.</returns>
    public static BulkInsertBuilder<T> Bulk<T>(string containerName)
    {
        return new BulkInsertBuilder<T>(containerName);
    }
}