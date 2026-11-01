using System;
using System.Collections.Generic;

namespace Fdw.Services.Data.Endpoints;

/// <summary>Every stored version of one data set field, oldest first.</summary>
public class DataSetFieldHistoryResponse
{
    /// <summary>Gets or sets the field's logical id, shared by all of its versions.</summary>
    public Guid FieldId { get; set; }

    /// <summary>Gets or sets the field name.</summary>
    public string Field { get; set; } = string.Empty;

    /// <summary>Gets or sets the data set name.</summary>
    public string DataSet { get; set; } = string.Empty;

    /// <summary>Gets or sets the versions, oldest first. The last one is current.</summary>
    public IReadOnlyList<DataSetFieldVersionDto> Versions { get; set; } = [];
}
