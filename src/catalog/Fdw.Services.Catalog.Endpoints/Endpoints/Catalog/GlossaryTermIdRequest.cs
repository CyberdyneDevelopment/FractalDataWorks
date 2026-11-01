using System;
using System.Collections.Generic;

namespace Fdw.Services.Catalog.Endpoints;

/// <summary>Request containing a glossary term identifier.</summary>
public class GlossaryTermIdRequest
{
    /// <summary>Gets or sets the unique identifier of the glossary term.</summary>
    public Guid Id { get; set; }
}