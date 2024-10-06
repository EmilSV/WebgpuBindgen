using System.Text.Json.Serialization;

namespace WebgpuBindgen.SpecDocRepresentation.Comments;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "tag")]
[JsonDerivedType(typeof(CommentSpecCommentElement), typeDiscriminator: "spec-comment")]
[JsonDerivedType(typeof(CommentTypeLinkElement), typeDiscriminator: "typeLink")]
[JsonDerivedType(typeof(CommentDocLinkElement), typeDiscriminator: "docLink")]
[JsonDerivedType(typeof(CommentNoteElement), typeDiscriminator: "text-note")]
[JsonDerivedType(typeof(CommentAlgorithmElement), typeDiscriminator: "text-algorithm")]
[JsonDerivedType(typeof(CommentExampleElement), typeDiscriminator: "text-example")]
[JsonDerivedType(typeof(CommentWebLinkElement), typeDiscriminator: "webLink")]
[JsonDerivedType(typeof(CommentAbstractOpLinkElement), typeDiscriminator: "abstractOpLink")]
[JsonDerivedType(typeof(CommentTextElement), typeDiscriminator: "text")]
[JsonDerivedType(typeof(CommentDfnElement), typeDiscriminator: "dfn")]
[JsonDerivedType(typeof(CommentValueElement), typeDiscriminator: "value")]
public abstract class ChildCommentItem : CommentElement
{

}