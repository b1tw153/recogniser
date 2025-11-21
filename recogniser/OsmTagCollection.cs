// <copyright file="OsmTagCollection.cs" company="recogniser project contributors">
// Copyright (c) 2025 recogniser project contributors.
// Licensed under the AGPL-3.0-or-later license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Recogniser
{
    using System.Collections;

    internal sealed class OsmTagCollection : ICollection<OsmTag>
    {
        private readonly List<OsmTag> tags;

        public OsmTagCollection(List<OsmTag>? tags)
        {
            this.tags = [];
            if (tags != null)
            {
                this.tags.AddRange(tags);
            }
        }

        public int Count => tags.Count;

        public bool IsReadOnly => true;

        public string? this[string key]
        {
            get
            {
                OsmTag? tag = tags.Find(t => key.Equals(t.Key, StringComparison.Ordinal));
                return tag != null ? tag.Value : null;
            }
        }

        public bool ContainsKey(string key)
        {
            return tags.Exists(t => key.Equals(t.Key, StringComparison.Ordinal));
        }

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
            return tags.Contains(item);
        }

        public void CopyTo(OsmTag[] array, int arrayIndex)
        {
            tags.CopyTo(array, arrayIndex);
        }

        public IEnumerator<OsmTag> GetEnumerator()
        {
            return tags.GetEnumerator();
        }

        public bool Remove(OsmTag item)
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return tags.GetEnumerator();
        }
    }
}