namespace Fdw.Services.Data.Endpoints;

/// <summary>One data classification level, as offered to the people classifying data.</summary>
public class DataClassificationResponse
{
    /// <summary>Gets or sets the id to send as <c>ClassificationId</c> on a data store, path, container, field, data set, data set field or workspace.</summary>
    public int ClassificationId { get; set; }

    /// <summary>Gets or sets the level's name, for example <c>Confidential</c>.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets what the level means.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the level's order, least sensitive first.</summary>
    public int Sensitivity { get; set; }

    /// <summary>Gets or sets whether only the owner and invited members may reach data at this level.</summary>
    public bool RequiresInvitation { get; set; }
}
