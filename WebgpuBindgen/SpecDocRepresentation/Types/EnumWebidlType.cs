using WebgpuBindgen.SpecDocRepresentation.Comments;

namespace WebgpuBindgen.SpecDocRepresentation.Types;

public class EnumWebidlType : RootWebidlTypeBase
{
    public sealed class EnumValue
    {
        public required string Type { get; set; }
        public required string Value { get; set; }
        public CommentElement[]? Comment { get; set; }
    }

    public required EnumValue[] Values { get; set; }
    public required ExtendedAttribute[] ExtAttrs { get; set; }
}