namespace Fdw.Tds.Endpoints.Tests;

/// <summary>A DONE, DONEPROC or DONEINPROC token read back from a response.</summary>
internal readonly record struct DoneToken(byte Token, ushort Status, ushort Command, long RowCount);
