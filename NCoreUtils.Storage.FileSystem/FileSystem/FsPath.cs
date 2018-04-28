using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.Primitives;

namespace NCoreUtils.Storage.FileSystem
{
    /// <summary>
    /// Represents OS-independant relative path.
    /// </summary>
    public sealed class FsPath : IReadOnlyList<StringSegment>, IEquatable<FsPath>
    {
        public sealed class Enumerator : IEnumerator<StringSegment>
        {
            readonly FsPath _path;

            int _offset;

            object IEnumerator.Current => Current;

            public StringSegment Current => _path[_offset];

            internal Enumerator(FsPath path)
            {
                _path = path;
                Reset();
            }

            public void Dispose() { }

            public bool MoveNext() => ++_offset < _path.Count;

            public void Reset() => _offset = -1;
        }

        public struct StringIndexer
        {
            readonly FsPath _path;

            public string this[int index]
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    var component = _path._components[index];
                    return _path._source.Substring(component.offset, component.length);
                }
            }

            public StringIndexer(FsPath path) => _path = path;
        }

        static readonly char[] _separators = new [] { '/', '\\' };

        public static FsPath Empty { get; } = new FsPath(string.Empty, ImmutableArray<(int, int)>.Empty);

        public static bool operator==(FsPath a, FsPath b) => null == a ? null == b : a.Equals(b);

        public static bool operator!=(FsPath a, FsPath b) => null == a ? null != b : !a.Equals(b);

        public static FsPath operator+(FsPath a, string b)
        {
            if (a == null)
            {
                throw new ArgumentNullException(nameof(a));
            }
            return a.Add(b);
        }

        public static FsPath Parse(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return Empty;
            }
            var segments = ImmutableArray.CreateBuilder<(int, int)>();
            var pos = 0;
            var len = path.Length;
            while (pos <= len)
            {
                var index = path.IndexOfAny(_separators, pos);
                if (-1 == index)
                {
                    if (len - pos > 0)
                    {
                        segments.Add((pos, len - pos));
                    }
                    pos = len;
                }
                else
                {
                    if (index - pos > 0)
                    {
                        segments.Add((pos, index - pos));
                    }
                    pos = index + 1;
                }
            }
            return new FsPath(path, segments.ToImmutable());
        }

        readonly string _source;

        readonly ImmutableArray<(int offset, int length)> _components;

        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _components.Length;
        }

        public string Name
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => 0 == Count ? string.Empty : this[Count - 1].Value;
        }

        public StringIndexer Strings => new StringIndexer(this);

        public StringSegment this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new StringSegment(_source, _components[index].offset, _components[index].length);
        }

        FsPath(string source, ImmutableArray<(int offset, int length)> components)
        {
            _source = source;
            _components = components;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        IEnumerator<StringSegment> IEnumerable<StringSegment>.GetEnumerator() => GetEnumerator();

        public FsPath Add(string name) => FsPath.Parse(_source + System.IO.Path.DirectorySeparatorChar + name);

        public FsPath ChangeName(string name)
        {
            if (0 == Count)
            {
                return new FsPath(name, ImmutableArray.Create<(int, int)>((0, name.Length)));
            }
            var newPath = _source.Substring(0, _components[Count - 1].offset) + name;
            return FsPath.Parse(newPath);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(FsPath other)
        {
            if (null == other)
            {
                return false;
            }
            if (Count != other.Count)
            {
                return false;
            }
            for (var i = 0; i < Count; ++i)
            {
                if (this[i] != other[i])
                {
                    return false;
                }
            }
            return true;
        }

        public override bool Equals(object obj) => Equals(obj as FsPath);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator() => new Enumerator(this);

        public override int GetHashCode()
        {
            var hash = 0;
            for (var i = 0; i < Count; ++i)
            {
                hash = hash * 17 + this[i].GetHashCode();
            }
            return hash;
        }

        public string Join(string separator)
        {
            if (0 == Count)
            {
                return string.Empty;
            }
            var builder = new StringBuilder(_components.Sum(component => component.length) + separator.Length * (Count - 1));
            builder.Append(_source, _components[0].offset, _components[0].length);
            for (var i = 1; i < _components.Length; ++i)
            {
                builder.Append(separator);
                builder.Append(_source, _components[i].offset, _components[i].length);
            }
            return builder.ToString();
        }

        public FsPath SubPath(int length)
        {
            if (length < 0)
            {
                length = Count + length;
            }

            if (length > Count)
            {
                throw new InvalidOperationException($"Trying to get subpath with length = {length} from path with {Count} segments.");
            }
            var lastComp = _components[length - 1];
            var strlen = lastComp.offset + lastComp.length;
            var newPath = _source.Substring(0, strlen);
            return FsPath.Parse(newPath);
        }
        public StringSegment[] ToArray()
        {
            var result = new StringSegment[Count];
            for (var i = 0; i < Count; ++i)
            {
                result[i] = this[i];
            }
            return result;
        }

        public string[] ToStringArray()
        {
            var result = new string[Count];
            var indexer = Strings;
            for (var i = 0; i < Count; ++i)
            {
                result[i] = indexer[i];
            }
            return result;
        }
    }
}