using System.Text.Json.Serialization;
using WebgpuBindgen.SpecDocRepresentation.Types;

namespace WebgpuBindgen.SpecDocRepresentation.Members;

public class SetlikeMember : WebidlMemberBase
{
    public required IdlTypeBase IdlType { get; set; }
    public required bool ReadOnly { get; set; }
    public required object[]? Arguments { get; set; }
    public required bool Readonly { get; set; }
    public required bool Async { get; set; }
}
