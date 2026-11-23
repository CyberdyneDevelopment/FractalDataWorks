using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Abstractions.Flow;

namespace Fdw.Services.Etl.TestDouble;

/// <summary>
/// A component type from a package outside FDW (FDW-6133), declared in this test double, an assembly its consumers reference, because the
/// module initializer collects options from referenced assemblies only (FDW-6244). A graph that uses it is read and written by the graph API.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ComponentTypes), "AskModel")]
public sealed class AskModelComponentType : ComponentTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="AskModelComponentType"/> class.</summary>
    public AskModelComponentType()
        : base(
            "AskModel",
            "Ask model",
            "Asks a language model and keeps its answer.",
            "Control flow",
            holdsComponents: false,
            [
                new StageMember("model", "Text", true, "The model to ask.", DataType: new StringDataType()),
                new StageMember("temperature", "Number", false, "How much the answer may vary.", DataType: new DoubleDataType()),
                new StageMember("question", "Text", false, "What to ask; may name variables, read when the step runs.", ReadsWhenStepRuns: true, DataType: new StringDataType()),
            ])
    {
    }
}
