using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace TestScenarios;

// Simple parent-child test without complex inheritance
public abstract class ChildTypeBase : TypeOptionBase<int, ChildTypeBase>
{
    protected ChildTypeBase(int id, string name) : base(id, name) { }
}

[TypeCollection(typeof(ChildTypeBase), typeof(ChildTypeBase), typeof(ChildTypes))]
public partial class ChildTypes : TypeCollectionBase<ChildTypeBase, ChildTypeBase>
{
}

[TypeOption(typeof(ChildTypes), "Option1")]
public class Option1 : ChildTypeBase
{
    public Option1() : base(1, "Option1") { }
}
