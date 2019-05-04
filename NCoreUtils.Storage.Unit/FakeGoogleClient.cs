using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.Api.Gax;
using Google.Apis.Download;
using Google.Apis.Upload;
using Google.Cloud.Storage.V1;
using Bucket = Google.Apis.Storage.v1.Data.Bucket;
using Buckets = Google.Apis.Storage.v1.Data.Buckets;
using Object = Google.Apis.Storage.v1.Data.Object;
using Objects = Google.Apis.Storage.v1.Data.Objects;

namespace NCoreUtils.Storage.Unit
{
    public class FakeGoogleClient : StorageClient
    {
        sealed class FakePagedEnumerable<TResponse, TResource> : PagedEnumerable<TResponse, TResource>
        {
            readonly TResponse[] _responses;

            readonly Func<TResponse, IEnumerable<TResource>> _extract;

            public FakePagedEnumerable(TResponse[] responses, Func<TResponse, IEnumerable<TResource>> extract)
            {
                _responses = responses;
                _extract = extract;
            }

            public override IEnumerable<TResponse> AsRawResponses() => _responses;

            public override IEnumerator<TResource> GetEnumerator() => _responses.SelectMany(_extract).GetEnumerator();

            public override Page<TResource> ReadPage(int pageSize) => new Page<TResource>(this.ToArray(), null);
        }

        sealed class FakePagedAsyncEnumerable<TResponse, TResource> : PagedAsyncEnumerable<TResponse, TResource>
        {
            readonly TResponse[] _responses;

            readonly Func<TResponse, IEnumerable<TResource>> _extract;

            public FakePagedAsyncEnumerable(TResponse[] responses, Func<TResponse, IEnumerable<TResource>> extract)
            {
                _responses = responses;
                _extract = extract;
            }

            public override IAsyncEnumerable<TResponse> AsRawResponses() => _responses.ToAsyncEnumerable();

            public override IAsyncEnumerator<TResource> GetEnumerator() => _responses.SelectMany(_extract).ToAsyncEnumerable().GetEnumerator();

            public override Task<Page<TResource>> ReadPageAsync(int pageSize, CancellationToken cancellationToken = default(CancellationToken))
                => Task.FromResult(new Page<TResource>(_responses.SelectMany(_extract).ToArray(), null));
        }

        public sealed class Entry
        {
            public string Name { get; }
            public byte[] Data { get; }
            public string MediaType { get; }

            public Entry(string name, string mediaType, byte[] data)
            {
                Name = name;
                MediaType = mediaType;
                Data = data;
            }

            public Object ToObject(string bucketName)
            {
                return new Object
                {
                    Name = Name,
                    ContentType = MediaType,
                    Size = (ulong)Data.Length,
                    Bucket = bucketName
                };
            }
        }

        public static string Key(string bucket, string objectName) => $"{bucket}${objectName}";

        readonly object _syncBuckets = new object();

        readonly List<string> _buckets;

        readonly ConcurrentDictionary<string, Entry> _entries;

        public FakeGoogleClient(List<string> buckets, ConcurrentDictionary<string, Entry> entries)
        {
            _buckets = buckets ?? throw new ArgumentNullException(nameof(buckets));
            _entries = entries ?? throw new ArgumentNullException(nameof(entries));
        }

        // *************************************************************************************************************
        // GET OBJECT

        public override Object CopyObject(
            string sourceBucket,
            string sourceObjectName,
            string destinationBucket,
            string destinationObjectName,
            CopyObjectOptions options = null)
        {
            var sourceKey = Key(sourceBucket, sourceObjectName);
            var targetkey = Key(destinationBucket, destinationObjectName);
            if (_entries.TryGetValue(sourceKey, out var source))
            {
                var entry = new Entry(destinationObjectName, source.MediaType, source.Data);
                _entries.AddOrUpdate(targetkey, entry, (_, __) => entry);
                return entry.ToObject(destinationBucket);
            }
            else
            {
                // FIXME: Google compatible exception
                throw new InvalidOperationException($"No object found for {sourceKey}.");
            }
        }

        public override Task<Object> CopyObjectAsync(
            string sourceBucket,
            string sourceObjectName,
            string destinationBucket = null,
            string destinationObjectName = null,
            CopyObjectOptions options = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.Run(() => CopyObject(sourceBucket, sourceObjectName, destinationBucket, destinationObjectName, options));
        }

