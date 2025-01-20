using CapiGenerator.CSModel.Comments;
using CapiGenerator.CSModel;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace WebgpuBindgen;

public sealed partial class XmlCommentDocs
{
    private readonly Dictionary<string, DocComment> xmlDocs = new();
    private readonly Dictionary<string, CommentClone> cloneFromDocsPreStage = new();
    private readonly Dictionary<string, CommentClone> cloneFromDocsPostStage = new();

    private XmlCommentDocs(
        Dictionary<string, DocComment> xmlDocs,
        Dictionary<string, CommentClone> cloneFromDocsPreStage,
        Dictionary<string, CommentClone> cloneFromDocsPostStage)
    {
        this.xmlDocs = xmlDocs;
        this.cloneFromDocsPreStage = cloneFromDocsPreStage;
        this.cloneFromDocsPostStage = cloneFromDocsPostStage;
    }

    public static async Task<XmlCommentDocs> Create(string folderPath)
    {
        var xmlDocs = new Dictionary<string, DocComment>();
        var cloneFromDocsPreStage = new Dictionary<string, CommentClone>();
        var cloneFromDocsPostStage = new Dictionary<string, CommentClone>();
        var xmlFiles = Directory.GetFiles(folderPath, "*.xml", SearchOption.AllDirectories)
        .Select(i => XDocument.LoadAsync(File.OpenRead(i), LoadOptions.None, CancellationToken.None))
        .ToList();

        var loadedXmlFiles = await Task.WhenAll(xmlFiles).ConfigureAwait(false);

        foreach (var doc in loadedXmlFiles)
        {
            LoadDoc(doc, xmlDocs, cloneFromDocsPreStage, cloneFromDocsPostStage);
        }

        return new XmlCommentDocs(xmlDocs, cloneFromDocsPreStage, cloneFromDocsPostStage);
    }

