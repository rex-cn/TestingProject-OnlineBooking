using System;
using System.Collections.Generic;
using System.Linq;
using OnlineBookingModel;
using OnlineBookingSupport.DBUtility;
using System.Data;
using System.Data.SqlClient;

namespace OnlineBookingDataAccess
{
    /// <summary>
    /// Rooms Data Access Class
    /// </summary>
    public class Rooms
    {
        /// <summary>
        /// const definition 
        /// </summary>
        /// 

        enum BookingStatus {
            Success = 100,
            Fail = 101,
            Cancelled = 102
        }

        public List<RoomModel> GetRoomList()
        {
            var roomtable = SqlHelper.Query("select r.Id,RoomName,RoomCode,RoomType,RoomSize,RoomTypeName,DefaultPrice" +
                " from Res_Rooms r" +
                " left join Res_RoomType rt on r.RoomType=rt.Id").Tables[0];
            return (from DataRow r in roomtable.Rows
                    select new RoomModel
                    {
                        RoomId = int.Parse(r["Id"].ToString()),
                        RoomName = r["RoomName"].ToString(),
                        RoomCode = r["RoomCode"].ToString(),
                        RoomSize = decimal.Parse(r["RoomSize"].ToString()),
                        RoomType = int.Parse(r["RoomType"].ToString()),
                        RoomTypeName = r["RoomTypeName"].ToString(),
                    }).ToList();

        }

        public RoomDetailModel GetRoom(int roomId)
        {
            var roomtable = SqlHelper.Query("select r.Id,RoomName,RoomCode,RoomType,RoomSize,RoomTypeName,DefaultPrice" +
               " from Res_Rooms r" +
               " left join Res_RoomType rt on r.RoomType=rt.Id" +
               " where r.id=@roomId", new SqlParameter("@roomId", SqlDbType.Int) { Value = roomId }).Tables[0];          
            if (roomtable.Rows.Count > 0)
            {
                var r = roomtable.Rows[0];
                var room = new RoomDetailModel
                {
                    RoomId = int.Parse(r["Id"].ToString()),
                    RoomName = r["RoomName"].ToString(),
                    RoomCode = r["RoomCode"].ToString(),
                    RoomSize = decimal.Parse(r["RoomSize"].ToString()),
                    RoomType = int.Parse(r["RoomType"].ToString()),
                    RoomTypeName = r["RoomTypeName"].ToString(),
                };
                room.RoomBookingRecord = GetBookingList(roomId);
                return room;
            }
            else {
                return null;
            }
        }

        public IEnumerable<RoomModel> GetAvailableRoom(DateTime fromDate, DateTime toDate)
        {
            var roomTable = SqlHelper.Query("select r.Id,RoomName,RoomCode,RoomType,RoomSize,RoomTypeName,DefaultPrice" +
               " from Res_Rooms r" +
               " left join Res_RoomType rt on r.RoomType=rt.Id" +
               " where r.id not in(select distinct roomid from Act_BookingRecord" +
                    " where (CheckinDate between @fromDate and @toDate or CheckoutDate between @fromDate and @toDate"+
                    " or @fromDate between CheckinDate and CheckoutDate or  @toDate between CheckinDate and CheckoutDate)" +
                " and bookingStatus=" + (int)BookingStatus.Success + ")",
                                new SqlParameter("@fromDate", SqlDbType.DateTime) { Value = fromDate },
                                new SqlParameter("@toDate", SqlDbType.DateTime) { Value = toDate }
                    ).Tables[0];
            return (from DataRow r in roomTable.Rows
                    select new RoomModel
                    {
                        RoomId = int.Parse(r["Id"].ToString()),
                        RoomName = r["RoomName"].ToString(),
                        RoomCode = r["RoomCode"].ToString(),
                        RoomSize = decimal.Parse(r["RoomSize"].ToString()),
                        RoomType = int.Parse(r["RoomType"].ToString()),
                        RoomTypeName = r["RoomTypeName"].ToString(),
                    }).ToList();
        }

