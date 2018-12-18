using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OnlineBookingSupport.DBUtility;
using System.Data.SqlClient;

namespace OnlineBookingDataAccess
{
    public class Shared
    {
        public OnlineBookingModel.Shared.StaffModel Login(string userName, string encyptedPwd)
        {
            var staffTable = SqlHelper.Query("select id,StaffName,StaffMobile,IsManager,LoginUserName,getdate() LoginTime" +
                " from Res_Staffs where LoginUserName=@un and LoginPwd=@pwd",
                new SqlParameter("@un", userName), new SqlParameter("@pwd", encyptedPwd)).Tables[0];
            if (staffTable.Rows.Count == 0)
                return null;
            else
                return new OnlineBookingModel.Shared.StaffModel
                {
                    StaffId = int.Parse(staffTable.Rows[0]["id"].ToString()),
                    StaffName = staffTable.Rows[0]["StaffName"].ToString(),
                    StaffMobile = staffTable.Rows[0]["StaffMobile"].ToString(),
                    UserName = staffTable.Rows[0]["LoginUserName"].ToString(),
                    LoginTime = DateTime.Parse(staffTable.Rows[0]["LoginTime"].ToString()),
                    IsManager = bool.Parse(staffTable.Rows[0]["IsManager"].ToString())

                };
        }
    }
}