    private static void LoadDoc(
        XDocument doc,
        Dictionary<string, DocComment> xmlDocs,
        Dictionary<string, CommentClone> cloneFromDocsPreStage,
        Dictionary<string, CommentClone> cloneFromDocsPostStage)
    {
        foreach (XElement comment in doc.XPathSelectElements("//Comment|//comment"))
        {
            try
            {
                var location = comment.Attribute("location")?.Value;
                if (location == null)
                {
                    Console.Error.WriteLine($"Location attribute not found in {ReadInnerXml(comment)}");
                    continue;
                }

                location = RemoveWhitespace(location);

                if (xmlDocs.ContainsKey(location))
                {
                    Console.Error.WriteLine($"Duplicate location found in {location}");
                    continue;
                }

                // var clones = comment.XPathSelectElements(".//Clone|.//clone")
                //     .Select(i => ToCommentClone(i, location)).ToList();

                // foreach (var preStageClone in clones.Where(i => i.Stage == CommentClone.StageType.Pre))
                // {
                //     cloneFromDocsPreStage.Add(preStageClone.CloneFromLocation, preStageClone);
                // }

                // foreach (var postStageClone in clones.Where(i => i.Stage == CommentClone.StageType.Post))
                // {
                //     cloneFromDocsPostStage.Add(postStageClone.CloneFromLocation, postStageClone);
                // }

                List<CommentParameter> parameters = new();
                List<CommentRemarks> remarks = new();

                foreach (XElement paramItem in comment.XPathSelectElements(".//Param|.//param"))
                {
                    var name = paramItem.Attribute("name")?.Value;
                    var description = TrimXml(ReadInnerXml(paramItem));
                    parameters.Add(new CommentParameter()
                    {
                        Name = name!,
                        Description = description
                    });
                }

                foreach (XElement remarkItem in comment.XPathSelectElements(".//Remarks|.//remarks"))
                {
                    var description = TrimXml(ReadInnerXml(remarkItem));
                    remarks.Add(new CommentRemarks()
                    {
                        Description = description
                    });
                }

                var summaryElements = comment.XPathSelectElements(".//Summary|.//summary").SingleOrDefault();
                var returnsElements = comment.XPathSelectElements(".//Returns|.//returns").SingleOrDefault();
                var valueElements = comment.XPathSelectElements(".//Value|.//value").SingleOrDefault();

                var summary = summaryElements == null ? null : TrimXml(ReadInnerXml(summaryElements));
                var returns = returnsElements == null ? null : TrimXml(ReadInnerXml(returnsElements));
                var value = valueElements == null ? null : TrimXml(ReadInnerXml(valueElements));

                xmlDocs.Add(location, new DocComment()
                {
                    Summary = summary == null ? null : new CommentSummery()
                    {
                        Description = summary
                    },
                    Value = value == null ? null : new CommentValue()
                    {
                        Description = value
                    },
                    Parameters = parameters,
                    Remarks = remarks,
                    Return = returns == null ? null : new CommentReturn()
                    {
                        Description = returns
                    }
                });
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"Error parsing {ReadInnerXml(comment)}");
                Console.Error.WriteLine(e);
            }
        }
    }

    private static CommentClone ToCommentClone(XElement element, string applyToLocation) => new()
    {
        ApplyToLocation = applyToLocation,
        CloneSummary = bool.TryParse(element.Attribute("CloneSummery")?.Value, out var cloneSummary) && cloneSummary,
        CloneValue = bool.TryParse(element.Attribute("CloneValue")?.Value, out var cloneValue) && cloneValue,
        CloneFromLocation = element.Attribute("location")?.Value,
        CloneRemarks = bool.TryParse(element.Attribute("CloneRemarks")?.Value, out var cloneRemarks) && cloneRemarks,
        CloneReturn = bool.TryParse(element.Attribute("CloneReturn")?.Value, out var cloneReturn) && cloneReturn,
        CloneParameters = element.XPathSelectElements(".//ParameterClone|.//parameterClone").Select(i => i.Attribute("name")?.Value).ToList()!,
        Stage = element.Attribute("stage")?.Value.ToLower() switch
        {
            "pre" => CommentClone.StageType.Pre,
            "post" => CommentClone.StageType.Post,
            _ => throw new Exception("Unknown stage type")
        }
    };

    public void AssignComment(List<CSEnum> enums, List<CSStruct> structs, List<CSStaticClass> staticClasses)
    {
        foreach (var enumType in enums)
        {
            AssignComment(enumType);
        }
        foreach (var structType in structs)
        {
            AssignComment(structType);
        }
        foreach (var staticClassType in staticClasses)
        {
            AssignComment(staticClassType);
        }
    }

    public void AssignComment(CSEnum enumType)
    {
        {
            var typeFullName = RemoveWhitespace(enumType.GetFullName());
            if (xmlDocs.TryGetValue(typeFullName, out var doc))
            {
                enumType.Comments = GetNewComment(enumType.Comments, doc);
            }
        }

        foreach (var field in enumType.Values)
        {
            var fullName = RemoveWhitespace(field.GetFullName());
            if (xmlDocs.TryGetValue(fullName, out var doc))
            {
                field.Comments = GetNewComment(field.Comments, doc);
            }
        }
    }

    public void AssignComment(CSStruct structType)
    {
        if (structType.Name == "AdapterHandle")
        {
            Debugger.Break();
        }

        {
            var typeFullName = RemoveWhitespace(structType.GetFullName());
            if (xmlDocs.TryGetValue(typeFullName, out var doc))
            {
                structType.Comments = GetNewComment(structType.Comments, doc);
            }
        }

        foreach (var field in structType.Fields)
        {
            var fullName = RemoveWhitespace(field.GetFullName());
            if (xmlDocs.TryGetValue(fullName, out var doc))
            {
                field.Comments = GetNewComment(field.Comments, doc);
            }
        }

        foreach (var method in structType.Methods)
        {
            var fullName = RemoveWhitespace(method.GetFullName());
            var fullNameWithParameters = RemoveWhitespace(method.GetFullNameWithParameters());
            if (xmlDocs.TryGetValue(fullName, out var doc1))
            {
                method.Comments = GetNewComment(method.Comments, doc1);
            }
            else if (fullNameWithParameters != null && xmlDocs.TryGetValue(method.GetFullNameWithParameters()!, out var doc2))
            {
                method.Comments = GetNewComment(method.Comments, doc2);
            }
        }

        foreach (var constructor in structType.Constructors)
        {
            var fullName = RemoveWhitespace(constructor.GetFullName());
            var fullNameWithParameters = RemoveWhitespace(constructor.GetFullNameWithParameters());
            if (xmlDocs.TryGetValue(fullName, out var doc1))
            {
                constructor.Comments = GetNewComment(constructor.Comments, doc1);
            }
            else if (fullNameWithParameters != null && xmlDocs.TryGetValue(fullNameWithParameters, out var doc2))
            {
                constructor.Comments = GetNewComment(constructor.Comments, doc2);
            }
        }
    }

    public void AssignComment(CSStaticClass staticClassType)
    {
        {
            var typeFullName = RemoveWhitespace(staticClassType.GetFullName());
            if (xmlDocs.TryGetValue(typeFullName, out var doc))
            {
                staticClassType.Comments = GetNewComment(staticClassType.Comments, doc);
            }
        }

        foreach (var field in staticClassType.Fields)
        {
            var fullName = RemoveWhitespace(field.GetFullName());
            if (xmlDocs.TryGetValue(fullName, out var doc))
            {
                field.Comments = GetNewComment(field.Comments, doc);
            }
        }

        foreach (var method in staticClassType.Methods)
        {
            var fullName = RemoveWhitespace(method.GetFullName());
            var fullNameWithParameters = RemoveWhitespace(method.GetFullNameWithParameters());
            if (xmlDocs.TryGetValue(fullName, out var doc))
            {
                method.Comments = GetNewComment(method.Comments, doc);
            }
            else if (fullNameWithParameters != null && xmlDocs.TryGetValue(fullNameWithParameters, out var doc2))
            {
                method.Comments = GetNewComment(method.Comments, doc2);
            }
        }
    }

    private static DocComment GetNewComment(DocComment? currentDoc, DocComment newDoc)
    {
        if (currentDoc == null)
        {
            currentDoc = newDoc;
        }
        else
        {
            currentDoc.Summary = newDoc.Summary ?? currentDoc.Summary;
            currentDoc.Value = newDoc.Value ?? currentDoc.Value;

            currentDoc.Parameters.Clear();
            currentDoc.Parameters.AddRange(newDoc.Parameters ?? Enumerable.Empty<CommentParameter>());

            currentDoc.Remarks.Clear();
            currentDoc.Remarks.AddRange(newDoc.Remarks ?? Enumerable.Empty<CommentRemarks>());

            currentDoc.Return = newDoc.Return ?? currentDoc.Return;
        }

        return currentDoc;
    }

    private static string ReadInnerXml(XElement element)
    {
        var reader = element.CreateReader();
        reader.MoveToContent();

        return reader.ReadInnerXml();
    }
    [return: NotNullIfNotNull("xml")]
    private static string? TrimXml(string? xml)
    {
        if (xml == null)
        {
            return null;
        }

        xml = xml.Trim();
        //Remove double space or more with single space
        xml = MyRegex().Replace(xml, " ");

        xml = string.Join("\n", xml.Split("\n").Select(i =>
        {
            var trimmed = i.Trim();
            if (trimmed.Length == 0)
            {
                return "";
            }
            return trimmed;
        }));
        return xml;
    }

    [return: NotNullIfNotNull("str")]
    private static string? RemoveWhitespace(string? str)
    {
        if (str == null)
        {
            return str;
        }

        var RegexWhitespace = WhiteSpaceRegex();
        return RegexWhitespace.Replace(str, " ");
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhiteSpaceRegex();
    [GeneratedRegex(@"  +")]
    private static partial Regex MyRegex();
}