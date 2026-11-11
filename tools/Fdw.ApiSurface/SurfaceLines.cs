using System;
using System.Collections.Generic;

namespace Fdw.ApiSurface;

/// <summary>The records of a surface, written as the four tab-separated fields apisurface.py reads.</summary>
internal sealed class SurfaceLines
{
    private readonly List<string> _lines = [];

    public void Add(string framework, string kind, string key, string detail)
    {
        _lines.Add(string.Join('\t', framework, kind, key, detail));
    }

    public IReadOnlyList<string> Sorted()
    {
        List<string> copy = [.. _lines];
        copy.Sort(StringComparer.Ordinal);
        return copy;
    }
}
