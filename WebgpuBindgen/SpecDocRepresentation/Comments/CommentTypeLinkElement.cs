using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace WebgpuBindgen.SpecDocRepresentation.Comments;

public sealed class CommentTypeLinkElement : ChildCommentItem
{
    public enum TypeLinkType
    {
        [EnumMember(Value = "function")]
        Function,
        [EnumMember(Value = "property")]
        Property,
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required TypeLinkType LinkType { get; set; }
    public required bool IsInternalState { get; set; }
    public required string[] path { get; set; }
    public string? displayText { get; set; }
}