namespace WebgpuBindgen.SpecDocRepresentation.Comments;

public sealed class CommentTypeLinkElement : ChildCommentItem
{
    public enum TypeLinkType
    {
        Function,
        Property,
    }

    public required TypeLinkType LinkType;
    public required bool IsInternalState;
    public required string[] path;
    public string? displayText;
}