        /// <summary>
        /// Book a room
        /// </summary>
        /// <param name="roomId">RoomId</param>
        /// <param name="customerName">customerName</param>
        /// <param name="customerMobile">customerMobile</param>
        /// <param name="checkinDate">checkinDate</param>
        /// <param name="checkoutDate">checkoutDate</param>
        /// <returns>bookingId</returns>
        public bool Booking(int roomId, string customerName, string customerMobile,string customerEmail, DateTime checkinDate, DateTime checkOutDate, int operationStaff,out string failMessage)
        {
            var result = true;
            failMessage = "";
            var bookable = SqlHelper.Exists("select 1 from Res_Rooms where Id=@id",
                new SqlParameter[] {
                    new SqlParameter("@id", SqlDbType.Int) { Value = roomId }
                });
            if (bookable)
            {
                bookable = !SqlHelper.Exists("select 1 from Act_BookingRecord " +
                " where RoomId=@id and (CheckinDate between @checkindate and @checkoutdate or checkoutDate between @checkindate and @checkoutdate) and bookingstatus=100",
                new SqlParameter[] {
                    new SqlParameter("@id", SqlDbType.Int) { Value = roomId },
                    new SqlParameter("@checkindate", SqlDbType.DateTime) { Value = checkinDate },
                    new SqlParameter("@checkoutdate", SqlDbType.DateTime) { Value = checkOutDate }
                });
                if (bookable)
                    SqlHelper.ExecuteSql("insert into Act_BookingRecord" +
                          " (RoomId,CustomerName,CustomerMobile,CustomerEmail,CheckinDate,CheckoutDate,BookingStatus,OperationTime,OperationStaff)"
                          + " values (@roomid,@customername,@customermobile,@customeremail,@checkindate,@checkoutdate,@bookingStatus,getdate(),@operationstaff);",
                            new SqlParameter("@roomid", SqlDbType.Int) { Value = roomId },
                          new SqlParameter("@customername", SqlDbType.NVarChar, 50) { Value = customerName },
                          new SqlParameter("@customermobile", SqlDbType.NVarChar, 50) { Value = customerMobile },
                          new SqlParameter("@customeremail", SqlDbType.NVarChar, 50) { Value = customerEmail },
                          new SqlParameter("@checkindate", SqlDbType.DateTime) { Value = checkinDate },
                          new SqlParameter("@checkoutdate", SqlDbType.DateTime) { Value = checkOutDate },
                          new SqlParameter("@bookingStatus", SqlDbType.Int) { Value = BookingStatus.Success },
                          new SqlParameter("@operationstaff", SqlDbType.Int) { Value = operationStaff }
                           );
                else
                {
                    //insert exceptionlog -- no this room
                    failMessage = "designated room not available.";
                    SqlHelper.ExecuteSql("insert Sys_ExceptionLog (ExceptionTime,ExceptionLog,CurrentStaff) values (getdate(),@exceptionInfo,@operationStaff)",
                        new SqlParameter("@exceptionInfo", SqlDbType.Text) { Value = failMessage },
                        new SqlParameter("@operationStaff", SqlDbType.Int) { Value = operationStaff });
                    result = false;
                }
            }
            else
            {
                //insert exceptionlog -- no this room
                failMessage = "designated room not exists.";
                SqlHelper.ExecuteSql("insert Sys_ExceptionLog (ExceptionTime,ExceptionLog,CurrentStaff) values (getdate(),@exceptionInfo,@operationStaff)",
                    new SqlParameter("@exceptionInfo", SqlDbType.Text) { Value = failMessage },
                    new SqlParameter("@operationStaff", SqlDbType.Int) { Value = operationStaff });
                result = false;
            }          
            return result;
        }

        public bool CancelBooking(int bookingId, int operationStaff)
        {
            var cancelable = SqlHelper.Exists("select 1 from Act_BookingRecord " +
                " where Id=@id and BookingStatus=@bookingStatus",

                    new SqlParameter("@id", SqlDbType.Int) { Value = bookingId },
                    new SqlParameter("@bookingStatus", SqlDbType.Int) { Value = BookingStatus.Success });

            if (cancelable)
            {
                SqlHelper.ExecuteSql("update Act_BookingRecord" +
                    " set BookingStatus=@bookingStatus,OperationTime=getdate(),OperationStaff=@operationStaff" +
                    " where Id=@bookingId",
                      new SqlParameter("@bookingId", SqlDbType.Int) { Value = bookingId },
                      new SqlParameter("@bookingStatus", SqlDbType.Int) { Value = BookingStatus.Cancelled },
                    new SqlParameter("@operationStaff", SqlDbType.NVarChar, 50) { Value = operationStaff }
                     );
                return true;
            }
            else
            {
                return false;
            }
        }

