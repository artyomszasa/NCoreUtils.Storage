using System;
using System.Collections.Immutable;
using Google.Cloud.Storage.V1;

namespace NCoreUtils.Storage
{
    public class GoogleCloudStorageOptionsBuilder
    {
        string _projectId;
        public string ProjectId
        {
            get => _projectId;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new InvalidOperationException("Project ID must be a non-empty string.");
                }
                _projectId = value;
            }
        }
        public int? ChunkSize { get; set; }
        public PredefinedObjectAcl? PredefinedAcl { get; set; }
        public string DefaultCacheControl { get; set; }
        public string DefaultContentDisposition { get; set; }
        public string DefaultContentEncoding { get; set; }
        public string DefaultContentLanguage { get; set; }
        public ImmutableDictionary<string, string>.Builder DefaultMetadata { get; } = ImmutableDictionary.CreateBuilder<string, string>();

        public GoogleCloudStorageOptionsBuilder(string projectId)
        {
            if (string.IsNullOrWhiteSpace(projectId))
            {
                throw new ArgumentException("Project ID must be a non-empty string", nameof(projectId));
            }
            ProjectId = projectId;
        }
    }

    public sealed class GoogleCloudStorageOptionsBuilder<TProvider> : GoogleCloudStorageOptionsBuilder
        where TProvider : IStorageProvider
    {
        public GoogleCloudStorageOptionsBuilder(string projectId) : base(projectId) { }
    }
}