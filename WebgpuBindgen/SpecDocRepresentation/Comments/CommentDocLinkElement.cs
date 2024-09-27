using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace WebgpuBindgen.SpecDocRepresentation.Comments;

public sealed class CommentDocLinkElement : ChildCommentItem
{

    public enum DocLinkType
    {
        [EnumMember(Value = "definition")]
        Definition,
        [EnumMember(Value = "heading")]
        Heading,
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required DocLinkType LinkType { get; set; }
    public required string[] Path { get; set; }
    public string? DisplayText { get; set; }
}