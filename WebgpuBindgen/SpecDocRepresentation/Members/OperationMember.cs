using WebgpuBindgen.SpecDocRepresentation.Comments;
using WebgpuBindgen.SpecDocRepresentation.Types;

namespace WebgpuBindgen.SpecDocRepresentation.Members;

public class OperationMember : WebidlMemberBase
{
    public required string Name { get; set; }
    public required IdlTypeBase IdlType { get; set; }
    public required Argument[] Arguments { get; set; }
    public required ExtendedAttribute[] ExtAttrs { get; set; }
    public required string Special { get; set; }
}