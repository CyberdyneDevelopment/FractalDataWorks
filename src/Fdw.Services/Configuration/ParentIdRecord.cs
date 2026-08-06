using System;
using Fdw.Data;

namespace Fdw.Services.Configuration;

/// <summary>Minimal record for resolving a parent row's Id by name in child config lookups.</summary>
[GenerateMapper]
internal sealed class ParentIdRecord
{
    public Guid Id { get; set; }
}
