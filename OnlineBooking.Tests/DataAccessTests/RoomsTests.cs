using Microsoft.VisualStudio.TestTools.UnitTesting;
using OnlineBookingDataAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OnlineBookingModel;

namespace OnlineBookingDataAccess.Tests
{
    [TestClass()]
    public class RoomsTests
    {
        Rooms rooms = new Rooms();

        [TestMethod()]
        public void GetListTest()
        {
            Assert.AreEqual(rooms.GetRoomList().Cast<RoomModel>().Count(),12);
        }

        [TestMethod()]
        public void BookingTest()
        {
            var failMessage = "";
            rooms.CancelAllBooking(0);
            Assert.AreEqual(rooms.Booking(0, "", "", "", DateTime.Today, DateTime.Today.AddDays(3), 0, out failMessage), false);
            Assert.AreEqual(rooms.Booking(100, "", "", "", DateTime.Today, DateTime.Today.AddDays(3), 0, out failMessage), true);
            Assert.AreEqual(rooms.Booking(100, "", "", "", DateTime.Today.AddDays(5), DateTime.Today.AddDays(8), 0, out failMessage), true);
            Assert.AreEqual(rooms.Booking(100, "", "", "", DateTime.Today.AddDays(-4), DateTime.Today.AddDays(-1), 0, out failMessage), true);
            Assert.AreEqual(rooms.Booking(100, "", "", "", DateTime.Today.AddDays(-2), DateTime.Today.AddDays(2), 0, out failMessage), false);

            Assert.AreEqual(rooms.GetBookingList(100).Count(), 3);
        }

        [TestMethod()]
        public void CancelBookingTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void CancelAllBookingTest()
        {
            //rooms.CancelAllBooking(0);
        }

        [TestMethod()]
        public void GetBookingListTest()
        {
            //Assert.AreEqual(rooms.GetBookingList(100).Count(), 3);
            Assert.AreEqual(rooms.GetBookingList(100,false).Count(), 15);
        }

        [TestMethod()]
        public void GetBookingListTest1()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void GetRoomTest()
        {
            Assert.AreEqual(rooms.GetRoom(100).RoomBookingRecord.Count(), 3);
        }

        [TestMethod()]
        public void GetAvailableRoomTest()
        {
            Assert.AreEqual(rooms.GetAvailableRoom(DateTime.Parse("2018-12-13"), DateTime.Parse("2018-12-15")).Count(), 11);
            Assert.AreEqual(rooms.GetAvailableRoom(DateTime.Parse("2018-12-12"), DateTime.Parse("2018-12-13")).Count(), 12);
            Assert.AreEqual(rooms.GetAvailableRoom(DateTime.Parse("2018-12-15"), DateTime.Parse("2018-12-19")).Count(), 11);
            Assert.AreEqual(rooms.GetAvailableRoom(DateTime.Parse("2018-12-30"), DateTime.Parse("2018-12-31")).Count(), 12);
            Assert.AreEqual(rooms.GetAvailableRoom(DateTime.Parse("2018-12-3"), DateTime.Parse("2018-12-31")).Count(), 11);
        }
    }
}