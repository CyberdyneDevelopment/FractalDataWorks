using Fdw.Collections;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>
/// Base class for the options of <see cref="SplitRoutings"/>, using the CRTP pattern.
/// </summary>
public abstract class SplitRoutingBase : TypeOptionBase<SplitRoutingBase>, ISplitRouting
{
    /// <summary>Initializes a new instance of the <see cref="SplitRoutingBase"/> class.</summary>
    /// <param name="name">The option name.</param>
    /// <param name="description">Where a row that meets several filters goes.</param>
    /// <param name="sendsToEveryMatch">Whether a row goes to every output whose filter it meets.</param>
    protected SplitRoutingBase(string name, string description, bool sendsToEveryMatch)
        : base(name, name, description, "SplitRoutings")
    {
        SendsToEveryMatch = sendsToEveryMatch;
    }

    /// <inheritdoc />
    public bool SendsToEveryMatch { get; }
}
