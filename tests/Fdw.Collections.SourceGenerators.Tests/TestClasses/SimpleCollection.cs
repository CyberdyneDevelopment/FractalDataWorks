using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace TestScenarios;

/// <summary>
/// Simple immutable TypeCollection - basic scenario.
/// </summary>
public abstract class StatusBase : TypeOptionBase<int, StatusBase>
{
    protected StatusBase(int id, string name) : base(id, name) { }
}

[TypeCollection(typeof(StatusBase), typeof(StatusBase), typeof(Statuses))]
public partial class Statuses : TypeCollectionBase<StatusBase, StatusBase>
{
}

[TypeOption(typeof(Statuses), "Open")]
public class OpenStatus : StatusBase
{
    public OpenStatus() : base(1, "Open") { }
}

[TypeOption(typeof(Statuses), "Closed")]
public class ClosedStatus : StatusBase
{
    public ClosedStatus() : base(2, "Closed") { }
}
