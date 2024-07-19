namespace NewsAggregation.Services
{
    public class QueryParameters
    {
        public int Page { get; set; } = 1;
        public int PerPage { get; set; } = 10;
        public int[] Range { get; set; }
        public string SortField { get; set; } = "id";
        public string SortOrder { get; set; } = "DESC";
    }

    public static class ParameterParser
    {
        public static QueryParameters ParseRangeAndSort(string range, string sort)
        {
            var parameters = new QueryParameters();

            // Parse the range query parameter
            if (!string.IsNullOrEmpty(range))
            {
                string[] rangeParts = range.Trim('[', ']').Split(',');
                if (rangeParts.Length == 2 && int.TryParse(rangeParts[0], out int start) && int.TryParse(rangeParts[1], out int end))
                {
                    if (start <= end)
                    {
                        parameters.Range = new int[] { start, end };
                        parameters.PerPage = end - start + 1;
                        parameters.Page = start / parameters.PerPage + 1;
                    }
                }
            }

            // Parse the sort query parameter
            if (!string.IsNullOrEmpty(sort))
            {
                string[] sortParts = sort.Trim('[', ']').Split(',');
                if (sortParts.Length == 2)
                {
                    parameters.SortField = sortParts[0].Trim('"');
                    parameters.SortOrder = sortParts[1].Trim('"').ToUpper();
                }
            }

            // If they are not valid, reset to default values

            if (parameters.Range == null || parameters.Range[0] < 0 || parameters.Range[1] < 0 ||
                parameters.PerPage < 1 || parameters.Page < 1)
            {
                parameters.Range = null;
                parameters.PerPage = 10;
                parameters.Page = 1;
            }

            return parameters;
        }
    }

}
