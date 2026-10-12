using Fdw.Collections;

namespace Fdw.Data.Abstractions;

/// <summary>Base for the data classification levels.</summary>
/// <remarks>
/// No option id is passed: the id derives from the option's fully qualified type name, as for every
/// type option. That id is what a <c>ClassificationId</c> column stores, what
/// <c>DataClassifications.ById</c> looks up, and what <c>data.DataClassification.Id</c> is seeded with
/// (databases-seed 46). Renaming or moving a level's class changes its id, so the seed changes with it.
/// </remarks>
public abstract class DataClassificationBase : TypeOptionBase<DataClassificationBase>, IDataClassification
{
    /// <summary>Initializes a new instance of the <see cref="DataClassificationBase"/> class.</summary>
    /// <param name="name">The level's name, as seeded in <c>data.DataClassification.Name</c>.</param>
    /// <param name="sensitivity">The level's order, least sensitive first.</param>
    /// <param name="requiresInvitation">Whether only the owner and invited members may reach data at this level.</param>
    /// <param name="description">What the level means, as shown to the people choosing it.</param>
    protected DataClassificationBase(string name, int sensitivity, bool requiresInvitation, string description)
        : base(name, name, description, null)
    {
        Sensitivity = sensitivity;
        RequiresInvitation = requiresInvitation;
    }

    /// <inheritdoc />
    public int Sensitivity { get; }

    /// <inheritdoc />
    public bool RequiresInvitation { get; }
}
