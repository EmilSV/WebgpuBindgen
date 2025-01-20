using CapiGenerator.CSModel.Comments;

namespace WebgpuBindgen.XmlComments;

public sealed record RemarkElement : SubCommentElementBase
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
            throw new NotImplementedException();
        }

        item.Comments ??= new DocComment();
        item.Comments.Remarks.Add(new()
        {
            Description = description
        });
    }
}