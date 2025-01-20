using CapiGenerator.CSModel.Comments;

namespace WebgpuBindgen.XmlComments;

public sealed record SummaryElement : SubCommentElementBase
{
    public override void AssignComment(TranslationResult translationResult)
    {
        var item = XmlCommentFinder.FindComments(translationResult, ApplyToLocation);
        if (item == null)
        {
            return;
        }

        string? description = Description;

        if (CloneFromLocation != null)
        {
            var newDescription = XmlCommentFinder.FindComments(translationResult, CloneFromLocation)?.Comments?.Summary?.Description;
            if (newDescription != null)
            {
                description = newDescription;
            }
        }

        item.Comments ??= new DocComment();
        item.Comments.Summary ??= new CommentSummery();
        item.Comments.Summary.Description = description;
    }
}