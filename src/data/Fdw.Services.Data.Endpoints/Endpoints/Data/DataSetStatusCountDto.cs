namespace Fdw.Services.Data.Endpoints;

/// <summary>How many data sets have one status (FDW-4488).</summary>
public sealed class DataSetStatusCountDto
{
    /// <summary>Gets or sets the status: the name of an option of <c>DataSetStatuses</c>.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Gets or sets how many data sets have that status.</summary>
    public int Count { get; set; }
}
