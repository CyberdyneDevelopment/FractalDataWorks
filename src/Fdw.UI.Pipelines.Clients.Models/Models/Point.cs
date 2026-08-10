using System;
using System.Runtime.InteropServices;

namespace Fdw.UI.Pipelines.Clients.Models;

/// <summary>
/// Represents a 2D point for canvas positioning.
/// </summary>
[StructLayout(LayoutKind.Auto)]
public readonly struct Point : IEquatable<Point>
{
    /// <summary>
    /// Gets the X coordinate.
    /// </summary>
    public double X { get; }

    /// <summary>
    /// Gets the Y coordinate.
    /// </summary>
    public double Y { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Point"/> struct.
    /// </summary>
    /// <param name="x">The X coordinate.</param>
    /// <param name="y">The Y coordinate.</param>
    public Point(double x, double y)
    {
        X = x;
        Y = y;
    }

    /// <summary>
    /// Gets a point at the origin (0, 0).
    /// </summary>
    public static Point Zero => new(0, 0);

    /// <inheritdoc/>
    public bool Equals(Point other) => X.Equals(other.X) && Y.Equals(other.Y);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is Point other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        unchecked
        {
            return (X.GetHashCode() * 397) ^ Y.GetHashCode();
        }
    }

    /// <summary>
    /// Determines whether two points are equal.
    /// </summary>
    public static bool operator ==(Point left, Point right) => left.Equals(right);

    /// <summary>
    /// Determines whether two points are not equal.
    /// </summary>
    public static bool operator !=(Point left, Point right) => !left.Equals(right);

    /// <summary>
    /// Adds another point to this point.
    /// </summary>
    public Point Add(Point other) => new Point(X + other.X, Y + other.Y);

    /// <summary>
    /// Subtracts another point from this point.
    /// </summary>
    public Point Subtract(Point other) => new Point(X - other.X, Y - other.Y);

    /// <summary>
    /// Scales this point by a scalar factor.
    /// </summary>
    public Point Scale(double factor) => new Point(X * factor, Y * factor);

    /// <summary>
    /// Calculates the Euclidean distance to another point.
    /// </summary>
    public double DistanceTo(Point other)
    {
        var dx = X - other.X;
        var dy = Y - other.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    /// <summary>
    /// Clamps this point within the given min and max bounds.
    /// </summary>
    public Point Clamp(Point min, Point max) =>
        new Point(
            Math.Max(min.X, Math.Min(max.X, X)),
            Math.Max(min.Y, Math.Min(max.Y, Y)));

    /// <summary>
    /// Adds two points.
    /// </summary>
    public static Point operator +(Point a, Point b) => a.Add(b);

    /// <summary>
    /// Subtracts two points.
    /// </summary>
    public static Point operator -(Point a, Point b) => a.Subtract(b);

    /// <summary>
    /// Scales a point by a scalar.
    /// </summary>
    public static Point operator *(Point p, double s) => p.Scale(s);

    /// <inheritdoc/>
    public override string ToString() => $"({X:F2}, {Y:F2})";
}
