namespace Fdw.Services.Data.Endpoints;

/// <summary>A note to add to a field out of a Graph data set (FDW-5452): the kind and what it says. Who wrote it and when are the caller and now.</summary>
public sealed class AddGraphFieldNoteRequest
{
    /// <summary>Gets or sets the name of the Graph data set.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the name of the field out.</summary>
    public string Field { get; set; } = string.Empty;

    /// <summary>Gets or sets what the note is: the name of a <c>GraphFieldNoteKinds</c> option (Question or Reply).</summary>
    public string Kind { get; set; } = string.Empty;

    /// <summary>Gets or sets what the note says.</summary>
    public string Body { get; set; } = string.Empty;
}
