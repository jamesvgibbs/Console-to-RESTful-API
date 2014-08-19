using System.Diagnostics;

namespace RalExtractorByDateRange.Models
{
    [DebuggerDisplay("Id = {Id}")]
    public class Contact
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public string WavPath { get; set; }
    }
}