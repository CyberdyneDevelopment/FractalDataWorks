using Fdw.Collections;

namespace Fdw.Services.Calculations.Abstractions;

/// <summary>
/// Represents a scalar value type supported as a calculation input.
/// </summary>
public interface IScalarValueType : ITypeOption<int, ScalarValueTypeBase>
{
}
