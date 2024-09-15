using WebgpuBindgen.SpecDocRepresentation.Types;

namespace WebgpuBindgen.SpecDocRepresentation.Members;

public class ConstMember : WebidlMemberBase
{
    public class ConstValue
    {
        public required string Type { get; set; }
        public required string Value { get; set; }
    }

    public required string Name { get; set; }
    public required IdlTypeBase IdlType { get; set; }
    public required ExtendedAttribute[] ExtAttrs { get; set; }
    public required ConstValue Value { get; set; }
}