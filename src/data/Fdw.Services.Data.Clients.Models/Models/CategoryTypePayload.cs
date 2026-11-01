namespace Fdw.Services.Data.Clients.Models;

/// <summary>A type of category an administrator can create (FDW-1465).</summary>
public sealed class CategoryTypePayload
{
    /// <summary>Gets or sets the type's name, which a category states as its type.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets what a category of this type is for.</summary>
    public string Description { get; set; } = string.Empty;
}
