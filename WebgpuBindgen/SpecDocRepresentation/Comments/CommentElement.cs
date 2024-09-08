using System.Text.Json.Serialization;

namespace WebgpuBindgen.SpecDocRepresentation.Comments;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "tag")]
[JsonDerivedType(typeof(CommentTextElement), typeDiscriminator: "text")]
[JsonDerivedType(typeof(CommentParamElement), typeDiscriminator: "param")]
[JsonDerivedType(typeof(CommentSpecCommentElement), typeDiscriminator: "spec-comment")]
[JsonDerivedType(typeof(CommentTypeLinkElement), typeDiscriminator: "typeLink")]
[JsonDerivedType(typeof(CommentDocLinkElement), typeDiscriminator: "docLink")]
[JsonDerivedType(typeof(CommentNoteElement), typeDiscriminator: "text-note")]
[JsonDerivedType(typeof(CommentAlgorithmElement), typeDiscriminator: "text-algorithm")]
[JsonDerivedType(typeof(CommentExampleElement), typeDiscriminator: "text-example")]
[JsonDerivedType(typeof(CommentWebLinkElement), typeDiscriminator: "webLink")]
[JsonDerivedType(typeof(CommentAbstractOpLinkElement), typeDiscriminator: "abstractOpLink")]
[JsonDerivedType(typeof(CommentDfnElement), typeDiscriminator: "dfn")]
public abstract class CommentElement
{
    public required string Tag;
}