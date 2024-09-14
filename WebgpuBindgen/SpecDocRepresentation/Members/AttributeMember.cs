using WebgpuBindgen.SpecDocRepresentation.Types;

namespace WebgpuBindgen.SpecDocRepresentation.Members;

public class AttributeMember : WebidlMemberBase
{
    public required string Name { get; set; }
    public required IdlTypeBase IdlType { get; set; }
    public required object[]? ExtAttrs { get; set; }
    public required string Special { get; set; }
    public required bool Readonly { get; set; }
}
