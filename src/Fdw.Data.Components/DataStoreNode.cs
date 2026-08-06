using System;

namespace Fdw.Data.Components;

/// <summary>
/// Unified node type for the DataStore → Path → Container drill-down hierarchy.
/// Used with <c>NestedObjectPicker&lt;DataStoreNode&gt;</c> so the entire three-level tree
/// is modelled by a single generic type parameter.
/// </summary>
public sealed record DataStoreNode(string Name, string Kind, Guid Id = default);
