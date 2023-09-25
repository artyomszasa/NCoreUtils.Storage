using System;

namespace NCoreUtils.Storage
{
    [Flags]
    public enum StoragePermissions
    {
        None = 0,
        Read = 0x01,
        Write = 0x02,
        Execute = 0x04,
        Control = 0x08,

        Full = Read | Write | Execute | Control
    }
}