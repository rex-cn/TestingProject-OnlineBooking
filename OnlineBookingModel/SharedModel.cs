using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnlineBookingModel.Shared
{
    public class LoginModel
    {
        public string UserName;
        public string EncryptPassword;            
    }

    public class StaffModel
    {
        public string UserName;
        public string StaffName;
        public int StaffId;
        public string StaffMobile;
        public bool IsManager;
        public DateTime LoginTime;
    }
}
