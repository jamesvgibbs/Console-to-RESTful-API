using System.Diagnostics;

namespace RalExtractorByDateRange.Models
{
    [DebuggerDisplay("BucketFullName = {BucketFullName}")]
    public class Category
    {
        public string CallId { get; set; }
        public string BucketId { get; set; }
        public string SectionId { get; set; }
        public string BucketFullName { get; set; }
        public string Weight { get; set; }
    }
}