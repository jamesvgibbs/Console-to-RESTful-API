using System.Diagnostics;

namespace RalExtractorByDateRange.Models
{
    [DebuggerDisplay("SectionName = {SectionName}")]
    public class Section
    {
        public string CallId { get; set; }
        public string SectionId { get; set; }
        public string SectionName { get; set; }
    }
}