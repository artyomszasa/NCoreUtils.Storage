using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.Cloud.Storage.V1;
using NCoreUtils.Progress;
using GoogleObject = Google.Apis.Storage.v1.Data.Object;


namespace NCoreUtils.Storage.GoogleCloudStorage
{
    public class StorageRecord : StorageItem, IStorageRecord
    {
        IStorageSecurity _security;

        public GoogleObject GoogleObject { get; internal set; }

        public long Size => (long)(GoogleObject.Size ?? 0ul);

        public string MediaType => GoogleObject.ContentType;

        public override IStorageSecurity Security => _security;

        public StorageRecord(StorageRoot storageRoot, string localPath, GoogleObject googleObject) : base(storageRoot, localPath)
        {
            GoogleObject = googleObject ?? throw new System.ArgumentNullException(nameof(googleObject));
            _security = storageRoot.GetPermissions(GoogleObject);
        }

        async Task<IStorageRecord> IStorageRecord.RenameAsync(string name, IProgress progress, CancellationToken cancellationToken)
            => await RenameAsync(name, progress, cancellationToken).ConfigureAwait(false);

        public Task<Stream> CreateReadableStreamAsync(CancellationToken cancellationToken)
        {
            return StorageRoot.CreateReadableStreamAsync(this, cancellationToken);
        }

        public Task<StorageRecord> RenameAsync(string name, IProgress progress, CancellationToken cancellationToken)
        {
            return StorageRoot.RenameAsync(this, name, progress, cancellationToken);
        }

        public Task UpdateContentAsync(Stream contents, string contentType, IProgress progress, CancellationToken cancellationToken)
        {
            return StorageRoot.UpdateContentAsync(this, contents, contentType, progress, cancellationToken);
        }

        public override async Task UpdateSecurityAsync(IStorageSecurity security, IProgress progress = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                if (null != progress)
                {
                    progress.Total = 1;
                    progress.Value = 0;
                }
                GoogleObject = await StorageRoot.UpdateObjectAclAsync(GoogleObject, StorageRoot.CreateObjectAccessControlList(security).ToList(), cancellationToken);
                _security = security;
            }
            finally
            {
                if (null != progress)
                {
                    progress.Value = 1;
                }
            }
        }
    }
}