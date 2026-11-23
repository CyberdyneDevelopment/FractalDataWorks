using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using Fdw.Configuration;
using Fdw.Data.Abstractions;
using Fdw.Data.Abstractions.Mappers.PocoMappers;
using Fdw.Results;

namespace Fdw.Web.RestEndpoints.Tests.Query;

/// <summary>
/// The mapper the generator would emit for <see cref="DescribedThing"/> with its Description [NotStored], written by hand: this project runs no mapper
/// generator. It reaches Name only. Every member the query does not use throws, so a test that reached one would fail for that reason and not for a default.
/// </summary>
internal sealed class DescribedThingMapper()
    : PocoMapperBase("Fdw.Web.RestEndpoints.Tests.Query.DescribedThing", typeof(DescribedThing))
{
    public override IReadOnlyList<IChildCascadeDescriptor> CascadeChildren => throw new NotSupportedException();

    public override IReadOnlyList<string> GetPropertyNames() => ["Name"];

    public override IReadOnlyDictionary<string, object?> MapToParameters(object instance)
        => new Dictionary<string, object?>(StringComparer.Ordinal) { ["Name"] = ((DescribedThing)instance).Name };

    public override IGenericResult<object> MapFromReader(DbDataReader reader, IStorageContainer container) => throw new NotSupportedException();

    public override IGenericResult<object> MapFromDictionary(IDictionary<string, object?> data) => throw new NotSupportedException();

    public override void SetValue(object instance, string columnName, object? value) => throw new NotSupportedException();

    public override IList CreateList() => throw new NotSupportedException();

    public override Array CreateArray(int length) => throw new NotSupportedException();

    public override IGenericConfiguration? GetTypedBody(object parent) => throw new NotSupportedException();

    public override void SetTypedBody(object parent, IGenericConfiguration? body) => throw new NotSupportedException();
}
