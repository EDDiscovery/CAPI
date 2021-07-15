using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace BaseUtils
{

    public static class FileHelpers
    {
        public static string TryReadAllTextFromFile(string filename)
        {
            if (File.Exists(filename))
            {
                try
                {
                    return File.ReadAllText(filename, Encoding.UTF8);
                }
                catch
                {
                    return null;
                }
            }
            else
                return null;
        }
    }
}

public static class ObjectExtensionsDates
{
    static public DateTime StartOfDay(this DateTime tme)      // start of day, 0:0:0
    {
        return new DateTime(tme.Year, tme.Month, tme.Day, 0, 0, 0, tme.Kind);
    }
}

public static class ObjectExtensionsStrings
{
    public static string SafeFileString(this string normal)
    {
        normal = normal.Replace("*", "_star");      // common ones rename
        normal = normal.Replace("/", "_slash");
        normal = normal.Replace("\\", "_slash");
        normal = normal.Replace(":", "_colon");
        normal = normal.Replace("?", "_qmark");

        char[] invalid = System.IO.Path.GetInvalidFileNameChars();
        foreach (char c in invalid)
            normal = normal.Replace(c, '_'); // all others _

        return normal;
    }

    static public bool HasChars(this string obj)
    {
        return obj != null && obj.Length > 0;
    }

    public static string ToStringInvariant(this int v, string format)
    {
        return v.ToString(format, System.Globalization.CultureInfo.InvariantCulture);
    }

    // trim, then if it ends with this, trim it
    public static string TrimReplaceEnd(this string obj, char endreplace)
    {
        obj = obj.Trim();
        int ep = obj.Length - 1;
        while (ep >= 0 && obj[ep] == endreplace)
            ep--;
        return obj.Substring(0, ep + 1);
    }

    public static string ToNullSafeString(this object obj)
    {
        return (obj ?? string.Empty).ToString();
    }


    static public string LineNumbering(this string s, int start, string fmt = "N", string newline = null)
    {
        if (newline == null)
            newline = Environment.NewLine;

        StringBuilder sb = new StringBuilder();
        int position = 0, positions = 0;
        while ((positions = s.IndexOf(newline, position)) != -1)
        {
            sb.Append(start.ToStringInvariant(fmt));
            sb.Append(':');
            sb.Append(s.Substring(position, positions - position));
            sb.Append(newline);
            position = positions + newline.Length;
            start++;
        }

        if (position < s.Length)
            sb.Append(s.Substring(position));

        return sb.ToNullSafeString();
    }


}
