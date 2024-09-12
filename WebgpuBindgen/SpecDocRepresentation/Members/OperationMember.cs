namespace WebgpuBindgen.SpecDocRepresentation.Members;

public class OperationMember : WebidlMember
{
    public IdlType IdlType { get; set; }
    public Argument[] Arguments { get; set; }
}