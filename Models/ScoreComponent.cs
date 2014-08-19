using System.Diagnostics;

namespace RalExtractorByDateRange.Models
{
    [DebuggerDisplay("ScoreComponentName = {ScoreComponentName}")]
    public class ScoreComponent
    {
        public string CallId { get; set; }
        public string ScoreComponentId { get; set; }
        public string ScoreComponentName { get; set; }
        public string ScoreId { get; set; }
        public decimal? Weight { get; set; }
    }
}