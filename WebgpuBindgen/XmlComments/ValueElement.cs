using CapiGenerator.CSModel.Comments;

namespace WebgpuBindgen.XmlComments;

public sealed record ValueElement : SubCommentElementBase
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
            var newDescription = XmlCommentFinder.FindComments(translationResult, CloneFromLocation)
                ?.Comments?.Value?.Description;

            if (newDescription != null)
            {
                description = newDescription;
            }
        }

        if (string.IsNullOrEmpty(description))
        {
            if (item.Comments?.Value == null)
            {
                return;
            }
            item.Comments.Value = null;
            return;
        }

        item.Comments ??= new DocComment();
        item.Comments.Value ??= new CommentValue();
        item.Comments.Value.Description = description;
    }
}