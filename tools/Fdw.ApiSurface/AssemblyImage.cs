namespace Fdw.ApiSurface;

/// <summary>One assembly to read: the target framework it was built for, its file name and its bytes.</summary>
internal sealed record AssemblyImage(string Framework, string Name, byte[] Bytes);
