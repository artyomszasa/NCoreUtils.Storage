using System;

namespace NCoreUtils.Storage.FileSystem
{
    public class WinDriveStorageRoot : StorageProvider, IStorageRoot
    {
        public string DriveLetter { get; }

        public WinDriveStorageRoot(FileSystemStorageDriver driver, string driveLetter)
            : base(driver, $"{driveLetter}:\\", $"/{driveLetter}/", '\\')
        {
            DriveLetter = driveLetter ?? throw new ArgumentNullException(nameof(driveLetter));
        }
    }
}