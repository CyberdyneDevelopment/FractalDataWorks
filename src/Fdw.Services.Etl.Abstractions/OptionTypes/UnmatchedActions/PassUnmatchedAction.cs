using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>An unmatched record passes on unchanged, after the records the paths produced.</summary>
[TypeOption(typeof(UnmatchedActions), "Pass")]
public sealed class PassUnmatchedAction : UnmatchedActionBase
{
    /// <summary>Initializes a new instance of the <see cref="PassUnmatchedAction"/> class.</summary>
    public PassUnmatchedAction() : base("Pass", "Pass on", "Records that match no path continue after the outputs of the paths", rejectsRecord: false)
    {
    }
}
