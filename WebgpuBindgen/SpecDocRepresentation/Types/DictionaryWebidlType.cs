using WebgpuBindgen.SpecDocRepresentation.Members;

namespace WebgpuBindgen.SpecDocRepresentation.Types;

public class DictionaryWebidlType : RootWebidlTypeBase
{
    public string? Inheritance { get; set; }
    public required WebidlMemberBase[] Members { get; set; }
    public required ExtendedAttribute[] ExtAttrs { get; set; }
    public required bool Partial { get; set; }
}