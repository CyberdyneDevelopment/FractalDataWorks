using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>Sets a declared variable to a value of a stated type, or to the result of a calculation (FDW-2042).</summary>
[TypeOption(typeof(ComponentTypes), "SetVariable")]
public sealed class SetVariableComponentType : ComponentTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="SetVariableComponentType"/> class.</summary>
    public SetVariableComponentType() : base(
        "SetVariable",
        "Set variable",
        "Sets a declared variable to a value of a stated type, or, when a transformation is named, to the result of that field transformation applied to a parameter or variable; several Set variable components, one in a later stage than the last, chain several transformations.",
        "Control flow",
        false,
        [
            new StageMember("variable", "Variable", true, "The variable to set; it must be declared by the graph."),
            new StageMember("valueType", "Text", false, "The data type of the value; required without a transformation, and not given with one, because the transformation gives the type."),
            new StageMember("value", "Text", false, "Without a transformation, the value as text; leave it out to set the variable to null. With one, the value to transform: a parameter or variable in double braces, which keeps its type, or text that may name variables in double braces."),
            new StageMember("transformation", "Text", false, "The name of the field transformation to apply to the value; leave it out to set the value as it is."),
            new StageMember("parameters", "Parameters", false, "The transformation's parameters, as text; a value may name a variable in double braces."),
        ])
    {
    }
}
