namespace NewsAggregation.Helpers
{
    public static class IP_Address
    {
        public static string GetUserIp()
        {
            var httpContex = new HttpContextAccessor().HttpContext;
            var forwardedHeader = httpContex.Request.Headers["X-Forwarded-For"].FirstOrDefault();

            if (!string.IsNullOrEmpty(forwardedHeader))
            {
                var ips = forwardedHeader.Split(',');
                return ips.FirstOrDefault();
            }

            return httpContex.Connection.RemoteIpAddress.ToString();
        }

    }
}
