using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using OnlineBookingModel;
using OnlineBookingDataAccess;
using System.Web.Mvc;

namespace OnlineBooking.Controllers
{
    public class RoomController : Controller
    {
        Rooms rooms = new Rooms();

        // GET: api/RoomList
        public ActionResult RoomList()
        {
            return View(rooms.GetRoomList());
        }

        // GET: api/Room/5
        public ActionResult RoomDetail(int id)
        {
            return View(rooms.GetRoom(id));
        }


        // GET: api/Room
        public ActionResult BookingList()
        {
            return View(rooms.GetBookingList(DateTime.Today, DateTime.Today.AddDays(30), ""));
        }

    }

    public class RoomActionController : ApiController
    {
        Rooms rooms = new Rooms();
        // POST: api/Cancel/id
        [System.Web.Mvc.HttpPost]
        [System.Web.Mvc.HttpGet]
        [System.Web.Http.HttpGet]
        [System.Web.Http.HttpPost]
        public int Cancel()
        {
            var bookId = int.Parse(System.Web.HttpContext.Current.Request.Form["bookId"]);
            return rooms.CancelBooking(bookId, 0) ? 0 : 1;
        }

        // PUT: api/Room/5
        [System.Web.Mvc.HttpPost]
        [System.Web.Mvc.HttpGet]
        [System.Web.Http.HttpGet]
        [System.Web.Http.HttpPost]
        public string Booking( )
        {
            var outMsg = "";
            var roomId = int.Parse(System.Web.HttpContext.Current.Request.Form["roomId"]);
            var customerName =  System.Web.HttpContext.Current.Request.Form["customerName"].ToString();
            var customerMobile =  System.Web.HttpContext.Current.Request.Form["customerMobile"].ToString();
            var customerEmail = System.Web.HttpContext.Current.Request.Form["customerEmail"].ToString();
            var checkinDate = DateTime.Parse(System.Web.HttpContext.Current.Request.Form["checkinDate"]);
            var checkoutDate = DateTime.Parse(System.Web.HttpContext.Current.Request.Form["checkoutDate"]);
            var result = rooms.Booking(roomId, customerName, customerMobile, customerEmail, checkinDate, checkoutDate, 0, out outMsg);
            return Newtonsoft.Json.JsonConvert.SerializeObject(new ReturnResult { ReturnValue = result ? 0 : 1, ReturnMessage = outMsg });
        }

        // PUT: api/Room/5
        [System.Web.Mvc.HttpPost]
        [System.Web.Mvc.HttpGet]
        [System.Web.Http.HttpGet]
        [System.Web.Http.HttpPost]
        public string GetAvailableRoom()
        {
            var checkinDate = DateTime.Parse(System.Web.HttpContext.Current.Request.Form["checkinDate"]);
            var checkoutDate = DateTime.Parse(System.Web.HttpContext.Current.Request.Form["checkoutDate"]);
            return Newtonsoft.Json.JsonConvert.SerializeObject(rooms.GetAvailableRoom(checkinDate, checkoutDate));
        }
    }    
}