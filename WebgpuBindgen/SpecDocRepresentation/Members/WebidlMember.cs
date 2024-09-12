using WebgpuBindgen.SpecDocRepresentation.Comments;

namespace WebgpuBindgen.SpecDocRepresentation;

public abstract class WebidlMember
{
    public required string Type { get; set; }
    public required string Name { get; set; }
    public required string Special { get; set; }
    public  CommentElement[]? Comment { get; set; }
    public required bool Partial { get; set; }
}