using System;
using System.Buffers;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.Toolkit.HighPerformance;
using NCoreUtils.Collections;
using NCoreUtils.Memory;

namespace NCoreUtils
{
    public struct GenericSubpath : IEquatable<GenericSubpath>, IEmplaceable<GenericSubpath>
    {
        internal struct Segment
        {
            public int Start { get; }

            public int Length { get; }

            public Segment(int start, int length)
            {
                Start = start;
                Length = length;
            }
        }

        public ref struct StringEnumerator
        {
            private readonly string? _source;

            private readonly ImmutableHeadArray<Segment>.Enumerator _enumerator;

            public string Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    if (string.IsNullOrEmpty(_source))
                    {
                        return string.Empty;
                    }
                    ref readonly Segment segment = ref _enumerator.Current;
                    return _source.Substring(segment.Start, segment.Length);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal StringEnumerator(string? source, ImmutableHeadArray<Segment>.Enumerator enumerator)
            {
                _source = source;
                _enumerator = enumerator;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal StringEnumerator(in GenericSubpath source)
                : this(source._source, source._segments.GetEnumerator())
            { }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
                => _enumerator.MoveNext();
        }

        public ref struct StringAccessor
        {
            private ReadOnlyRef<GenericSubpath> _ref;

            public string this[int index]
            {
                get
                {
                    var source = _ref.Value._source;
                    if (string.IsNullOrEmpty(source))
                    {
                        throw new IndexOutOfRangeException();
                    }
                    ref readonly Segment segment = ref _ref.Value._segments[index];
                    return source.Substring(segment.Start, segment.Length);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal StringAccessor(ReadOnlyRef<GenericSubpath> @ref)
            {
                _ref = @ref;
            }

            public StringEnumerator GetEnumerator()
                => new StringEnumerator(in _ref.Value);
        }

        public static readonly GenericSubpath Empty = default;

        public static GenericSubpath operator+(GenericSubpath a, GenericSubpath b)
            => a.Append(b);

        public static GenericSubpath operator+(GenericSubpath a, string b)
            => a.Append(b);

        public static GenericSubpath Parse(string? input, int startIndex = 0)
        {
            if (string.IsNullOrEmpty(input))
            {
                return default;
            }
            var src = input.AsSpan();
            var start = startIndex;
            var segments = new ImmutableHeadArrayBuilder<Segment>(7);
            var i = startIndex;
            while (i < src.Length)
            {
                if (IsSeparator(src[i]))
                {
                    segments.Add(new Segment(start, i - start));
                    start = i + 1;
                }
                ++i;
            }
            if (start < src.Length)
            {
                segments.Add(new Segment(start, src.Length - start));
            }
            return new GenericSubpath(input, segments.Build());
        }

        private const int MaxStackAllocSize = 8 * 1024;

        private const int MaxPoolAllocSize = 16 * 1024;

        private static readonly char[] _separators = new []
        {
            Path.DirectorySeparatorChar,
            Path.AltDirectorySeparatorChar
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsSeparator(char ch)
            => ch == '/' || ch == '\\';

        private readonly string? _source;

        private readonly ImmutableHeadArray<Segment> _segments;

        public bool IsEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => string.IsNullOrEmpty(_source);
        }

        public int SegmentCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _segments.Length;
        }

        public StringAccessor Strings
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new StringAccessor(new ReadOnlyRef<GenericSubpath>(in this));
        }

        private GenericSubpath(string source, ImmutableHeadArray<Segment> segments)
        {
            _source = source;
            _segments = segments;
        }

        #region internal helpers

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ReadOnlySpan<char> GetSpan(in Segment segment)
            => _source.AsSpan().Slice(segment.Start, segment.Length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private string GetSubstringUnsafe(Segment segment)
            => _source!.Substring(segment.Start, segment.Length);

        #endregion

        #region emplaceable

        public int ComputeRequiredBufferSize()
        {
            if (string.IsNullOrEmpty(_source))
            {
                return 0;
            }
            var total = 0;
            // NOTE: invariant: _segments.Length > 0 when _source is non-empty.
            foreach (ref readonly Segment segment in _segments)
            {
                total += segment.Length;
            }
            total += _segments.Length - 1;
            return total;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int EmplaceUnchecked(Span<char> buffer, char separator)
        {
            var builder = new SpanBuilder(buffer);
            for (var i = 0; i < _segments.Length; ++i)
            {
                if (0 != i)
                {
                    builder.Append(separator);
                }
                builder.Append(GetSpan(_segments[i]));
            }
            return builder.Length;
        }

        public int Emplace(Span<char> buffer, char separator)
        {
            if (IsEmpty)
            {
                return 0;
            }
            var requiredSize = ComputeRequiredBufferSize();
            if (requiredSize > buffer.Length)
            {
                throw new InsufficientBufferSizeException(buffer, requiredSize);
            }
            return EmplaceUnchecked(buffer, separator);
        }

        public int Emplace(Span<char> buffer)
            => Emplace(buffer, Path.DirectorySeparatorChar);

        public bool TryEmplace(Span<char> buffer, char separator, out int size)
        {
            if (IsEmpty)
            {
                size = 0;
                return true;
            }
            var requiredSize = ComputeRequiredBufferSize();
            if (requiredSize > buffer.Length)
            {
                size = 0;
                return false;
            }
            size = EmplaceUnchecked(buffer, separator);
            return true;
        }

        public bool TryEmplace(Span<char> buffer, out int size)
            => TryEmplace(buffer, Path.DirectorySeparatorChar, out size);

        public string ToString(char separator)
        {
            if (IsEmpty)
            {
                return string.Empty;
            }
            var requiredSize = ComputeRequiredBufferSize();
            if (requiredSize <= MaxStackAllocSize)
            {
                Span<char> buffer = stackalloc char[requiredSize];
                EmplaceUnchecked(buffer, separator);
                return buffer.ToString();
            }
            if (requiredSize <= MaxPoolAllocSize)
            {
                using var buffer = MemoryPool<char>.Shared.Rent(requiredSize);
                var size = EmplaceUnchecked(buffer.Memory.Span, separator);
                return buffer.Memory.Span.Slice(0, size).ToString();
            }
            // fallback to allocation
            return string.Join(separator, _segments.ToArray().Select(GetSubstringUnsafe));
        }

        #endregion

        #region equatable

        public bool Equals(GenericSubpath other)
        {
            if (IsEmpty)
            {
                return other.IsEmpty;
            }
            if (_segments.Length != other._segments.Length)
            {
                return false;
            }
            for (var i = 0; i < _segments.Length; ++i)
            {
                var selfSegment = GetSpan(in _segments[i]);
                var otherSegment = other.GetSpan(in other._segments[i]);
                if (!selfSegment.IsSame(otherSegment))
                {
                    return false;
                }
            }
            return true;
        }

        public override bool Equals(object? obj)
            => obj is GenericSubpath other && Equals(other);

        public override int GetHashCode()
        {
            var hash = new HashCode();
            for (var i = 0; i < _segments.Length; ++i)
            {
                var span = GetSpan(in _segments[i]);
                foreach (var ch in span)
                {
                    hash.Add(ch);
                }
            }
            return hash.ToHashCode();
        }

        #endregion

        public GenericSubpath Append(in GenericSubpath appendix)
        {
            if (IsEmpty)
            {
                return appendix;
            }
            if (appendix.IsEmpty)
            {
                return this;
            }
            var l = _segments.Length;
            var sl = _source!.Length;
            Span<Segment> buffer = stackalloc Segment[l + appendix._segments.Length];
            _segments.CopyTo(buffer);
            appendix._segments.CopyTo(buffer.Slice(l));
            for (var i = 0; i < appendix._segments.Length; ++i)
            {
                var orig = buffer[l + i];
                buffer[l + i] = new Segment(orig.Start + sl, orig.Length);
            }
            return new GenericSubpath(_source! + appendix._source!, new ImmutableHeadArray<Segment>(buffer));
        }

        public GenericSubpath Append(string appendix)
        {
            if (string.IsNullOrEmpty(appendix))
            {
                return this;
            }
            if (-1 == appendix.IndexOfAny(_separators))
            {
                // no separators
                if (IsEmpty)
                {
                    return new GenericSubpath(appendix, new ImmutableHeadArray<Segment>(new Segment(0, appendix.Length)));
                }
                return new GenericSubpath(_source! + appendix, _segments.Append(new Segment(_source!.Length, appendix.Length)));
            }
            var subpath = Parse(appendix);
            return Append(subpath);
        }

        public GenericSubpath GetParentPath()
            => new GenericSubpath(_source!, _segments.Pop());

        public GenericSubpath Unshift(out string prefix)
        {
            if (IsEmpty)
            {
                throw new InvalidOperationException("Unable to unshift empty subpath");
            }
            if (_segments.Length == 1)
            {
                prefix = GetSubstringUnsafe(_segments[0]);
                return default;
            }
            var res = new GenericSubpath(_source!, _segments.Unshift(out var segment));
            prefix = GetSubstringUnsafe(segment);
            return res;
        }

        public override string ToString()
            => ToString(Path.DirectorySeparatorChar);
    }
}