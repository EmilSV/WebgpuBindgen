namespace WebgpuBindgen.SpecDocRepresentation.Comments;

public sealed class CommentDocLinkElement : ChildCommentItem
{
    public enum DocLinkType
    {
        Definition,
        Heading,
    }

    public required DocLinkType LinkType;
    public required string[] path;
    public string? displayText;
}