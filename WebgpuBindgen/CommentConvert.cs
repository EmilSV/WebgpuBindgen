using CapiGenerator.CSModel;
using CapiGenerator.CSModel.Comments;
using WebgpuBindgen.SpecDocRepresentation.Comments;
using WebgpuBindgen.SpecDocRepresentation.Members;
using WebgpuBindgen.SpecDocRepresentation.Types;

namespace WebgpuBindgen;


public class CommentConvert(CsTypeLookup csTypeLookup)
{
    public CommentSummery Convert(CommentElement[] items)
    {
        var param = items.OfType<CommentParamElement>().ToList();
        var noParam = items.Where(i => !param.Contains(i)).ToArray();

        return new()
        {
            SummaryText = string.Join("", noParam.Select(i => Convert(i))),
            Params = param.SelectMany(i =>
            {
                var values = Convert(i);
                return values.Select(j => new CommentParameter()
                {
                    Name = j.name,
                    Description = j.value,
                });
            }).ToList()
        };
    }


    string Convert(CommentElement item)
    {
        return item switch
        {
            CommentTextElement text => Convert(text),
            CommentSpecCommentElement specComment => Convert(specComment),
            CommentTypeLinkElement typeLink => Convert(typeLink),
            CommentDocLinkElement docLink => Convert(docLink),
            CommentNoteElement note => Convert(note),
            CommentAlgorithmElement algorithm => Convert(algorithm),
            CommentExampleElement example => Convert(example),
            CommentWebLinkElement webLink => Convert(webLink),
            CommentAbstractOpLinkElement abstractOpLink => Convert(abstractOpLink),
            CommentDfnElement dfn => Convert(dfn),
            _ => "",
        };
    }


    string Convert(CommentDfnElement item)
    {
        return item.Text;
    }

    string Convert(CommentAbstractOpLinkElement item)
    {
        return item.DisplayText ?? item.Text;
    }

    string Convert(CommentTextElement item)
    {
        if (item.Items == null)
        {
            return "";
        }

        return string.Join("", item.Items);
    }

    (string name, string value)[] Convert(CommentParamElement item)
    {
        var list = new List<(string name, string value)>();
        foreach (var param in item.Items)
        {
            string name = param.Name;
            string value = string.Join("", param.Description.Select(i => Convert(i)));
            list.Add((name, value));
        }

        return [.. list];
    }

    string Convert(CommentSpecCommentElement item)
    {
        return "";
    }

    string Convert(CommentTypeLinkElement item)
    {
        if (item.path.Length < 1)
        {
            return item.displayText ?? "";
        }

        var typeName = ToWebgpuSharpName(item.path[0]);
        BaseCSType? csType = csTypeLookup.FindType(typeName);

        if (csType == null)
        {
            return string.Join(".", item.path.Select(ToWebgpuSharpNameFromTypeLink));
        }

        string cref;
        if (csType.Namespace != null)
        {
            cref = string.Join(".", [csType.Namespace, .. item.path.Select(ToWebgpuSharpNameFromTypeLink)]);
        }
        else
        {
            cref = string.Join(".", item.path.Select(ToWebgpuSharpNameFromTypeLink));
        }

        if (item.displayText != null)
        {
            return $""" <see cref="{cref}">{item.displayText}</see>""";
        }
        else
        {
            return $""" <see cref="{cref}"/>""";
        }
    }

    string Convert(CommentDocLinkElement item)
    {
        if (item.DisplayText != null)
        {
            return item.DisplayText;
        }

        return string.Join(".", item.Path);
    }

    string Convert(CommentNoteElement item)
    {
        var text = string.Join("", item.Items.Select(i => Convert(i)));
        return
        $"""
        <remarks>
        {text}
        </remarks>
        """;
    }

    string Convert(CommentAlgorithmElement item)
    {
        return "";
    }

    string Convert(CommentExampleElement item)
    {
        return "";
    }

    string Convert(CommentWebLinkElement item)
    {
        if (item.DisplayText != null)
        {
            return $" <a href=\"{item.Url}\">{item.DisplayText}</a>";
        }
        else
        {
            return $" <a href=\"{item.Url}\"/>";
        }
    }



    static string ToWebgpuSharpName(string name)
    {
        if (name.StartsWith("GPU"))
        {
            name = name[3..];
        }

        if (name.EndsWith("Dict"))
        {
            name = name[0..^"Dict".Length];
        }

        if (name.Length > 1 && char.IsLower(name[0]))
        {
            name = char.ToUpper(name[0]) + name[1..];
        }

        return name;
    }

    static string ToWebgpuSharpNameFromTypeLink(string name)
    {
        if (name.StartsWith("GPU"))
        {
            name = name[3..];
        }

        if (name.EndsWith("Dict"))
        {
            name = name[0..^"Dict".Length];
        }

        if (name.Length > 1 && char.IsLower(name[0]))
        {
            name = char.ToUpper(name[0]) + name[1..];
        }

        if (name.EndsWith("()"))
        {
            name = name[0..^"()".Length];
        }

        return name;
    }

}