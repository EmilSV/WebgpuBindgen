namespace WebgpuBindgen.SpecDocRepresentation.Comments;

public sealed class CommentParamElement : CommentElement
{
    public class ParamItems
    {
        public required string Name { get; set; }
        public required ChildCommentItem[] Description { get; set; }
    }

    public required ParamItems[] Items { get; set; }
}