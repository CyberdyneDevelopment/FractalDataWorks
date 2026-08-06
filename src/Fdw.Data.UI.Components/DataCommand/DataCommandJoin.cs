using System;
using System.Collections.Generic;

namespace Fdw.Data.UI.Components.DataCommand;

/// <summary>JOIN clause for a Query command.</summary>
public sealed class DataCommandJoin
{
    /// <summary>Gets or sets the join kind (Inner, Left, Right, Full, Cross).</summary>
    public string Kind { get; set; } = "Inner";

    /// <summary>Gets or sets the right-side container ID.</summary>
    public Guid ContainerId { get; set; }

    /// <summary>Gets or sets the alias for the right-side container.</summary>
    public string Alias { get; set; } = string.Empty;

    /// <summary>Gets or sets the equi-join ON conditions.</summary>
    public IList<DataCommandJoinCondition> On { get; set; } = new List<DataCommandJoinCondition>();
}
