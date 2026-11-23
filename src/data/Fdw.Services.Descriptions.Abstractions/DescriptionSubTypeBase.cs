using Fdw.Collections;

namespace Fdw.Services.Descriptions.Abstractions;

/// <summary>Base class for the description sub-types.</summary>
/// <remarks>
/// No id is passed: <see cref="TypeOptionBase{TBase}"/> derives one from the option's fully qualified type name, so a package can declare its own
/// sub-type. The name is the part of this contract that must not change once descriptions exist.
/// </remarks>
public abstract class DescriptionSubTypeBase : TypeOptionBase<DescriptionSubTypeBase>, IDescriptionSubType
{
    /// <summary>Initializes a new instance of the <see cref="DescriptionSubTypeBase"/> class.</summary>
    /// <param name="name">The sub-type name, which is what a request and the table's <c>DescriptionSubType</c> column name.</param>
    protected DescriptionSubTypeBase(string name)
        : base(name)
    {
    }
}
