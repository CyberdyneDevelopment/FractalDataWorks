using System;
using System.Collections.Generic;

namespace Fdw.Services.Abstractions;

/// <summary>
/// Narrows the rows of one kind of child collection a configuration read loads, to those whose property holds one of the values (FDW-4328).
/// </summary>
/// <remarks>
/// A read of a datastore composes every path, container, field and key of it. A caller that needs two containers of a store of 500 names them here
/// and the level whose rows are <see cref="ChildType"/> reads those and no others; the levels below read only the children of the rows that were read.
/// The narrowing applies at whatever depth <see cref="ChildType"/> is met and to no other kind of row.
/// </remarks>
/// <param name="ChildType">The record type of the collection to narrow, for example the data container configuration.</param>
/// <param name="Property">The column the values are compared with, for example <c>Name</c>.</param>
/// <param name="Values">The values a row may hold; a row holding none of them is not read.</param>
public sealed record ChildNarrowing(Type ChildType, string Property, IReadOnlyList<string> Values);
