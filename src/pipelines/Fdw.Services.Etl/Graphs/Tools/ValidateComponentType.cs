using Fdw.Collections.Attributes;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Transforms;

namespace Fdw.Services.Etl.Graphs.Tools;

/// <summary>The Validate tool: the Validate stage kind placed in a graph.</summary>
[TypeOption(typeof(ComponentTypes), "Validate")]
public sealed class ValidateComponentType : StageToolComponentTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="ValidateComponentType"/> class.</summary>
    public ValidateComponentType() : base(new ValidateTransformType(), "Validate", "The rows that passed every check; failing rows go to the error output or stop the run.")
    {
    }
}
