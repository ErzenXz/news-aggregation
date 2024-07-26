namespace NewsAggregation.Helpers
{
    public static class IP_Address
    {
        public static string GetUserIp()
        {
            var httpContex = new HttpContextAccessor().HttpContext;
            // Get the value from the X-Forwarded-For header
            var forwardedHeader = httpContex.Request.Headers["X-Forwarded-For"].FirstOrDefault();

            // Check if the header contains multiple IP addresses
            if (!string.IsNullOrEmpty(forwardedHeader))
            {
                // Split the header value by comma to get a list of IP addresses
                var ips = forwardedHeader.Split(',');

                // Return the first IP address which is the client's original IP
                return ips.FirstOrDefault();
            }

            // If there is no X-Forwarded-For header, fall back to the RemoteIpAddress
            return httpContex.Connection.RemoteIpAddress.ToString();
        }

    }
}
