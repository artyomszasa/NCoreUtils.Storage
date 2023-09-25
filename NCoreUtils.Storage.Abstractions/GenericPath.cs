using System;
using System.Buffers;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.Toolkit.HighPerformance;
using NCoreUtils.Collections;
using NCoreUtils.Memory;

namespace NCoreUtils
{
    public struct GenericPath : IEmplaceable<GenericPath>
    {
        private struct Segment
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
            private readonly Ref<GenericPath> _source;

            /// <summary>
            /// Index can be:
            /// <para>
            /// <c>0</c> --> uninitalized;
            /// </para>
            /// <para>
            /// <c>-1</c> --> iteration has been completed;
            /// </para>
            /// <para>
            /// or valid index --> if source is rooted 1 refers to the root, otherwise 1 refers to the first segment.
            /// </para>
            /// </summary>
            private int _index;

            public string Current
            {
                get
                {
                    if (_index > 0)
                    {
                        // source and index are valid
                        ref readonly GenericPath path = ref _source.Value;
                        int segmentIndex;
                        if (path._root.HasValue)
                        {
                            if (_index == 1)
                            {
                                return path._source!.Substring(path._root.Value.Start, path._root.Value.Length);
                            }
                            segmentIndex = _index - 2;
                        }
                        else
                        {
                            segmentIndex = _index - 1;
                        }
                        var segment = path._segments![segmentIndex];
                        return path._source!.Substring(segment.Start, segment.Length);
                    }
                    // either index is uninitialized or iteration has been completed...
                    return default!;
                }
            }

            public StringEnumerator(ref GenericPath path)
            {
                _source = new Ref<GenericPath>(ref path);
                _index = 0;
            }

            public bool MoveNext()
            {
                if (_index == -1)
                {
                    return false;
                }
                ref readonly GenericPath path = ref _source.Value;
                if (_index == 0)
                {
                    // initialize
                    if (path.IsEmpty)
                    {
                        // mark as done
                        _index = -1;
                        return false;
                    }
                    // either root or segments are resent
                    _index = 1;
                    return true;
                }
                // try advance ot mark as done
                if (path.IsRooted)
                {
                    if (_index == 1)
                    {
                        // only advance is there are segments
                        if (!path._segments.IsEmpty)
                        {
                            _index = 2;
                            return true;
                        }
                        _index = -1;
                        return false;
                    }
                    if (path._segments!.Length > _index - 1)
                    {
                        ++_index;
                        return true;
                    }
                    _index = -1;
                    return false;
                }
                if (path._segments!.Length > _index)
                {
                    ++_index;
                    return true;
                }
                _index = -1;
                return false;
            }
        }

        public readonly ref struct StringEnumerable
        {
            private readonly Ref<GenericPath> _source;

            public StringEnumerable(scoped ref GenericPath path)
            {
                _source = new Ref<GenericPath>(ref path);
            }

            public StringEnumerator GetEnumerator()
                => new StringEnumerator(ref _source.Value);
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

        public static GenericPath Parse(string? source)
        {
            if (string.IsNullOrEmpty(source))
            {
                return default;
            }
            Segment? root = default;
            ImmutableHeadArrayBuilder<Segment> segments = default;
            ReadOnlySpan<char> input = source.AsSpan();
            var start = 0;
            // handle roots
            if (input[0] == '/')
            {
                // linux root
                root = new Segment(0, 0);
                start = 1;
            }
            else if (input[0] == '\\' && input.Length > 1 && input[1] == '\\')
            {
                // windows network root
                root = new Segment(0, 1);
                start = 2;
            }
            else
            {
                // windows drive root
                var separatorIndex = input.IndexOfAny('/', '\\');
                var colonIndex = input.IndexOf(':');
                if (colonIndex == 1 && ((separatorIndex == -1 && input.Length == 2) || separatorIndex == 2))
                {
                    root = new Segment(0, 2);
                    start = Math.Min(input.Length, 3);
                }
            }

            for (var i = start; i < input.Length; ++i)
            {
                var ch = input[i];
                if (IsSeparator(ch))
                {
                    segments.Add(new Segment(start, i - start));
                    start = i + 1;
                }
            }

            if (start < input.Length)
            {
                segments.Add(new Segment(start, input.Length - start));
            }
            return new GenericPath(source, root, segments.Build());
        }

        private readonly string? _source;

        private readonly Segment? _root;

        private readonly ImmutableHeadArray<Segment> _segments;

        public bool IsEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => string.IsNullOrEmpty(_source);
        }

        public bool IsRooted
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _root.HasValue;
        }

        public StringEnumerable Strings
            => new StringEnumerable(ref this);

        public int SegmentCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _segments.Length;
        }

        private GenericPath(string? source, Segment? root, ImmutableHeadArray<Segment> segments)
        {
            _source = source;
            _root = root;
            _segments = segments;
        }

        private int ComputeRequiredBufferSize()
        {
            if (string.IsNullOrEmpty(_source))
            {
                return 0;
            }
            var total = 0;
            if (_root.HasValue)
            {
                total += _root.Value.Length + 1;
            }
            if (!_segments.IsEmpty)
            {
                for (var i = 0; i < _segments.Length; ++i)
                {
                    total += _segments[i].Length;
                }
                total += _segments.Length - 1;
            }
            return total;
        }

        private int EmplaceUnchecked(Span<char> buffer, char separator)
        {
            var builder = new SpanBuilder(buffer);
            if (_root.HasValue)
            {
                builder.Append(GetSpan(_root.Value));
                builder.Append(separator);
            }
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

        private ReadOnlySpan<char> GetSpan(in Segment segment)
        {
            return _source.AsSpan().Slice(segment.Start, segment.Length);
        }

        private string GetSubstringUnsafe(Segment segment)
        {
            return _source!.Substring(segment.Start, segment.Length);
        }

        public GenericPath Append(in GenericPath appendix)
        {
            if (appendix.IsRooted)
            {
                throw new InvalidOperationException($"Unable to append rooted path \"{appendix}\" to \"{this}\".");
            }
            if (appendix.IsEmpty)
            {
                return this;
            }
            if (IsEmpty)
            {
                return appendix;
            }
            return new GenericPath(_source! + appendix._source!, _root, _segments.Append(appendix._segments));
        }

        public GenericPath Append(string appendix)
        {
            if (string.IsNullOrEmpty(appendix))
            {
                return this;
            }
            if (-1 != appendix.IndexOfAny(_separators))
            {
                if (IsEmpty)
                {
                    return new GenericPath(appendix, default, new ImmutableHeadArray<Segment>(new Segment(0, appendix.Length)));
                }
                return new GenericPath(
                    _source! + appendix,
                    _root,
                    _segments.Append( new Segment(_source!.Length, appendix.Length))
                );
            }
            var asPath = Parse(appendix);
            return Append(asPath);
        }

        public GenericPath GetParentPath()
            => new GenericPath(_source, _root, _segments.Pop());

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
            return _segments.IsEmpty
                ? _root.HasValue
                    ? $"{GetSubstringUnsafe(_root.Value)}{separator}"
                    : string.Empty
                : _root.HasValue
                    ? string.Join(separator, _segments.ToArray().Prepend(_root.Value).Select(GetSubstringUnsafe))
                    : string.Join(separator, _segments.ToArray().Select(GetSubstringUnsafe));
        }

        public override string ToString()
            => ToString(Path.DirectorySeparatorChar);
    }
}