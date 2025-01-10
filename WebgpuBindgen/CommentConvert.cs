using CapiGenerator.CModel;
using CapiGenerator.CSModel;
using CapiGenerator.CSModel.Comments;
using WebgpuBindgen.SpecDocRepresentation.Comments;
using WebgpuBindgen.SpecDocRepresentation.Members;
using WebgpuBindgen.SpecDocRepresentation.Types;

namespace WebgpuBindgen;


public class CommentConvert(CsTypeLookup csTypeLookup)
{
    public DocComment Convert(CommentElement[] items, BaseCSAstItem member)
    {
        var param = items.OfType<CommentParamElement>().ToList();
        var noParam = items.Where(i => !param.Contains(i)).ToArray();

        return new()
        {
            Summary = new() { Description = string.Join("", noParam.Select(i => Convert(i, member))) },
            Parameters = param.SelectMany(i =>
            {
                var values = Convert(i, member);
                return values.Select(j => new CommentParameter()
                {
                    Name = j.name,
                    Description = j.value,
                });
            }).ToList()
        };
    }


    string Convert(CommentElement item, BaseCSAstItem member) => item switch
    {
        CommentTextElement text => Convert(text),
        CommentSpecCommentElement specComment => Convert(specComment),
        CommentTypeLinkElement typeLink => Convert(typeLink),
        CommentDocLinkElement docLink => Convert(docLink),
        CommentNoteElement note => Convert(note, member),
        CommentAlgorithmElement algorithm => Convert(algorithm),
        CommentExampleElement example => Convert(example),
        CommentWebLinkElement webLink => Convert(webLink),
        CommentAbstractOpLinkElement abstractOpLink => Convert(abstractOpLink),
        CommentDfnElement dfn => Convert(dfn),
        CommentValueElement value => Convert(value, member),
        _ => "",
    };

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

    (string name, string value)[] Convert(CommentParamElement item, BaseCSAstItem member)
    {
        var list = new List<(string name, string value)>();
        foreach (var param in item.Items)
        {
            string name = param.Name;
            string value = string.Join("", param.Description.Select(i => Convert(i, member)));
            list.Add((name, value));
        }

        return [.. list];
    }

    string Convert(CommentSpecCommentElement item)
    {
        return "";
    }

    static string Convert(CommentValueElement item, BaseCSAstItem member)
    {
        var text = item.Text;
        if (member is CSMethod method)
        {
            var paramName = method.Parameters.FirstOrDefault(
                i => i.Name.Equals(item.Text, StringComparison.CurrentCultureIgnoreCase)
            )?.Name;
            if (paramName == null)
            {
                return text;
            }

            return $"""<paramref name="{paramName}"/>""";
        }

        return text;
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

        var namespaceName = csType.Namespace;
        if (namespaceName != null && namespaceName.EndsWith(".FFI"))
        {
            namespaceName = namespaceName[0..^".FFI".Length];
        }

        string cref;
        if (namespaceName != null)
        {
            cref = string.Join(".", (ReadOnlySpan<string>)[namespaceName, .. item.path.Select(ToWebgpuSharpNameFromTypeLink)]);
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

    string Convert(CommentNoteElement item, BaseCSAstItem member)
    {
        var text = string.Join("", item.Items.Select(i => Convert(i, member)));
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
            return $" <seealso href=\"{item.Url}\">{item.DisplayText}</seealso>";
        }
        else
        {
            return $" <seealso href=\"{item.Url}\"/>";
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
        if (name.StartsWith('"') && name.EndsWith('"'))
        {
            name = name[1..^1];
        }

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

        if (name.Length > 0 && char.IsNumber(name[0]))
        {
            int lastNumberIndex = 0;
            for (int i = 1; i < name.Length; i++)
            {
                if (char.IsNumber(name[i]))
                {
                    lastNumberIndex = i;
                }
                else
                {
                    break;
                }
            }
            //But number at the end
            name = name[(lastNumberIndex + 1)..] + name[0..(lastNumberIndex + 1)];
        }

        if (name.Length > 1 && char.IsLower(name[0]))
        {
            name = char.ToUpper(name[0]) + name[1..];
        }

        return name;
    }

}