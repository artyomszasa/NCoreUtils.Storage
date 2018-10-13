using Microsoft.Extensions.DependencyInjection;
using NCoreUtils.Storage.Unit;
using Xunit;

namespace NCoreUtils.Storage.Integration
{
    public class GoogleLiveTests : GoogleTestsBase
    {
        public GoogleLiveTests()
            : base("ncoreutils-storage-test", services => services
                .AddGoogleCloudStorageProvider("artyom-2017", b =>
                {
                    b.ChunkSize = 262144;
                    b.PredefinedAcl = null;
                    b.DefaultCacheControl = "max-age=2628000, public";
                }))
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
    }
}