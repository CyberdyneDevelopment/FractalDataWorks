using System;
using System.Collections.Generic;
using System.Text.Json;
using Fdw.Collections;

namespace Fdw.Web.RestEndpoints.Query;

/// <summary>One type a calculation can yield for a filter: the types of field it may be compared with, and the literal it becomes.</summary>
public interface IFilterCalculationResultKind : ITypeOption<int, FilterCalculationResultKindBase>
{
    /// <summary>Gets the CLR type the calculation yields.</summary>
    Type ValueType { get; }

    /// <summary>Gets how a refusal words this kind of result: "a number".</summary>
    string Word { get; }

    /// <summary>Gets the types of field a result of this kind may be compared with.</summary>
    IReadOnlyList<Type> FieldTypes { get; }

    /// <summary>Writes a result of this kind as the literal a filter holds.</summary>
    /// <param name="value">The result, boxed as <see cref="ValueType"/>.</param>
    /// <returns>The literal.</returns>
    JsonElement Literal(object value);
}
