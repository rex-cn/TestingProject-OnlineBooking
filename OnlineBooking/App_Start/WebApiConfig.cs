using OnlineBooking.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;

namespace OnlineBooking
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "CancelBooking",
                routeTemplate: "api/{controller}/Cancel",
                defaults: new { ApiController = "RoomAction", Action = "Cancel" }
            );
            config.Routes.MapHttpRoute(
             name: "Booking",
             routeTemplate: "api/{controller}/Booking",
             defaults: new { ApiController = "RoomAction", Action = "Booking" }
         );
            config.Routes.MapHttpRoute(
            name: "GetAvailableRoom",
            routeTemplate: "api/{controller}/GetAvailableRoom",
            defaults: new { ApiController = "RoomAction", Action = "GetAvailableRoom" }
        );
            config.Routes.MapHttpRoute(
            name: "Login",
            routeTemplate: "api/{controller}/Login",
            defaults: new { ApiController = "SharedAction", Action = "Login" }
        );
        }
    }
}