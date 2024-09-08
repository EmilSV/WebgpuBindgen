namespace WebgpuBindgen.SpecDocRepresentation.Comments;

public sealed class CommentParamElement : CommentElement
{
    public class ParamItems
    {
        public required string Name;
        public required ChildCommentItem[] description;
    }

    public required ParamItems[] Items;
}