        public void CancelAllBooking(int operationStaff)
        {
            var cancelTable = SqlHelper.Query("select id from Act_BookingRecord " +
                " where BookingStatus=@bookingStatus",                    
                    new SqlParameter("@bookingStatus", SqlDbType.Int) { Value = BookingStatus.Success });
            var cancelIdList = (from dr in cancelTable.Tables[0].Rows.Cast<DataRow>() select int.Parse(dr["id"].ToString())).ToList();
            foreach (int id in cancelIdList) CancelBooking(id, operationStaff);
        }

        public IEnumerable<BookingRecordModel> GetBookingList(int roomId)
        {
            return GetBookingList(roomId, true);
        }


        public IEnumerable<BookingRecordModel> GetBookingList(DateTime fromDate,DateTime toDate,string customerMobile)
        {
            return GetBookingList(fromDate,toDate,customerMobile, true);
        }

        public IEnumerable<BookingRecordModel> GetBookingList(int roomId, bool successOnly)
        {
            var roomtable = SqlHelper.Query(
                "select br.Id,r.RoomCode,CustomerName,CustomerMobile,CheckinDate,CheckoutDate,BookingStatus,OperationTime,OperationStaff,StaffName" +
              " from Act_BookingRecord br" +
              " left join Res_Staffs st on br.OperationStaff=st.Id" +
              " left join Res_Rooms r on br.RoomId=r.Id" +
              " where RoomId=@roomId" +
              (successOnly ? " and bookingStatus=" + (int)BookingStatus.Success : ""),
                new SqlParameter("@roomId", SqlDbType.Int) { Value = roomId }
              ).Tables[0];
            return (from DataRow r in roomtable.Rows
                    select new BookingRecordModel
                    {
                        Id = int.Parse(r["Id"].ToString()),
                        CustomerName = r["CustomerName"].ToString(),
                        RoomCode = r["RoomCode"].ToString(),
                        CustomerMobile = r["CustomerMobile"].ToString(),
                        CheckinDate = DateTime.Parse(r["CheckinDate"].ToString()),
                        CheckoutDate = DateTime.Parse(r["CheckoutDate"].ToString()),
                        BookingStatus = Enum.GetName(typeof(BookingStatus), int.Parse(r["BookingStatus"].ToString())),
                        OperationTime = DateTime.Parse(r["OperationTime"].ToString()),
                        OperationStaff = int.Parse(r["OperationStaff"].ToString()),
                        OperationStaffName = r["StaffName"].ToString()
                    }).ToList();
        }



        public IEnumerable<BookingRecordModel> GetBookingList(DateTime fromDate, DateTime toDate, string customerMobile, bool successOnly)
        {
            var roomtable = SqlHelper.Query(
                "select br.Id,r.RoomCode,CustomerName,CustomerMobile,CheckinDate,CheckoutDate,BookingStatus,OperationTime,OperationStaff,StaffName" +
              " from Act_BookingRecord br" +
              " left join Res_Staffs st on br.OperationStaff=st.Id" +
              " left join Res_Rooms r on br.RoomId=r.Id" +
              " where (CheckinDate between @fromDate and @toDate or CheckoutDate between @fromDate and @toDate)" +
              (successOnly ? " and bookingStatus=" + (int)BookingStatus.Success : "") +
              (customerMobile=="" ?"": " and CustomerMobile like '%" + customerMobile + "%'"),
                new SqlParameter("@customerMobile", SqlDbType.NVarChar,50) { Value = "%"+customerMobile+"%" },
                                new SqlParameter("@fromDate", SqlDbType.DateTime) { Value = fromDate },
                                new SqlParameter("@toDate", SqlDbType.DateTime) { Value = toDate }
              ).Tables[0];
            return (from DataRow r in roomtable.Rows
                    select new BookingRecordModel
                    {
                        Id = int.Parse(r["Id"].ToString()),
                        RoomCode = r["RoomCode"].ToString(),
                        CustomerName = r["CustomerName"].ToString(),
                        CustomerMobile = r["CustomerMobile"].ToString(),
                        CheckinDate = DateTime.Parse(r["CheckinDate"].ToString()),
                        CheckoutDate = DateTime.Parse(r["CheckoutDate"].ToString()),
                        BookingStatus = Enum.GetName(typeof(BookingStatus), int.Parse(r["BookingStatus"].ToString())),
                        OperationTime = DateTime.Parse(r["OperationTime"].ToString()),
                        OperationStaff = int.Parse(r["OperationStaff"].ToString()),
                        OperationStaffName = r["StaffName"].ToString()
                    }).ToList();
        }
    }
}