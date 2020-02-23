using System;
using System.Collections.Generic;
using System.Text;

namespace sc2dsstats.lib.Models
{
    public class DesktopStatus
    {
        public int DatabaseReplays { get; set; } = 0;
        public int FoldersReplays { get; set; } = 0;
        public int NewReplays { get; set; } = 0;
        public bool Decoding { get; set; } = false;
        public bool Scanning { get; set; } = false;
        public UploadStatus UploadStatus { get; set; } = UploadStatus.UploadDone;
    }

    public enum UploadStatus
    {
        Uploading,
        UploadSuccess,
        UploadFailed,
        UploadDone
    }
}
