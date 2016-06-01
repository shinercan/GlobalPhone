using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using Hash = System.Collections.Generic.IDictionary<string, object>;
using RegexKvp = System.Collections.Generic.KeyValuePair<string, System.Text.RegularExpressions.Regex>;
namespace GlobalPhone
{
    public class Territory : Record
    {
        private readonly Region _region;
        public readonly string Name;
        public readonly Regex PossiblePattern;
        public readonly Regex NationalPattern;
        public readonly string NationalPrefixFormattingRule;
        public readonly IEnumerable<RegexKvp> ValidNumberFormats;
        public Territory(object data, Region region)
            : base(data)
        {
            _region = region;
            Name = Field<string>(0, column: "name");
            PossiblePattern = Field<string, Regex>(1, column: "possibleNumber", block: p => new Regex("^(" + p + ")$"));
            NationalPattern = Field<string, Regex>(2, column: "nationalNumber", block: p => new Regex("^(" + p + ")$"));
            NationalPrefixFormattingRule = Field<string>(3, column: "formattingRule");
            ValidNumberFormats = Field<object[], IEnumerable<RegexKvp>>(4, column: "possibleFormats", block:
                formats =>
                    formats.Select(format =>
                        IsArray(format)
                            ? AsArray(format).Select(f => KeyValue("__", new Regex("^(" + f + ")$")))
                            : AsHash(format).Select(f =>
                                KeyValue(f.Key.ToString(), new Regex("^(" + f.Value + ")$")))
                    ).Flatten<RegexKvp>());
        }

        public string CountryCode
        {
            get { return _region.CountryCode; }
        }
        public Regex InternationalPrefix
        {
            get { return _region.InternationalPrefix; }
        }
        public string NationalPrefix
        {
            get { return _region.NationalPrefix; }
        }
        public Regex NationalPrefixForParsing
        {
            get { return _region.NationalPrefixForParsing; }
        }

        public string NationalPrefixTransformRule
        {
            get { return _region.NationalPrefixTransformRule; }
        }

        public Region Region
        {
            get { return _region; }
        }

        public Number ParseNationalString(string str)
        {
            str = ToNationalNumber(str);
            if (Possible(str))
                return new Number(this, str);
            throw new FailedToParseNumberException("not possible for " + Name);
        }

        private bool Possible(string str)
        {
            return PossiblePattern.Match(str ?? string.Empty).Success;
        }

        protected internal string Normalize(string str)
        {
            return Number.Normalize((E161.UsedBy(this)) ? E161.Normalize(str) : str);
        }

        protected string ToNationalNumber(string str)
        {
            return StripNationalPrefix(Number.Normalize(str));
        }

        private bool NationalNumber(string str)
        {
            return NationalPattern.Match(str ?? string.Empty).Success;
        }

        protected string StripNationalPrefix(string str)
        {
            string stringWithoutPrefix = null;
            if (NationalPrefixForParsing != null && NationalPrefixForParsing.Match(str).Success)
            {
                var transformRule = NationalPrefixTransformRule ?? "";
                stringWithoutPrefix = NationalPrefixForParsing.Replace(str, transformRule, 1);
            }
            else if (StartsWithNationalPrefix(str))
            {
                stringWithoutPrefix = str.Substring(NationalPrefix.Length);
            }
            return NationalNumber(stringWithoutPrefix) ? stringWithoutPrefix : str;
        }

        protected bool StartsWithNationalPrefix(string str)
        {
            return NationalPrefix != null && str.StartsWith(NationalPrefix);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof(Territory)) return false;
            return Equals((Territory)obj);
        }

        public bool Equals(Territory other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(other.Name, Name);
        }

        public override int GetHashCode()
        {
            return (Name != null ? Name.GetHashCode() : 0);
        }
        public override string ToString()
        {
            return Name;
        }
    }
}
