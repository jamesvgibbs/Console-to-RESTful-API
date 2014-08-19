using System.Collections.Generic;

namespace RalExtractorByDateRange.Models
{
    public class Ral
    {
        public Contact Contact { get; set; }
        public RalExtractorByDateRange.Models.Attributes Attributes { get; set; }
        public Measures Measures { get; set; }
        public List<Section> Sections { get; set; }
        public List<Category> Categories { get; set; }
        public List<Score> Scores { get; set; }
        public List<ScoreComponent> ScoreComponents { get; set; }
    }
}