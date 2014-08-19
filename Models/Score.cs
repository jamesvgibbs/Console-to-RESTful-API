using System.Diagnostics;

namespace RalExtractorByDateRange.Models
{
    [DebuggerDisplay("ScoreName = {ScoreName}")]
    public class Score
    {
        public string CallId { get; set; }
        public string ScoreName { get; set; }
        public string ScoreId { get; set; }
        public decimal? Weight { get; set; }
    }
}