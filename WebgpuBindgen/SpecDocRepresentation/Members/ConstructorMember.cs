namespace WebgpuBindgen.SpecDocRepresentation.Members;

public class ConstructorMember : WebidlMemberBase
{
    public required Argument[] Arguments { get; set; }
    public required ExtendedAttribute[] ExtAttrs { get; set; }
}