using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NCoreUtils.ContentDetection;
using NCoreUtils.Features;
using NCoreUtils.Storage.GoogleCloudStorage;
using StorageClient = Google.Cloud.Storage.V1.StorageClient;

namespace NCoreUtils.Storage.Unit
{
    public class FakeGoogleProvider : GoogleCloudStorage.StorageProvider
    {
        readonly FakeGoogleClient _fakeClient;

        public FakeGoogleProvider(
            FakeGoogleClient fakeClient,
            ILogger<StorageProvider> logger,
            IFeatureCollection<IStorageProvider> features = null,
            IContentAnalyzer contentAnalyzer = null,
            GoogleCloudStorageOptions<FakeGoogleProvider> options = null) : base(logger, features, contentAnalyzer, options)
        {
            _fakeClient = fakeClient;
        }

        internal override ValueTask<StorageClient> GetPooledStorageClientAsync()
            => new ValueTask<StorageClient>(_fakeClient);

        internal override void ReturnPooledStorageClientAsync(StorageClient _) { }
    }
}