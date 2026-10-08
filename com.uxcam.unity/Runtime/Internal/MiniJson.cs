using System;
using System.Collections;
using System.Globalization;
using System.Text;

namespace UXCamSDK.Internal
{
    // JsonUtility can't serialise dictionaries or top-level scalars.
    internal static class MiniJson
    {
        const int MaxDepth = 32;

        internal static string Serialize(object value)
        {
            var sb = new StringBuilder(64);
            Write(sb, value, 0);
            return sb.ToString();
        }

        static void Write(StringBuilder sb, object value, int depth)
        {
            if (depth > MaxDepth) throw new ArgumentException("UXCam: properties are nested too deeply.");

            switch (value)
            {
                case null:
                    sb.Append("null");
                    break;
                case string s:
                    WriteString(sb, s);
                    break;
                case bool b:
                    sb.Append(b ? "true" : "false");
                    break;
                case char c:
                    WriteString(sb, c.ToString());
                    break;
                case Enum e:
                    WriteString(sb, e.ToString());
                    break;
                case float f:
                    WriteDouble(sb, f);
                    break;
                case double d:
                    WriteDouble(sb, d);
                    break;
                case decimal m:
                    sb.Append(m.ToString(CultureInfo.InvariantCulture));
                    break;
                case sbyte _:
                case byte _:
                case short _:
                case ushort _:
                case int _:
                case uint _:
                case long _:
                case ulong _:
                    sb.Append(Convert.ToString(value, CultureInfo.InvariantCulture));
                    break;
                case IDictionary dict:
                    WriteObject(sb, dict, depth);
                    break;
                case IEnumerable list:
                    WriteArray(sb, list, depth);
                    break;
                case DateTime dt:
                    WriteString(sb, dt.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture));
                    break;
                default:
                    WriteString(sb, Convert.ToString(value, CultureInfo.InvariantCulture));
                    break;
            }
        }

        static void WriteObject(StringBuilder sb, IDictionary dict, int depth)
        {
            sb.Append('{');
            bool first = true;
            foreach (DictionaryEntry entry in dict)
            {
                if (entry.Key == null) continue;
                if (!first) sb.Append(',');
                first = false;
                WriteString(sb, Convert.ToString(entry.Key, CultureInfo.InvariantCulture));
                sb.Append(':');
                Write(sb, entry.Value, depth + 1);
            }
            sb.Append('}');
        }

        static void WriteArray(StringBuilder sb, IEnumerable list, int depth)
        {
            sb.Append('[');
            bool first = true;
            foreach (var item in list)
            {
                if (!first) sb.Append(',');
                first = false;
                Write(sb, item, depth + 1);
            }
            sb.Append(']');
        }

        static void WriteDouble(StringBuilder sb, double d)
        {
            // JSON has no NaN or Infinity.
            if (double.IsNaN(d) || double.IsInfinity(d))
                sb.Append("null");
            else
                sb.Append(d.ToString("R", CultureInfo.InvariantCulture));
        }

        static void WriteString(StringBuilder sb, string s)
        {
            sb.Append('"');
            foreach (char c in s)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 0x20)
                            sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        else
                            sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
        }
    }
}
