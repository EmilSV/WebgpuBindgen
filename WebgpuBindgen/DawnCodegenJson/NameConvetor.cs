using System.Text;

namespace WebgpuBindgen.DawnCodegenJson;


public static class NameConvertor
{
    public static string ConvertName(string fromName, NameStyle fromStyle, NameStyle toStyle)
    {
        var parts = fromStyle switch
        {
            NameStyle.DawnCodegen => DawnCodeGenToParts(fromName),
            NameStyle.CSharp => CsStyleToParts(fromName),
            NameStyle.CSharpConstant => CSharpConstantToParts(fromName),
            _ => throw new NotSupportedException($"Unsupported name style: {fromStyle}")
        };

        var toName = toStyle switch
        {
            NameStyle.CSharp => PartsToCsStyle(parts),
            NameStyle.DawnCodegen => PartsToDawnCodeGenStyle(parts),
            NameStyle.CSharpConstant => PartsToCSharpConstantStyle(parts),
            _ => throw new NotSupportedException($"Unsupported name style: {toStyle}")
        };

        return toName;
    }

    private static List<string> CsStyleToParts(string name)
    {
        var sb = new StringBuilder();
        var parts = new List<string>();

        if (name.Length == 0)
        {
            return parts;
        }
        else if (name.Length == 1)
        {
            parts.Add(name.ToLowerInvariant());
            return parts;
        }

        for (int i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (char.IsUpper(c) || (!char.IsNumber(c) && i > 0 && char.IsNumber(name[i - 1]) && parts.Count == 0) && sb.Length > 0)
            {
                parts.Add(sb.ToString());
                sb.Clear();
            }
            sb.Append(char.ToLowerInvariant(c));
        }

        if (sb.Length > 0)
        {
            parts.Add(sb.ToString());
        }


        return parts;
    }

    private static List<string> DawnCodeGenToParts(string name)
    {
        var sb = new StringBuilder();
        var parts = new List<string>();

        if (name.Length == 0)
        {
            return parts;
        }
        else if (name.Length == 1)
        {
            parts.Add(name.ToLowerInvariant());
            return parts;
        }

        for (int i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (c == ' ' || (!char.IsNumber(c) && i > 0 && char.IsNumber(name[i - 1]) && parts.Count == 0) && sb.Length > 0)
            {
                parts.Add(sb.ToString());
                sb.Clear();
            }
            if (c != ' ')
            {
                sb.Append(char.ToLowerInvariant(c));
            }
        }
        if (sb.Length > 0)
        {
            parts.Add(sb.ToString());
        }

        return parts;
    }

    private static List<string> CSharpConstantToParts(string name)
    {
        var sb = new StringBuilder();
        var parts = new List<string>();

        if (name.Length == 0)
        {
            return parts;
        }
        else if (name.Length == 1)
        {
            parts.Add(name.ToLowerInvariant());
            return parts;
        }

        for (int i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (c == '_' || (!char.IsNumber(c) && i > 0 && char.IsNumber(name[i - 1]) && parts.Count == 0) && sb.Length > 0)
            {
                parts.Add(sb.ToString());
                sb.Clear();
            }
            if (c != '_')
            {
                sb.Append(char.ToLowerInvariant(c));
            }
        }

        if (sb.Length > 0)
        {
            parts.Add(sb.ToString());
        }


        return parts;
    }

    private static string PartsToCsStyle(List<string> parts)
    {
        if (parts.Count == 0)
        {
            return string.Empty;
        }
        if (parts.Count == 1)
        {
            var name = parts[0];
            return name switch
            {
                "" => string.Empty,
                [char c] when !char.IsNumber(c) => $"_{char.ToUpperInvariant(c)}",
                [char c] => char.ToLowerInvariant(c).ToString(),
                _ => char.ToUpperInvariant(name[0]) + name[1..]
            };
        }

        // Handle cases like "3d" or "2D"
        var firstPart = parts[0];
        if (char.IsNumber(firstPart[0]))
        {
            var secondPart = parts[1];
            parts[0] = secondPart;
            parts[1] = firstPart;
        }


        var sb = new StringBuilder();
        foreach (var part in parts)
        {
            if (part.Length == 0)
            {
                continue;
            }
            sb.Append(char.ToUpperInvariant(part[0]));
            if (part.Length > 1)
            {
                sb.Append(part[1..]);
            }
        }

        return sb.ToString();
    }

    private static string PartsToCSharpConstantStyle(List<string> parts)
    {
        if (parts.Count == 0)
        {
            return string.Empty;
        }
        if (parts.Count == 1)
        {
            var name = parts[0];
            return name switch
            {
                "" => string.Empty,
                [char c] when !char.IsNumber(c) => $"_{char.ToUpperInvariant(c)}",
                [char c] => char.ToLowerInvariant(c).ToString(),
                _ => name.ToUpperInvariant()
            };
        }

        // Handle cases like "3d" or "2D"
        var firstPart = parts[0];
        if (char.IsNumber(firstPart[0]))
        {
            var secondPart = parts[1];
            parts[0] = secondPart;
            parts[1] = firstPart;
        }

        var sb = new StringBuilder();
        for (int i = 0; i < parts.Count; i++)
        {
            if (i > 0)
            {
                sb.Append('_');
            }
            sb.Append(parts[i].ToUpperInvariant());
        }

        return sb.ToString();
    }

    private static string PartsToDawnCodeGenStyle(List<string> parts)
    {
        if (parts.Count == 0)
        {
            return string.Empty;
        }
        if (parts.Count == 1)
        {
            return parts[0];
        }

        var sb = new StringBuilder();
        for (int i = 0; i < parts.Count; i++)
        {
            if (i > 0)
            {
                sb.Append(' ');
            }
            sb.Append(parts[i]);
        }

        return sb.ToString();
    }
}