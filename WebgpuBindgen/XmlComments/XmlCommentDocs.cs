namespace WebgpuBindgen.XmlComments;

public sealed class XmlCommentDocs
{
    private readonly List<SubElementBase> subElements;

    private XmlCommentDocs(List<SubElementBase> subElements)
    {
        this.subElements = subElements;
    }

    public static async Task<XmlCommentDocs> Create(string folderPath)
    {
        var xmlFiles = Directory.GetFiles(folderPath, "*.xmlc", SearchOption.AllDirectories)
       .Select(i => XmlCommentParser.Parse(File.OpenRead(i)))
       .ToList();

        var loadedXmlFiles = await Task.WhenAll(xmlFiles).ConfigureAwait(false);
        var finalXmlFiles = loadedXmlFiles.SelectMany(i => i).ToList();

        finalXmlFiles.Sort((a, b) => b.Priority.CompareTo(a.Priority));

        return new XmlCommentDocs(finalXmlFiles);
    }

    public void AssignComment(TranslationResult translationResult)
    {
        foreach (var subCommentElement in subElements)
        {
            subCommentElement.AssignComment(translationResult);
        }
    }
}