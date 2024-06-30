namespace WebgpuBindgen;

public sealed class IsFFITypeMarker
{
    private IsFFITypeMarker() { }
    public static readonly IsFFITypeMarker Instance = new();
}