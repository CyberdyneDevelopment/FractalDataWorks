// Assembly-level attributes for Fdw.Services.Data
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Fdw.Services.Data.Tests")]
// Why: this suite tests Fdw.Services.Data INTERNALS but needs a real connection to do it, so it
// is hosted in reference-servicetypes where the connections now live. Test-only grant: no
// production assembly in that repo gets internals access.
[assembly: InternalsVisibleTo("ReferenceConnections.Data.Tests")]
[assembly: InternalsVisibleTo("Fdw.Services.Connections.Limits.Tests")]
