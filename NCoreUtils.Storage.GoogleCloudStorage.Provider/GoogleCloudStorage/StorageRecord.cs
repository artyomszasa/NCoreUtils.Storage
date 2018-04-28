using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Google.Cloud.Storage.V1;
using NCoreUtils.Progress;
using GoogleObject = Google.Apis.Storage.v1.Data.Object;


namespace NCoreUtils.Storage.GoogleCloudStorage
{
    public class StorageRecord : StorageItem, IStorageRecord
    {
        const long MaxBufferSize = 50 * 1024 * 1024; // 50 MB

        public GoogleObject GoogleObject { get; private set; }

        public long Size => (long)(GoogleObject.Size ?? 0ul);

        public string MediaType => GoogleObject.ContentType;

        public StorageRecord(StorageRoot storageRoot, string localPath, GoogleObject googleObject) : base(storageRoot, localPath)
        {
            GoogleObject = googleObject ?? throw new System.ArgumentNullException(nameof(googleObject));
        }

        public override async Task DeleteAsync(IProgress progress, CancellationToken cancellationToken)
        {
            progress.SetTotal(1);
            var client = await StorageClient.CreateAsync().ConfigureAwait(false);
            await client.DeleteObjectAsync(GoogleObject).ConfigureAwait(false);
            progress.SetValue(1);
        }

        async Task<IStorageRecord> IStorageRecord.RenameAsync(string name, IProgress progress, CancellationToken cancellationToken)
            => await RenameAsync(name, progress, cancellationToken).ConfigureAwait(false);

        public async Task<Stream> CreateReadableStreamAsync(CancellationToken cancellationToken)
        {
            // TODO: avoid using large in-memory buffers...
            Stream buffer;
            if (Size > MaxBufferSize)
            {
                buffer = new FileStream(Path.GetTempFileName(), FileMode.Create, FileAccess.ReadWrite, FileShare.Read, 8091, FileOptions.DeleteOnClose);
            }
            else
            {
                buffer = new MemoryStream((int)Size);
            }
            var client = await StorageClient.CreateAsync().ConfigureAwait(false);
            await client.DownloadObjectAsync(GoogleObject, buffer, cancellationToken: cancellationToken).ConfigureAwait(false);
            buffer.Seek(0, SeekOrigin.Begin);
            return buffer;
        }

        public async Task<StorageRecord> RenameAsync(string name, IProgress progress, CancellationToken cancellationToken)
        {
            progress.SetTotal(3);
            progress.SetValue(0);
            var client = await StorageClient.CreateAsync().ConfigureAwait(false);
            var slashIndex = LocalPath.LastIndexOf('/');
            var folder = -1 == slashIndex ? "" : LocalPath.Substring(0, slashIndex + 1);
            var newPath = folder + name;
            progress.SetValue(1);
            var gobj = await client.CopyObjectAsync(StorageRoot.BucketName, LocalPath, StorageRoot.BucketName, newPath, cancellationToken: cancellationToken).ConfigureAwait(false);
            progress.SetValue(2);
            await client.DeleteObjectAsync(GoogleObject).ConfigureAwait(false);
            progress.SetValue(3);
            return new StorageRecord(StorageRoot, newPath, gobj);
        }

        public async Task UpdateContentAsync(Stream contents, IProgress progress, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            GoogleProgressSource uploadProgress = null;
            ProgressReporter miscProgress = null;
            if (null != progress)
            {
                uploadProgress = new GoogleProgressSource();
                miscProgress = new NCoreUtils.Progress.ProgressReporter();
                new SummaryProgress(progress, uploadProgress, miscProgress);
            }
            miscProgress.SetTotal(2);
            miscProgress.SetValue(0);
            uploadProgress.SetTotal(contents.Length);
            uploadProgress.SetValue(0);
            string mediaType = await StorageRoot.StorageProvider.GetMediaTypeAsync(contents, Name, cancellationToken).ConfigureAwait(false);
            miscProgress.SetValue(1);
            var options = new UploadObjectOptions
            {
                ChunkSize = StorageRoot.StorageProvider.Options.ChunkSize,
                PredefinedAcl = StorageRoot.StorageProvider.Options.PredefinedAcl
            };
            GoogleObject.Size = (ulong)contents.Length;
            GoogleObject.Crc32c = null;
            GoogleObject.ComponentCount = null;
            GoogleObject.ETag = null;
            GoogleObject.Md5Hash = null;
            GoogleObject.ContentType = mediaType ?? "application/octet-stream";
            var client = await StorageClient.CreateAsync().ConfigureAwait(false);
            GoogleObject = await client.UploadObjectAsync(GoogleObject, contents, options, cancellationToken, uploadProgress).ConfigureAwait(false);
            miscProgress.SetValue(2);
        }
    }
}