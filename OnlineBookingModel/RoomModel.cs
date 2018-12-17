using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace OnlineBookingModel
{
    public class RoomModel
    {
        [Display(Name = "RoomId")]
        public int RoomId;
        [Display(Name = "RoomName")]
        public string RoomName;
        [Display(Name = "RoomCode")]
        public string RoomCode;
        [Display(Name = "RoomSize")]
        public decimal RoomSize;
        [Display(Name = "RoomType")]
        public int RoomType;
        [Display(Name = "RoomTypeName")]
        public string RoomTypeName;
    }

    public class RoomDetailModel:RoomModel
    {
        public IEnumerable<BookingRecordModel> RoomBookingRecord;
    }

    public class BookingRecordModel
    {
        [Display(Name = "Id")]
        public int Id;
        [Display(Name = "RoomId")]
        public int RoomId;
        [Display(Name = "RoomCode")]
        public string RoomCode;
        [Display(Name = "CustomerName")]
        public string CustomerName;
        [Display(Name = "CustomerMobile")]
        public string CustomerMobile;
        [Display(Name = "CheckinDate")]
        public DateTime CheckinDate;
        [Display(Name = "CheckoutDate")]
        public DateTime CheckoutDate;
        [Display(Name = "BookingStatus")]
        public string BookingStatus;
        [Display(Name = "OperationTime")]
        public DateTime OperationTime;
        [Display(Name = "OperationStaff")]
        public int OperationStaff;
        [Display(Name = "OperationStaffName")]
        public string OperationStaffName;
    }

    public class ReturnResult
    {
        public int ReturnValue;
        public string ReturnMessage;
    }
}