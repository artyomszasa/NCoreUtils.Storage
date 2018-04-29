using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace NCoreUtils.Storage.FileSystem
{
    public class FsPathTests
    {
        [Fact]
        public void Enumerate()
        {
            var components = new [] { "a", "b", "c" };
            var path = FsPath.Parse(string.Join("/", components));
            var enumerable = (IEnumerable<StringSegment>)path;
            Assert.Equal(components.Select(s => new StringSegment(s)), path);
            var i = 0;
            using (var enumerator = enumerable.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    Assert.Equal(new StringSegment(components[i]), enumerator.Current);
                    ++i;
                }
            }
        }

        [Fact]
        public void ToArray()
        {
            var components = new [] { "a", "b", "c" };
            var path = FsPath.Parse(string.Join("/", components));
            var ssArray = path.ToArray();
            var sArray = path.ToStringArray();
            Assert.Equal(components.Select(s => new StringSegment(s)), ssArray);
            Assert.Equal((IEnumerable<string>)components, sArray);
        }

        [Fact]
        public void AccessByIndex()
        {
            var components = new [] { "a", "b", "c" };
            var path = FsPath.Parse(string.Join("/", components));
            Assert.Equal(components.Length, path.Count);
            for (var i = 0; i < path.Count; ++i)
            {
                Assert.Equal(components[i], path.Strings[i]);
            }
        }

        [Fact]
        public void ParseNull()
        {
            var path = FsPath.Parse(null);
            var count = path.Count;
            Assert.Equal(0, count);
        }

        [Fact]
        public void AddToNull()
        {
            FsPath nullPath = null;
            Assert.Throws<ArgumentNullException>(() => nullPath + "xxx");
        }

        [Fact]
        public void Equality()
        {
            FsPath nullPath = null;
            var emptyPath = FsPath.Empty;
            var aPath = FsPath.Parse("a");
            var bPath = FsPath.Parse("b");
            var aPath2 = FsPath.Parse("a");
            Assert.True(nullPath == null);
            Assert.False(nullPath != null);
            Assert.True(emptyPath != nullPath);
            Assert.True(emptyPath != aPath);
            Assert.True(emptyPath != bPath);
            Assert.True(null != aPath);
            Assert.True(bPath != aPath);
            Assert.False(aPath2 != aPath);
            // IEquatable
            Assert.False(emptyPath.Equals(nullPath));
            Assert.False(emptyPath.Equals(aPath));
            Assert.False(emptyPath.Equals(bPath));
            Assert.False(bPath.Equals(aPath));
            Assert.True(aPath2.Equals(aPath));
            // boxed
            Assert.False(emptyPath.Equals((object)nullPath));
            Assert.False(emptyPath.Equals((object)aPath));
            Assert.False(emptyPath.Equals((object)bPath));
            Assert.False(bPath.Equals((object)aPath));
            Assert.True(aPath2.Equals((object)aPath));
            Assert.False(aPath2.Equals((object)2));
            // GetHashCode
            Assert.Equal(emptyPath.GetHashCode(), emptyPath.GetHashCode()); // consistency
            Assert.Equal(aPath.GetHashCode(), aPath.GetHashCode()); // consistency
            Assert.Equal(aPath.GetHashCode(), aPath2.GetHashCode());
        }

        [Fact]
        public void Subpath()
        {
            var x = FsPath.Parse("x/y/z");
            var x0 = FsPath.Empty;
            var x1 = FsPath.Parse("x");
            var x2 = FsPath.Parse("x/y");
            var x3 = FsPath.Parse("x/y/z");
            Assert.Equal(x0, x.SubPath(0));
            Assert.Equal(x1, x.SubPath(1));
            Assert.Equal(x2, x.SubPath(2));
            Assert.Equal(x3, x.SubPath(3));
            Assert.Throws<InvalidOperationException>(() => x.SubPath(4));
            Assert.Throws<InvalidOperationException>(() => x.SubPath(-4));
            Assert.Equal(x0, x.SubPath(-3));
            Assert.Equal(x1, x.SubPath(-2));
            Assert.Equal(x2, x.SubPath(-1));
        }

        [Fact]
        public void ChangeName()
        {
            var orig = "xasd/aaa";
            var path = FsPath.Parse(orig).ChangeName("bbb");
            Assert.Equal("xasd/bbb", path.Join("/"));
            var xxxPath = FsPath.Empty.ChangeName("xxx");
        }

        [Fact]
        public void JoinEmpty()
        {
            Assert.Equal(string.Empty, FsPath.Empty.Join("/"));
        }
    }
}