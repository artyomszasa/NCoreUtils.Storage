using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NCoreUtils.ContentDetection;
using Xunit;

namespace NCoreUtils.Storage.Unit
{
    public class GoogleTests : GoogleTestsBase
    {
        readonly IReadOnlyList<string> _buckets;

        readonly ConcurrentDictionary<string, FakeGoogleClient.Entry> _entries;

        GoogleTests(List<string> buckets, ConcurrentDictionary<string, FakeGoogleClient.Entry> entries)
            : base("dummy", services => services
                .AddSingleton(new FakeGoogleClient(buckets, entries))
                .AddGoogleCloudStorageProvider<FakeGoogleProvider>("dummy"))
        {
            _buckets = buckets;
            _entries = entries;
        }

        public GoogleTests()
            : this(new List<string> { "dummy" }, new ConcurrentDictionary<string, FakeGoogleClient.Entry>())
        { }


        [Fact]
        public override void GetRoots() => base.GetRoots();

        [Fact]
        public override void CreateRecord() => base.CreateRecord();

        [Fact]
        public override void CreateRecordStream() => base.CreateRecordStream();

        [Fact]
        public override void CreateRecordWithoutMediaType() => base.CreateRecordWithoutMediaType();

        [Fact]
        public override void CreateRecordInSubfolder() => base.CreateRecordInSubfolder();

        [Fact]
        public override void CreateRecordInSubfolderWithDelete() => base.CreateRecordInSubfolderWithDelete();

        [Fact]
        public override void UploadAndRename() => base.UploadAndRename();

        [Fact]
        public override void Root() => base.Root();

        [Fact]
        public override void NonSeekableStream() => base.NonSeekableStream();

        [Fact]
        public override void Permissions() => base.Permissions();
    }
}