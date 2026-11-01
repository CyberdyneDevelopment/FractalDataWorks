using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Abstractions;

/// <summary>
/// TypeCollection for data mappers between type systems.
/// Contains explicit mapper implementations.
/// Provides GetMapper() factory that returns explicit or default mapper.
/// </summary>
[TypeCollection(typeof(DataMapperBase<,>),
                typeof(IDataMapper<,>),
                typeof(DataMappers))]
public abstract partial class DataMappers
    : TypeCollectionBase<DataMapperBase<IDataTypeConverter, IDataTypeConverter>,
                        IDataMapper<IDataTypeConverter, IDataTypeConverter>>
{

}