        // *************************************************************************************************************
        // DELETE OBJECT

        public override void DeleteObject(string bucket, string objectName, DeleteObjectOptions options = null)
        {
            var key = Key(bucket, objectName);
            if (_entries.TryRemove(key, out var _))
            {
                return;
            }
            // FIXME: Google compatible exception
            throw new InvalidOperationException($"No object found for {key}.");
        }

        public override void DeleteObject(Object obj, DeleteObjectOptions options = null)
        {
            DeleteObject(obj.Bucket, obj.Name, options);
        }

        public override Task DeleteObjectAsync(
            string bucket,
            string objectName,
            DeleteObjectOptions options = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.Run(() => DeleteObject(bucket, objectName, options));
        }

        public override Task DeleteObjectAsync(Object obj, DeleteObjectOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.Run(() => DeleteObject(obj, options));
        }

        // *************************************************************************************************************
        // DOWNLOAD OBJECT

        public override void DownloadObject(
            string bucket,
            string objectName,
            Stream destination,
            DownloadObjectOptions options = null,
            IProgress<IDownloadProgress> progress = null)
        {
            var key = Key(bucket, objectName);
            if (_entries.TryGetValue(key, out var entry))
            {
                destination.Write(entry.Data, 0, entry.Data.Length);
                destination.Flush();
                return;
            }
            // FIXME: Google compatible exception
            throw new InvalidOperationException($"No object found for {key}.");
        }

        public override void DownloadObject(
            Object source,
            Stream destination,
            DownloadObjectOptions options = null,
            IProgress<IDownloadProgress> progress = null)
        {
            DownloadObject(source.Bucket, source.Name, destination, options, progress);
        }

        public override Task DownloadObjectAsync(
            string bucket,
            string objectName,
            Stream destination,
            DownloadObjectOptions options = null,
            CancellationToken cancellationToken = default(CancellationToken),
            IProgress<IDownloadProgress> progress = null)
        {
            return Task.Run(() => DownloadObject(bucket, objectName, destination, options, progress));
        }

        public override Task DownloadObjectAsync(
            Object source,
            Stream destination,
            DownloadObjectOptions options = null,
            CancellationToken cancellationToken = default(CancellationToken),
            IProgress<IDownloadProgress> progress = null)
        {
            return Task.Run(() => DownloadObject(source, destination, options, progress));
        }

        // *************************************************************************************************************
        // GET BUCKET

        public override Bucket GetBucket(string bucket, GetBucketOptions options = null)
        {
            lock (_syncBuckets)
            {
                if (!_buckets.Contains(bucket))
                {
                    // FIXME: Google compatible exception
                    throw new InvalidOperationException($"No bucket found for {bucket}.");
                }
            }
            return new Bucket
            {
                Name = bucket
            };
        }

        public override Task<Bucket> GetBucketAsync(
            string bucket,
            GetBucketOptions options = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.Run(() => GetBucket(bucket, options));
        }

        // *************************************************************************************************************
        // GET OBJECT


        public override Object GetObject(string bucket, string objectName, GetObjectOptions options = null)
        {
            var key = Key(bucket, objectName);
            if (_entries.TryGetValue(key, out var entry))
            {
                return entry.ToObject(bucket);
            }
            // FIXME: Google compatible exception
            throw new InvalidOperationException($"No object found for {key}.");
        }

        public override Task<Object> GetObjectAsync(
            string bucket,
            string objectName,
            GetObjectOptions options = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.Run(() => GetObject(bucket, objectName, options));
        }

        // *************************************************************************************************************
        // LIST BUCKETS

        public override PagedAsyncEnumerable<Buckets, Bucket> ListBucketsAsync(string projectId, ListBucketsOptions options = null)
        {
            var buckets = new Buckets
            {
                Items = _buckets.Select(b => new Bucket { Name = b }).ToArray()
            };
            return new FakePagedAsyncEnumerable<Buckets, Bucket>(new [] { buckets }, bs => bs.Items);
        }

