namespace WebgpuBindgen.XmlComments;

public sealed record CommentElement
{
    public required GroupElement Parent { get; init; }
    public int? Priority { get; init; }
    public required string? ApplyToLocation { get; init; }
    public string? CloneFromLocation { get; init; }
}