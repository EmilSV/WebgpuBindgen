namespace WebgpuBindgen.SpecDocRepresentation.Comments;

public sealed class CommentDfnElement : ChildCommentItem
{
    public string? Type { get; set; }
    public required string Text { get; set; }
    public required string For { get; set; }
}