        public override PagedEnumerable<Buckets, Bucket> ListBuckets(string projectId, ListBucketsOptions options = null)
        {
            var buckets = new Buckets
            {
                Items = _buckets.Select(b => new Bucket { Name = b }).ToArray()
            };
            return new FakePagedEnumerable<Buckets, Bucket>(new [] { buckets }, bs => bs.Items);
        }

        // *************************************************************************************************************
        // LIST OBJECTS

        private Objects ListObjectsInternal(string bucket, string prefix, ListObjectsOptions options)
        {
            var prefixes = new List<string>();
            var entries = new List<Entry>();
            foreach (var kv in _entries)
            {
                var key = kv.Key;
                var i = key.IndexOf('$');
                var buck = key.Substring(0, i);
                var nam = key.Substring(i + 1);
                if (buck == bucket && (null == prefix || nam.StartsWith(prefix)))
                {
                    var suffix = null == prefix ? nam : nam.Substring(prefix.Length).TrimStart('/');
                    if (options?.Delimiter == null || -1 == suffix.IndexOf(options?.Delimiter))
                    {
                        entries.Add(kv.Value);
                    }
                    else
                    {
                        var j = suffix.IndexOf(options?.Delimiter);
                        prefixes.Add(nam.Substring(prefix.Length + j + 1));
                    }
                }
            }
            return new Objects
            {
                Items = entries.Select(entry => entry.ToObject(bucket)).ToArray(),
                Prefixes = prefixes
            };
        }

        public override PagedAsyncEnumerable<Objects, Object> ListObjectsAsync(
            string bucket,
            string prefix = null,
            ListObjectsOptions options = null)
        {
            var objects = ListObjectsInternal(bucket, prefix, options);
            return new FakePagedAsyncEnumerable<Objects, Object>(new [] { objects }, os => os.Items);
        }

        public override PagedEnumerable<Objects, Object> ListObjects(string bucket, string prefix = null, ListObjectsOptions options = null)
        {
            var objects = ListObjectsInternal(bucket, prefix, options);
            return new FakePagedEnumerable<Objects, Object>(new [] { objects }, os => os.Items);
        }

        // *************************************************************************************************************
        // PATCH OBJECT


        public override Object PatchObject(
            Object obj,
            PatchObjectOptions options = null)
        {
            return obj;
        }

        public override Task<Object> PatchObjectAsync(
            Object obj,
            PatchObjectOptions options = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(obj);
        }

        // *************************************************************************************************************
        // UPDATE OBJECT

        public override Object UpdateObject(
            Object obj,
            UpdateObjectOptions options = null)
        {
            return obj;
        }

        public override Task<Object> UpdateObjectAsync(
            Object obj,
            UpdateObjectOptions options = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(obj);
        }

        // *************************************************************************************************************
        // UPLOAD OBJECT

        public override Object UploadObject(
            string bucket,
            string objectName,
            string contentType,
            Stream source,
            UploadObjectOptions options = null,
            IProgress<IUploadProgress> progress = null)
        {
            var key = Key(bucket, objectName);
            byte[] data;
            using (var buffer = new MemoryStream())
            {
                source.CopyTo(buffer);
                buffer.Flush();
                data = buffer.ToArray();
            }
            var entry = new Entry(objectName, contentType ?? "application/octet-stream", data);
            _entries.AddOrUpdate(key, entry, (_, __) => entry);
            return entry.ToObject(bucket);
        }

        public override Task<Object> UploadObjectAsync(
            string bucket,
            string objectName,
            string contentType,
            Stream source,
            UploadObjectOptions options = null,
            CancellationToken cancellationToken = default(CancellationToken),
            IProgress<IUploadProgress> progress = null)
        {
            return Task.Run(() => UploadObject(bucket, objectName, contentType, source, options, progress));
        }

        public override Object UploadObject(
            Object destination,
            Stream source,
            UploadObjectOptions options = null,
            IProgress<IUploadProgress> progress = null)
        {
            return UploadObject(destination.Bucket, destination.Name, destination.ContentType, source, options, progress);
        }

        public override Task<Object> UploadObjectAsync(
            Object destination,
            Stream source,
            UploadObjectOptions options = null,
            CancellationToken cancellationToken = default(CancellationToken),
            IProgress<IUploadProgress> progress = null)
        {
            return Task.Run(() => UploadObject(destination, source, options, progress));
        }
    }
}