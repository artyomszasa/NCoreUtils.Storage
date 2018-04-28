using System;
using Google.Apis.Upload;
using NCoreUtils.Progress;

namespace NCoreUtils.Storage.GoogleCloudStorage
{
    sealed class GoogleProgressSource : IProgressReporter, IProgress<IUploadProgress>
    {
        decimal _total;

        decimal _value;

        public decimal Total
        {
            get => _total;
            set
            {
                _total = value;
                TotalChanged?.Invoke(this, new ProgressValueChangedArgs(value));
            }
        }
        public decimal Value
        {
            get => _value;
            set
            {
                _value = value;
                ValueChanged?.Invoke(this, new ProgressValueChangedArgs(value));
            }
        }

        public event EventHandler<ProgressValueChangedArgs> TotalChanged;
        public event EventHandler<ProgressValueChangedArgs> ValueChanged;

        public void Report(IUploadProgress value)
        {
            Value = value.Status == UploadStatus.Completed ? Total : value.BytesSent;
        }
    }
}