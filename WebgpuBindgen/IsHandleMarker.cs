namespace WebgpuBindgen;

public sealed class IsHandleMarker
{
    private IsHandleMarker() { }
    public readonly static IsHandleMarker Instance = new();
}