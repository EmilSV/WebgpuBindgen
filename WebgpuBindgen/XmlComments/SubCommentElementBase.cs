namespace WebgpuBindgen.XmlComments;

public abstract record SubCommentElementBase
{
    public required int Priority { get; init; }
    public required string ApplyToLocation { get; init; }
    public string? Description { get; init; }
    public string? CloneFromLocation { get; init; }

    public abstract void AssignComment(TranslationResult translationResult);
}