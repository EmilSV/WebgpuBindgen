namespace WebgpuBindgen.SpecDocRepresentation.Comments;

public sealed class CommentDfnElement : ChildCommentItem
{
    public string? Type;
    public required string Text;
    public required string For;
}