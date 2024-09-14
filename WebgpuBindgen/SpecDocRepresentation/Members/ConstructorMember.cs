namespace WebgpuBindgen.SpecDocRepresentation.Members;

public class ConstructorMember : WebidlMemberBase
{
    public required Argument[] Arguments { get; set; }
    public required object[] ExtAttrs { get; set; }
}