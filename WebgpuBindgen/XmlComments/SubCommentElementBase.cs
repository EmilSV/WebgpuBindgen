namespace WebgpuBindgen.XmlComments;

public abstract record SubCommentElementBase : SubElementBase
{
    public required string ApplyToLocation { get; init; }
    public string? Description { get; init; }
    public string? CloneFromLocation { get; init; }
}