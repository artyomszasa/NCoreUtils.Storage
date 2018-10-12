using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NCoreUtils.Progress;
using NCoreUtils.Storage.Features;

namespace NCoreUtils.Storage.GoogleCloudStorage
{
    class RecordCopyFeature : IRecordCopyFeature
    {
        public async Task CopyToAsync(
            IStorageRecord record,
            Stream destination,
            int? bufferSize = null,
            IProgress progress = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (record is StorageRecord googleStorageRecord)
            {
                await googleStorageRecord.StorageRoot.DownloadRecordAsync(googleStorageRecord, destination, bufferSize, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                throw new InvalidOperationException($"Invalid storage record of type {record.GetType()}");
            }
        }
    }
}