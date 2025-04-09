using System.Diagnostics;
using CapiGenerator.CSModel.Comments;

namespace WebgpuBindgen.XmlComments;

public record InheritElement : SubElementBase
{
    public required string ApplyToLocation { get; init; }
    public required string InheritFrom { get; init; }

    public override void AssignComment(TranslationResult translationResult)
    {
        var itemInheritingFrom = XmlCommentFinder.FindComments(translationResult, InheritFrom);
        if (itemInheritingFrom == null)
        {
            return;
        }

        var itemToAssignTo = XmlCommentFinder.FindComments(translationResult, ApplyToLocation);

        var commentsToInherit = itemInheritingFrom.Comments;
        if (commentsToInherit == null || itemToAssignTo == null)
        {
            return;
        }

        itemToAssignTo.Comments ??= new DocComment();
        itemToAssignTo.Comments.Summary ??= new CommentSummery();
        itemToAssignTo.Comments.Summary.Description = commentsToInherit.Summary?.Description;

        foreach (var existingParam in itemToAssignTo.Comments.Parameters)
        {
            var overridingParam = commentsToInherit.Parameters.FirstOrDefault(i => i.Name == existingParam.Name);
            if (overridingParam != null)
            {
                existingParam.Description = overridingParam.Description;
            }
        }

        foreach (var newParam in commentsToInherit.Parameters)
        {
            if (itemToAssignTo.Comments.Parameters.All(i => i.Name != newParam.Name))
            {
                itemToAssignTo.Comments.Parameters.Add(newParam);
            }
        }

        itemToAssignTo.Comments.Remarks.AddRange(commentsToInherit.Remarks);
        itemToAssignTo.Comments.Return ??= new CommentReturn();
        itemToAssignTo.Comments.Return.Description = commentsToInherit.Return?.Description;

        itemToAssignTo.Comments.Value ??= new CommentValue();
        itemToAssignTo.Comments.Value.Description = commentsToInherit.Value?.Description;
    }
}
