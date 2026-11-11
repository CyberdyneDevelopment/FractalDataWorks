using System.Collections.Generic;

namespace Fdw.ApiSurface;

/// <summary>The names of the generic parameters in scope, so a signature can print <c>T</c> rather than an index.</summary>
internal sealed class GenericNames
{
    public GenericNames(IReadOnlyList<string> type, IReadOnlyList<string> method)
    {
        Type = type;
        Method = method;
    }

    public static GenericNames None { get; } = new GenericNames([], []);

    public IReadOnlyList<string> Type { get; }

    public IReadOnlyList<string> Method { get; }
}
