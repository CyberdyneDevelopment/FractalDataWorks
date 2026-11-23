using Fdw.Data.Abstractions;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Abstractions.Flow;

namespace Fdw.Services.Etl.Tests.Graphs.DeclaredSettings;

/// <summary>
/// A component type the way a package outside FDW states one (FDW-6133): its settings have no column of the component, so they are stored as declared
/// settings. Built directly, not registered, so the tests hand it to what they test.
/// </summary>
internal sealed class ModelCallComponentType : ComponentTypeBase
{
    public ModelCallComponentType()
        : base(
            "ModelCall",
            "Model call",
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
