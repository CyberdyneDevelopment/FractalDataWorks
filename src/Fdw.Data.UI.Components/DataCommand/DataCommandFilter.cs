using System.Collections.Generic;

namespace Fdw.Data.UI.Components.DataCommand;

/// <summary>Filter clause for WHERE conditions in Query, Update, and Upsert commands.</summary>
public sealed class DataCommandFilter
{
    /// <summary>Gets or sets the flat list of filter clauses.</summary>
    public IList<DataCommandFilterClause> Clauses { get; set; } = new List<DataCommandFilterClause>();
}
