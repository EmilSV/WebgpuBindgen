namespace WebgpuBindgen;

public static class StringExtension
{
    public static string FirstCharToUpper(this string str)
    {
        if (str.Length == 0)
        {
            return str;
        }

        if (str.Length == 1)
        {
            return char.ToUpper(str[0]).ToString();
        }

        return char.ToUpper(str[0]) + str[1..];
    }
}