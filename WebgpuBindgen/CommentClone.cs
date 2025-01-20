namespace WebgpuBindgen;

public class CommentClone
{
    public enum StageType
    {
        Pre,
        Post
    }

    public required string ApplyToLocation { get; set; }
    public required string CloneFromLocation { get; set; }
    public required bool CloneSummary { get; set; }
    public required bool CloneValue { get; set; }
    public required List<string>? CloneParameters { get; set; }
    public required bool CloneRemarks { get; set; }
    public required bool CloneReturn { get; set; }
    public required StageType Stage { get; set; }
}