namespace WebgpuBindgen.SpecDocRepresentation.Members;

public class OperationMember : WebidlMember
{
    public IdlType IdlType { get; set; }
    public object[] Arguments { get; set; }
}