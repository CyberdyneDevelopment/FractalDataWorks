using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace TestScenarios;

// Simple parent-child test without complex inheritance
public abstract class ChildTypeBase : TypeOptionBase<ChildTypeBase>
{
    protected ChildTypeBase(string name) : base(name) { }
}

[TypeCollection(typeof(ChildTypeBase), typeof(ChildTypeBase), typeof(ChildTypes))]
public partial class ChildTypes : TypeCollectionBase<ChildTypeBase, ChildTypeBase>
{
}

[TypeOption(typeof(ChildTypes), "Option1")]
public class Option1 : ChildTypeBase
{
    public Option1() : base("Option1") { }
}
