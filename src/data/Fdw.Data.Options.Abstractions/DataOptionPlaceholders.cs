using Fdw.Conventions;

namespace Fdw.Data.Options;

/// <summary>
/// The text a data option states where it stands for an option that is not registered. It is never a name a stored row can name.
/// </summary>
/// <remarks>Why declared here: FDW055 accepts a placeholder only as a [PlaceholderText] constant.</remarks>
[PlaceholderText]
public static class DataOptionPlaceholders
{
    /// <summary>The name of the option a data option type answers with when it has no option collection of its own.</summary>
    public const string UnregisteredName = "Unregistered";
}
