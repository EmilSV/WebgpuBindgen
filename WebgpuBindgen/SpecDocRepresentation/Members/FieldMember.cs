using System.Text.Json.Serialization;
using WebgpuBindgen.SpecDocRepresentation.Defaults;
using WebgpuBindgen.SpecDocRepresentation.Types;

namespace WebgpuBindgen.SpecDocRepresentation.Members;

public class FieldMember : WebidlMemberBase
{
    public required string Name { get; set; }
    public required object[] ExtAttrs { get; set; }
    public required IdlTypeBase IdlType { get; set; }
    public DefaultValueBase? Default { get; set; }
    public required bool Required { get; set; }
}
