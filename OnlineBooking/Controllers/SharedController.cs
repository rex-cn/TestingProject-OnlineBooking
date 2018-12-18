using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.SessionState;

namespace OnlineBooking.Controllers
{
    public class SharedActionController : ApiController
    {
        OnlineBookingDataAccess.Shared shared = new OnlineBookingDataAccess.Shared();
        // GET: api/Shared
        [System.Web.Mvc.HttpPost]
        [System.Web.Mvc.HttpGet]
        [System.Web.Http.HttpGet]
        [System.Web.Http.HttpPost]
        public int Login()
        {
            var userName = System.Web.HttpContext.Current.Request.Form["userName"].ToString();
            var encryptedPwd = System.Web.HttpContext.Current.Request.Form["encryptedPwd"].ToString();
            if (userName != "" && encryptedPwd != "")
            {
                var loginSuc = shared.Login(userName, encryptedPwd);
                if (loginSuc != null)
                {
                    //System.Web.HttpContext.Current.Session.Add("LoginUser", loginSuc);
                    return 0;
                }
            }
            return 1;
        }
    }

    public class SharedController : Controller
    {
        // GET: api/Shared
        public ActionResult Login(string userName, string encryptedPwd)
        {
            return View();
        }
    }
}
