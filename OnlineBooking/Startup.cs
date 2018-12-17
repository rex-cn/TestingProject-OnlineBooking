using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(OnlineBooking.Startup))]
namespace OnlineBooking
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
           
        }
    }
}
