namespace Fdw.UI.Pipelines.Clients.Models;

using System;
using System.Collections.Generic;

/// <summary>
/// Specifies columns to be dropped/discarded after a task completes.
/// This is a memory optimization that releases columns no longer needed downstream.
/// </summary>
public sealed class ColumnDisposal : IEquatable<ColumnDisposal>
{
    /// <summary>
    /// Gets or sets the columns to drop after this task.
    /// </summary>
    public IList<string> DropColumns { get; set; } = new List<string>();

    /// <summary>
    /// Gets or sets whether to use a whitelist approach (keep only specified columns).
    /// When true, KeepColumns is used instead of DropColumns.
    /// </summary>
    public bool UseKeepList { get; set; }

    /// <summary>
    /// Gets or sets the columns to keep (all others are dropped).
    /// Only used when UseKeepList is true.
    /// </summary>
    public IList<string> KeepColumns { get; set; } = new List<string>();

    /// <summary>
    /// Gets or sets whether to automatically drop columns not referenced by downstream tasks.
    /// This enables automatic memory optimization based on data lineage analysis.
    /// </summary>
    public bool AutoDispose { get; set; } = true;

    /// <summary>
    /// Creates a column disposal that drops specific columns.
    /// </summary>
    public static ColumnDisposal Drop(params string[] columns)
    {
        return new ColumnDisposal
        {
            DropColumns = new List<string>(columns),
            UseKeepList = false
        };
    }

    /// <summary>
    /// Creates a column disposal that keeps only specific columns.
    /// </summary>
    public static ColumnDisposal KeepOnly(params string[] columns)
    {
        return new ColumnDisposal
        {
            KeepColumns = new List<string>(columns),
            UseKeepList = true
        };
    }

    /// <summary>
    /// Creates a column disposal with automatic optimization.
    /// </summary>
    public static ColumnDisposal Auto()
    {
        return new ColumnDisposal { AutoDispose = true };
    }

    /// <summary>
    /// Creates a deep copy of this disposal specification.
    /// </summary>
    public ColumnDisposal Clone()
    {
        return new ColumnDisposal
        {
            DropColumns = new List<string>(DropColumns),
            UseKeepList = UseKeepList,
            KeepColumns = new List<string>(KeepColumns),
            AutoDispose = AutoDispose
        };
    }

    /// <inheritdoc />
    public bool Equals(ColumnDisposal? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return UseKeepList == other.UseKeepList &&
               AutoDispose == other.AutoDispose;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as ColumnDisposal);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 31 + UseKeepList.GetHashCode();
            hash = hash * 31 + AutoDispose.GetHashCode();
            return hash;
        }
    }
}
