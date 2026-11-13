namespace Fdw.Services.Etl.Graphs;

/// <summary>A SQL step of a graph (FDW-5457): the component that holds the script, the data store it runs on, and the script as stored.</summary>
/// <param name="Component">The component's name.</param>
/// <param name="DataStore">The data store it names, as stored: a name, or a parameter in double braces.</param>
/// <param name="Script">The script text, with its <c>$(Name)</c> tokens as stored.</param>
public sealed record GraphScript(string Component, string DataStore, string Script);
