namespace Fdw.Data.UI.Components.DataCommand;

/// <summary>Paging specification for Query commands.</summary>
public sealed class DataCommandPaging
{
    /// <summary>Gets or sets the number of rows to skip (OFFSET).</summary>
    public int Skip { get; set; }

    /// <summary>Gets or sets the maximum number of rows to return (FETCH NEXT). 0 = no limit.</summary>
    public int Take { get; set; }
}
