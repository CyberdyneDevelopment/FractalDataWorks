using System.Text.Json.Serialization;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>Which part of a set of children an explode sent, and whether more follow: the paging of a children response.</summary>
public sealed class MapPageDto
{
    /// <summary>Gets or sets how many children, in name order, precede the page.</summary>
    public int Skip { get; set; }

    /// <summary>Gets or sets how many children the page holds at most; nothing when every remaining child was asked for.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Take { get; set; }

    /// <summary>Gets or sets whether children follow this page.</summary>
    public bool HasMore { get; set; }

    /// <summary>Gets or sets how many children match in all: the filter applied, the page not.</summary>
    public int Total { get; set; }
}
