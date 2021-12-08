using System;

namespace sc2dsstats._2022.Shared
{
    public class DsUploadInfo
    {
        public string Name { get; set; }
        public string Json { get; set; }
        public int Total { get; set; }
        public DateTime LastUpload { get; set; }
        public string LastRep { get; set; }
        public string Version { get; set; }
    }

    public class DsUploadRequest
    {
        public string RealName { get; set; }
        public string Hash { get; set; }
        public Guid AppId { get; set; }
        public int Total { get; set; }
        public DateTime LastUpload { get; set; }
        public string LastRep { get; set; }
        public string Version { get; set; }
    }

    public class DsUploadResponse
    {
        public Guid DbId { get; set; }
    }
}
