using System.Collections.Generic;
using Fdw.Protocols.Tds;

namespace Fdw.Tds.Endpoints.Tests;

/// <summary>One result set read back from a response: its column names, their types and its rows.</summary>
internal sealed class ResultSet
{
    public List<string> Columns { get; } = [];

    public List<TdsTypeInfo> Types { get; } = [];

    public List<object?[]> Rows { get; } = [];
}
