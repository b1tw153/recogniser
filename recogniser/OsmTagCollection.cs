using System.Collections;

namespace recogniser
{
    public class OsmTagCollection : ICollection<OsmTag>
    {
        List<OsmTag> _tags;

        public OsmTagCollection(List<OsmTag>? tags)
        {
            _tags = new List<OsmTag>();
            if (tags != null)
            {
                _tags.AddRange(tags);
            }
        }

        public bool ContainsKey(string key)
        {
            return _tags.Exists(t => key.Equals(t.Key));
        }

        public string? this[string key]
        {
            get
            {
                OsmTag? tag = _tags.Find(t => key.Equals(t.Key));
                return tag != null ? tag.Value : null;
            }
        }

        public int Count => _tags.Count;

        public bool IsReadOnly => true;

        public void Add(OsmTag item)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(OsmTag item)
        {
            return _tags.Contains(item);
        }

        public void CopyTo(OsmTag[] array, int arrayIndex)
        {
            _tags.CopyTo(array, arrayIndex);
        }

        public IEnumerator<OsmTag> GetEnumerator()
        {
            return _tags.GetEnumerator();
        }

        public bool Remove(OsmTag item)
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _tags.GetEnumerator();
        }
    }
}