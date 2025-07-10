using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MiniJSON
{


    public static class JSON
    {
        public static object Deserialize(string json)
        {
            if (json == null) return null;
            return Parser.Parse(json);
        }

        public static string Serialize(object obj)
        {
            return Serializer.Serialize(obj);
        }

        sealed class Parser : IDisposable
        {
            const string WORD_BREAK = "{}[],:\"";

            public static bool IsWordBreak(char c) => Char.IsWhiteSpace(c) || WORD_BREAK.IndexOf(c) != -1;

            StringReader json;

            Parser(string jsonString) => json = new StringReader(jsonString);

            public static object Parse(string jsonString)
            {
                using (var instance = new Parser(jsonString))
                    return instance.ParseValue();
            }

            public void Dispose() => json.Dispose();

            enum TOKEN
            {
                NONE,
                CURLY_OPEN,
                CURLY_CLOSE,
                SQUARE_OPEN,
                SQUARE_CLOSE,
                COMMA,
                COLON,
                STRING,
                NUMBER,
                TRUE,
                FALSE,
                NULL
            }

            Dictionary<string, object> ParseObject()
            {
                var table = new Dictionary<string, object>();
                json.Read(); // consume '{'

                while (true)
                {
                    TOKEN token = NextToken;
                    if (token == TOKEN.NONE) return null;
                    if (token == TOKEN.CURLY_CLOSE) return table;

                    // key
                    string name = ParseString();
                    if (NextToken != TOKEN.COLON) return null;
                    json.Read(); // consume ':'

                    // value
                    table[name] = ParseValue();

                    if (NextToken == TOKEN.COMMA)
                    {
                        json.Read(); // consume ','
                        continue;
                    }
                    else if (NextToken == TOKEN.CURLY_CLOSE)
                    {
                        json.Read(); // consume '}'
                        return table;
                    }
                }
            }

            List<object> ParseArray()
            {
                var array = new List<object>();
                json.Read(); // consume '['

                while (true)
                {
                    TOKEN nextToken = NextToken;
                    if (nextToken == TOKEN.NONE) return null;
                    if (nextToken == TOKEN.SQUARE_CLOSE)
                    {
                        json.Read(); // consume ']'
                        break;
                    }

                    object value = ParseValue();
                    array.Add(value);

                    nextToken = NextToken;
                    if (nextToken == TOKEN.COMMA)
                    {
                        json.Read(); // consume ','
                        continue;
                    }
                    else if (nextToken == TOKEN.SQUARE_CLOSE)
                    {
                        json.Read(); // consume ']'
                        break;
                    }
                }

                return array;
            }

            object ParseValue()
            {
                switch (NextToken)
                {
                    case TOKEN.STRING: return ParseString();
                    case TOKEN.NUMBER: return ParseNumber();
                    case TOKEN.CURLY_OPEN: return ParseObject();
                    case TOKEN.SQUARE_OPEN: return ParseArray();
                    case TOKEN.TRUE:
                        json.Read();
                        json.Read();
                        json.Read();
                        json.Read();
                        return true;
                    case TOKEN.FALSE:
                        json.Read();
                        json.Read();
                        json.Read();
                        json.Read();
                        json.Read();
                        return false;
                    case TOKEN.NULL:
                        json.Read();
                        json.Read();
                        json.Read();
                        json.Read();
                        return null;
                    default: return null;
                }
            }

            string ParseString()
            {
                StringBuilder s = new StringBuilder();
                json.Read(); // consume opening '"'

                while (true)
                {
                    if (json.Peek() == -1) break;

                    char c = (char)json.Read();
                    if (c == '"') break;
                    if (c == '\\')
                    {
                        if (json.Peek() == -1) break;

                        c = (char)json.Read();
                        switch (c)
                        {
                            case '"': s.Append('"'); break;
                            case '\\': s.Append('\\'); break;
                            case '/': s.Append('/'); break;
                            case 'b': s.Append('\b'); break;
                            case 'f': s.Append('\f'); break;
                            case 'n': s.Append('\n'); break;
                            case 'r': s.Append('\r'); break;
                            case 't': s.Append('\t'); break;
                            case 'u':
                                var hex = new char[4];
                                for (int i = 0; i < 4; i++) hex[i] = (char)json.Read();
                                s.Append((char)Convert.ToInt32(new string(hex), 16));
                                break;
                        }
                    }
                    else
                    {
                        s.Append(c);
                    }
                }

                return s.ToString();
            }

            object ParseNumber()
            {
                string number = NextWord;
                if (number.IndexOf('.') == -1)
                {
                    if (long.TryParse(number, out var result)) return result;
                }
                else
                {
                    if (double.TryParse(number, System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture, out var d)) return d;
                }

                return 0;
            }

            void EatWhitespace()
            {
                while (json.Peek() != -1 && Char.IsWhiteSpace((char)json.Peek()))
                    json.Read();
            }

            char PeekChar => (char)json.Peek();

            string NextWord
            {
                get
                {
                    var word = new StringBuilder();
                    while (json.Peek() != -1 && !IsWordBreak((char)json.Peek()))
                        word.Append((char)json.Read());
                    return word.ToString();
                }
            }

            TOKEN NextToken
            {
                get
                {
                    EatWhitespace();
                    if (json.Peek() == -1) return TOKEN.NONE;

                    switch ((char)json.Peek())
                    {
                        case '{': return TOKEN.CURLY_OPEN;
                        case '}': return TOKEN.CURLY_CLOSE;
                        case '[': return TOKEN.SQUARE_OPEN;
                        case ']': return TOKEN.SQUARE_CLOSE;
                        case ',': return TOKEN.COMMA;
                        case ':': return TOKEN.COLON;
                        case '"': return TOKEN.STRING;
                        case '-':
                        case '0':
                        case '1':
                        case '2':
                        case '3':
                        case '4':
                        case '5':
                        case '6':
                        case '7':
                        case '8':
                        case '9': return TOKEN.NUMBER;
                    }

                    string word = NextWord;
                    switch (word)
                    {
                        case "true": return TOKEN.TRUE;
                        case "false": return TOKEN.FALSE;
                        case "null": return TOKEN.NULL;
                    }

                    return TOKEN.NONE;
                }
            }
        }

        sealed class Serializer
        {
            StringBuilder builder;

            Serializer() => builder = new StringBuilder();

            public static string Serialize(object obj)
            {
                var instance = new Serializer();
                instance.SerializeValue(obj);
                return instance.builder.ToString();
            }

            void SerializeValue(object value)
            {
                if (value == null)
                {
                    builder.Append("null");
                }
                else if (value is string s)
                {
                    SerializeString(s);
                }
                else if (value is bool b)
                {
                    builder.Append(b ? "true" : "false");
                }
                else if (value is IDictionary dict)
                {
                    SerializeObject(dict);
                }
                else if (value is IList list)
                {
                    SerializeArray(list);
                }
                else if (value is char c)
                {
                    SerializeString(c.ToString());
                }
                else
                {
                    builder.Append(Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture));
                }
            }

            void SerializeObject(IDictionary obj)
            {
                bool first = true;
                builder.Append('{');
                foreach (object e in obj.Keys)
                {
                    if (!first) builder.Append(',');
                    SerializeString(e.ToString());
                    builder.Append(':');
                    SerializeValue(obj[e]);
                    first = false;
                }

                builder.Append('}');
            }

            void SerializeArray(IList array)
            {
                builder.Append('[');
                bool first = true;
                foreach (object obj in array)
                {
                    if (!first) builder.Append(',');
                    SerializeValue(obj);
                    first = false;
                }

                builder.Append(']');
            }

            void SerializeString(string str)
            {
                builder.Append('"');
                foreach (var c in str)
                {
                    switch (c)
                    {
                        case '"': builder.Append("\\\""); break;
                        case '\\': builder.Append("\\\\"); break;
                        case '\b': builder.Append("\\b"); break;
                        case '\f': builder.Append("\\f"); break;
                        case '\n': builder.Append("\\n"); break;
                        case '\r': builder.Append("\\r"); break;
                        case '\t': builder.Append("\\t"); break;
                        default:
                            if (c < ' ' || c > 126)
                                builder.Append("\\u" + ((int)c).ToString("x4"));
                            else
                                builder.Append(c);
                            break;
                    }
                }

                builder.Append('"');
            }
        }
    }
}