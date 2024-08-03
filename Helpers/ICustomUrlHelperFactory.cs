using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc;

namespace NewsAggregation.Helpers
{
    public interface ICustomUrlHelperFactory
    {
        IUrlHelper GetUrlHelper(ActionContext actionContext);
    }

    public class CustomUrlHelperFactory : ICustomUrlHelperFactory
    {
        private readonly IUrlHelperFactory _urlHelperFactory;

        public CustomUrlHelperFactory(IUrlHelperFactory urlHelperFactory)
        {
            _urlHelperFactory = urlHelperFactory;
        }

        public IUrlHelper GetUrlHelper(ActionContext actionContext)
        {
            return _urlHelperFactory.GetUrlHelper(actionContext);
        }
    }

}
