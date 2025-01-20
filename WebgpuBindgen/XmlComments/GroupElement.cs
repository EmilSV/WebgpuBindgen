namespace WebgpuBindgen.XmlComments;

public sealed record GroupElement
{
    public GroupElement? Parent { get; init; }
    public required string Prefix { get; init; }
}