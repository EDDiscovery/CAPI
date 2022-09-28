using System;
using System.Globalization;
using System.IO;
using System.Linq;
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
    public static class ObjectExtensionsNumbersBool
    {
        static public int InvariantParseInt(this string s, int def)
        {
            int i;
            return int.TryParse(s, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out i) ? i : def;
        }


        static public int? InvariantParseIntNull(this string s)     // s can be null
        {
            int i;
            if (s != null && int.TryParse(s, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out i))
                return i;
            else
                return null;
        }

        static public long InvariantParseLong(this string s, long def)
        {
            long i;
            return long.TryParse(s, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out i) ? i : def;
        }

        static public long? InvariantParseLongNull(this string s)
        {
            long i;
            if (s != null && long.TryParse(s, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out i))
                return i;
            else
                return null;
        }

        static public int? ToHex(this char c)
        {
            if (char.IsDigit(c))
                return c - '0';
            else if ("ABCDEF".Contains(c))
                return c - 'A' + 10;
            else if ("abcdef".Contains(c))
                return c - 'a' + 10;
            else
                return null;
        }

        static public int? ToHex(this string s, int p)
        {
            if ( s.Length>p+1)
            {
                int? top = ToHex(s[p]);
                int? bot = ToHex(s[p+1]);
                if (top.HasValue && bot.HasValue)
                    return (top << 4) | bot;
            }
            return null;
        }

        static public string FromHexString(this string ascii)
        {
            string s = "";
            for( int i = 0; i < ascii.Length; i += 2)
            {
                int? v = ascii.ToHex(i);
                if (v.HasValue)
                    s += Convert.ToChar(v.Value);
                else
                    return null;
            }

            return s;
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


    public static string ToStringZulu(this DateTime dt)     // zulu warrior format web style
    {
        if (dt.Millisecond != 0)
            return dt.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'");
        else
            return dt.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'Z'");

    }

}
