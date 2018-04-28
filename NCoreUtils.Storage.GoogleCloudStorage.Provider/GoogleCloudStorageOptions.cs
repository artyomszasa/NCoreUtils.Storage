using System;
using System.Collections.Generic;
using System.Net.Http;
using Google.Cloud.Storage.V1;

namespace NCoreUtils.Storage
{
    public class GoogleCloudStorageOptions
    {
        const string MetadataEndPoint = "http://metadata.google.internal/computeMetadata/v1/project/project-id";
        static readonly object _sync = new object();
        static GoogleCloudStorageOptions _default;

        public static GoogleCloudStorageOptions Default
        {
            get
            {
                if (null == _default)
                {
                    lock (_sync)
                    {
                        if (null == _default)
                        {
                            using (var client = new HttpClient())
                            {
                                try
                                {
                                    var projectId = client.GetStringAsync(MetadataEndPoint).GetAwaiter().GetResult();
                                    var builder = new GoogleCloudStorageOptionsBuilder(projectId);
                                    builder.PredefinedAcl = PredefinedObjectAcl.PublicRead;
                                    _default = new GoogleCloudStorageOptions(builder);
                                }
                                catch (Exception exn)
                                {
                                    throw new InvalidOperationException("Unable to get project id from environemnt.", exn);
                                }
                            }
                        }
                    }
                }
                return _default;
            }
        }

        public string ProjectId { get; }
        public int? ChunkSize { get; }
        public PredefinedObjectAcl? PredefinedAcl { get; }
        public IReadOnlyDictionary<string, string> DefaultMetadata { get; }
        public string DefaultCacheControl { get; }
        public string DefaultContentDisposition { get; }
        public string DefaultContentEncoding { get; }
        public string DefaultContentLanguage { get; }

        public GoogleCloudStorageOptions(GoogleCloudStorageOptionsBuilder builder)
        {
            ProjectId = builder.ProjectId;
            ChunkSize = builder.ChunkSize;
            PredefinedAcl = builder.PredefinedAcl;
            DefaultMetadata = builder.DefaultMetadata.ToImmutable();
            DefaultCacheControl = builder.DefaultCacheControl;
            DefaultContentDisposition = builder.DefaultContentDisposition;
            DefaultContentEncoding = builder.DefaultContentEncoding;
            DefaultContentLanguage = builder.DefaultContentLanguage;
        }
    }

    public sealed class GoogleCloudStorageOptions<TProvider> : GoogleCloudStorageOptions
        where TProvider : IStorageProvider
    {
        public GoogleCloudStorageOptions(GoogleCloudStorageOptionsBuilder<TProvider> builder) : base(builder) { }
    }
}