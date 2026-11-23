using Fdw.Data.Abstractions;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Abstractions.Flow;

namespace Fdw.Operations.Endpoints.Tests.TypeCollections;

/// <summary>A component type the way a package outside FDW states one (FDW-6133), built directly and not registered.</summary>
internal sealed class DeclaringComponentType : ComponentTypeBase
{
    public DeclaringComponentType()
        : base(
            "ModelCall",
            "Model call",
            "Asks a language model and keeps its answer.",
            "Control flow",
            holdsComponents: false,
            [
                new StageMember("temperature", "Number", false, "How much the answer may vary.", DataType: new DoubleDataType()),
                new StageMember("question", "Text", false, "What to ask.", ReadsWhenStepRuns: true, DataType: new StringDataType()),
            ])
    {
    }
}
