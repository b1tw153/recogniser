namespace Recogniser
{
    internal class OsmTagProto
    {
        private readonly string name;
        private readonly string value;

        public OsmTagProto(string name, string value)
        {
            this.name = name;
            this.value = value;
        }

        public OsmTagProto(string tag)
        {
            string[] tagParts = tag.Split('=');
            if (tagParts.Length == 2)
            {
                this.name = tagParts[0];
                this.value = tagParts[1];
            }
            else
            {
                throw new FormatException("Invalid tag format: " + tag);
            }
        }

        public string Name
        {
            get { return name; }
        }

        public string Value
        {
            get { return value; }
        }

        public override string ToString()
        {
            return $"{name}={value}";
        }

        public bool Matches(string tag)
        {
            string[] tagParts = tag.Split('=');
            if (tagParts.Length == 2)
            {
                return Matches(tagParts[0], tagParts[1]);
            }
            return false;
        }

        public bool Matches(string name, string value)
        {
            if (this.name.Equals(name, StringComparison.Ordinal) || "*".Equals(name, StringComparison.Ordinal) || "*".Equals(this.name, StringComparison.Ordinal))
            {
                if (this.value.Equals(value, StringComparison.Ordinal) || "*".Equals(value, StringComparison.Ordinal) || "*".Equals(this.value, StringComparison.Ordinal))
                {
                    return true;
                }
            }
            return false;
        }

        public bool Matches(OsmTag tag)
        {
            return Matches(tag.Key, tag.Value);
        }
    }
}