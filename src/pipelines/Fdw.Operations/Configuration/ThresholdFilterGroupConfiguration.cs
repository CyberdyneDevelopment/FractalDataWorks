using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Operations.Configuration;

/// <summary>A group of a rule's criteria (FDW-3936): every child must hold (All) or any one (Any). Child of ThresholdRuleConfiguration.</summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "Operations")]
public sealed partial class ThresholdFilterGroupConfiguration : IGenericConfiguration
{
    /// <summary>Gets or sets the unique identifier; the logical Id that ParentGroupId of a child group cites.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the Id of the parent group; none for the root. The API checks that it exists and that no cycle is made.</summary>
    public Guid? ParentGroupId { get; set; }

    /// <summary>Gets or sets whether every child must hold (<c>All</c>) or any one (<c>Any</c>).</summary>
    public string Operator { get; set; } = string.Empty;

    /// <summary>Gets or sets the place of the group among the groups of its parent, counted from 1; for display, the criteria do not depend on it.</summary>
    public int Ordinal { get; set; }

    /// <summary>Gets or sets the conditions of the group, in order.</summary>
    public IList<ThresholdFilterConditionConfiguration> Conditions { get; set; } = [];
}
