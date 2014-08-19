using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RalExtractorByDateRange
{
    public class Helper
    {
        public const string SecurityApiPart = "security/getToken";
        public const string RalDateSearchApiPart = "ral/datesearch?startDate={0}&stopDate={1}&records={2}&page={3}";
        public const string ContactsByDateRangeApiPart = "contacts?startDate={0}&stopDate={1}&records={2}&page={3}";
        public const string TranscriptApiPart = "transcript/fulltext?contactsIds={0}";
    }
}
