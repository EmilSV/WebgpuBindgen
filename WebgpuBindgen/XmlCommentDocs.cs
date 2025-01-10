using System.Xml;
using System.Linq;
using CapiGenerator.CSModel.Comments;
using CapiGenerator.CSModel;

namespace WebgpuBindgen;

public sealed class XmlCommentDocs
{
    private readonly Dictionary<string, DocComment> xmlDocs = new();

    private XmlCommentDocs(Dictionary<string, DocComment> xmlDocs)
    {
        this.xmlDocs = xmlDocs;
    }

    public static async Task<XmlCommentDocs> Create(string folderPath)
    {
        var xmlDocs = new Dictionary<string, DocComment>();
        foreach (var file in Directory.GetFiles(folderPath, "*.xml", SearchOption.AllDirectories))
        {
            XmlDocument doc = new();
            doc.Load(file);
            LoadDoc(doc, xmlDocs);
        }

        return new XmlCommentDocs(xmlDocs);
    }

    private static void LoadDoc(XmlDocument doc, Dictionary<string, DocComment> xmlDocs)
    {
        foreach (XmlElement comment in doc.GetElementsByTagName("Comment"))
        {
            var location = comment.GetAttribute("location");
            if (location == null)
            {
                Console.Error.WriteLine("Location attribute not found in " + comment.OuterXml);
                continue;
            }

            if (xmlDocs.ContainsKey(location))
            {
                Console.Error.WriteLine("Duplicate location found in " + location);
                continue;
            }


            List<CommentParameter> parameters = new();
            List<CommentRemarks> remarks = new();

            foreach (XmlElement paramItem in comment.GetElementsByTagName("param"))
            {
                var name = paramItem.GetAttribute("name");
                var description = paramItem.InnerText;
                parameters.Add(new CommentParameter()
                {
                    Name = name,
                    Description = description
                });
            }

            foreach (XmlElement remarkItem in comment.GetElementsByTagName("remarks"))
            {
                var description = remarkItem.InnerText;
                remarks.Add(new CommentRemarks()
                {
                    Description = description
                });
            }

            var summaryElements = comment.GetElementsByTagName("Summary");
            if (summaryElements.Count > 1)
            {
                Console.Error.WriteLine($"Multiple summary elements found in {location}");
                continue;
            }

            var returnsElements = comment.GetElementsByTagName("returns");
            if (returnsElements.Count > 1)
            {
                Console.Error.WriteLine($"Multiple returns elements found in {location}");
                continue;
            }

            var valueElements = comment.GetElementsByTagName("value");
            if (valueElements.Count > 1)
            {
                Console.Error.WriteLine($"Multiple value elements found in {location}");
                continue;
            }

            var summary = summaryElements.Count == 1 ? summaryElements[0]?.InnerText : null;
            var returns = returnsElements.Count == 1 ? returnsElements[0]?.InnerText : null;
            var value = valueElements.Count == 1 ? valueElements[0]?.InnerText : null;

            xmlDocs.Add(location, new DocComment()
            {
                Summary = new CommentSummery()
                {
                    Description = summary
                },
                Value = new CommentValue()
                {
                    Description = value
                },
                Parameters = parameters,
                Remarks = remarks,
                Return = new CommentReturn()
                {
                    Description = returns
                }
            });
        }
    }

    public void AssignComment(CSEnum enumType)
    {
        {
            var typeFullName = enumType.GetFullName();
            if (xmlDocs.TryGetValue(typeFullName, out var doc))
            {
                enumType.Comments = doc;
            }
        }

        foreach (var field in enumType.Values)
        {
            var fullName = field.GetFullName();
            if (xmlDocs.TryGetValue(fullName, out var doc))
            {
                field.Comments = doc;
            }
        }
    }

    public void AssignComment(CSStruct structType)
    {
        {
            var typeFullName = structType.GetFullName();
            if (xmlDocs.TryGetValue(typeFullName, out var doc))
            {
                structType.Comments = doc;
            }
        }

        foreach (var field in structType.Fields)
        {
            var fullName = field.GetFullName();
            if (xmlDocs.TryGetValue(fullName, out var doc))
            {
                field.Comments = doc;
            }
        }

        foreach (var method in structType.Methods)
        {
            var fullName = method.GetFullName();
            if (xmlDocs.TryGetValue(fullName, out var doc))
            {
                method.Comments = doc;
            }
        }

        // foreach (var constructor in structType.Constructors)
        // {
        //     var fullName = constructor.
        // }
    }

    public void AssignComment(CSStaticClass staticClassType)
    {
        {
            var typeFullName = staticClassType.GetFullName();
            if (xmlDocs.TryGetValue(typeFullName, out var doc))
            {
                staticClassType.Comments = doc;
            }
        }

        foreach (var field in staticClassType.Fields)
        {
            var fullName = field.GetFullName();
            if (xmlDocs.TryGetValue(fullName, out var doc))
            {
                field.Comments = doc;
            }
        }

        foreach (var method in staticClassType.Methods)
        {
            var fullName = method.GetFullName();
            if (xmlDocs.TryGetValue(fullName, out var doc))
            {
                method.Comments = doc;
            }
        }
    }
}