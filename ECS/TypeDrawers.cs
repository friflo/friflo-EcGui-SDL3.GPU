using Friflo.EcGui;

// ReSharper disable InconsistentNaming
// ReSharper disable once CheckNamespace
namespace Demo;

/// <summary><see cref="TypeDrawer"/>'s are optional.<br/>
/// They are used to render common types like: Coordinate, Color, Texture, Mesh, ... in a single line.<br/>
/// Without <see cref="TypeDrawer"/>'s those types are rendered across multiple lines.
/// </summary>
internal static class TypeDrawers
{
    /// <summary>
    /// Register custom <see cref="TypeDrawer"/>'s to render custom types in a single line or Explorer cell.
    /// </summary>
    internal static void Register()
    {
        EcGui.Setup.RegisterTypeDrawer<Coord2>    (new Coord2Drawer());
        EcGui.Setup.RegisterTypeDrawer<Coord3>    (new Coord3Drawer());
        EcGui.Setup.RegisterTypeDrawer<ColorRGB>  (new ColorRGBDrawer());
        EcGui.Setup.RegisterTypeDrawer<ColorRGBA> (new ColorRGBADrawer());
    }
}

/// <summary>Display the components of a 2D point struct <see cref="Coord2"/> in a single line.</summary>
internal sealed class Coord2Drawer : TypeDrawer
{
    public  override    string[]    SortFields      => ["x", "y"];
    public  override    string[]    FormatFields    => ["x", "y"];
    public  override    int         DefaultWidth    => 250;

    public override ItemFlags DrawValue(in DrawValue drawValue)
    {
        if (!drawValue.GetValue<Coord2>(out var value, out var exception)) {
            return drawValue.DrawException(exception);
        }
        if (EcUtils.InputInt2(ref value.x, ref value.y, drawValue, out var flags)) {
            drawValue.SetValue(value);
        }
        return flags;
    }
    
    public  override void Format(MemberFormat format) {
        format.GetValue<Coord2>(out var value, out var exception);
        format.Append(value.x, exception);
        format.Append(value.y, exception);
    }
}

/// <summary>Display the components of the 3D point struct <see cref="Coord3"/> in a single line.</summary>
internal sealed class Coord3Drawer : TypeDrawer
{
    public  override    string[]    SortFields      => ["x", "y", "z"];
    public  override    string[]    FormatFields    => ["x", "y", "z"];
    public  override    int         DefaultWidth    => 320;

    public override ItemFlags DrawValue(in DrawValue drawValue)
    {
        if (!drawValue.GetValue<Coord3>(out var value, out var exception)) {
            return drawValue.DrawException(exception);
        }
        if (EcUtils.InputInt3(ref value.x, ref value.y, ref value.z, drawValue, out var flags)) {
            drawValue.SetValue(value);
        }
        return flags;
    }
    
    public  override void Format(MemberFormat format) {
        format.GetValue<Coord3>(out var value, out var exception);
        format.Append(value.x, exception);
        format.Append(value.y, exception);
        format.Append(value.z, exception);
    }
}

/// <summary>Display the components of an <see cref="ColorRGB"/> struct in a single line.</summary>
internal sealed class ColorRGBDrawer : TypeDrawer
{
    public  override    string[]    SortFields      => ["R", "G", "B"];
    public  override    string[]    FormatFields    => ["R", "G", "B"];
    public  override    int         DefaultWidth    => 300;

    public override ItemFlags DrawValue(in DrawValue drawValue)
    {
        if (!drawValue.GetValue<ColorRGB>(out var value, out var exception)) {
            return drawValue.DrawException(exception);
        }
        if (EcUtils.ColorEditRGB(ref value.R, ref value.G, ref value.B, drawValue, out var flags)) {
            drawValue.SetValue(value);
        }
        return flags;
    }
    
    public  override void Format(MemberFormat format) {
        format.GetValue<ColorRGB>(out var value, out var exception);
        format.Append(value.R, exception);
        format.Append(value.G, exception);
        format.Append(value.B, exception);
    }
}

/// <summary>Display the components of the <see cref="ColorRGBA"/> struct in a single line.</summary>
internal sealed class ColorRGBADrawer : TypeDrawer
{
    public  override    string[]    SortFields      => ["R", "G", "B", "A"];
    public  override    string[]    FormatFields    => ["R", "G", "B", "A"];
    public  override    int         DefaultWidth    => 400;

    public override ItemFlags DrawValue(in DrawValue drawValue)
    {
        if (!drawValue.GetValue<ColorRGBA>(out var value, out var exception)) {
            return drawValue.DrawException(exception);
        }
        if (EcUtils.ColorEditRGBA(ref value.R, ref value.G, ref value.B, ref value.A, drawValue, out var flags)) {
            drawValue.SetValue(value);
        }
        return flags;
    }
    
    public  override void Format(MemberFormat format) {
        format.GetValue<ColorRGBA>(out var value, out var exception);
        format.Append(value.R, exception);
        format.Append(value.G, exception);
        format.Append(value.B, exception);
        format.Append(value.A, exception);
    }
}


