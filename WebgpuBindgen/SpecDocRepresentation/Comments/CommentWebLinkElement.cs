namespace WebgpuBindgen.SpecDocRepresentation.Comments;

public sealed class CommentWebLinkElement : ChildCommentItem
{
    public required string Url { get; set; }
    public required string DisplayText { get; set; }
}