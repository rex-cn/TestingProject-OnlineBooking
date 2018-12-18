using Microsoft.VisualStudio.TestTools.UnitTesting;
using OnlineBookingDataAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace OnlineBookingDataAccess.Tests
{
    [TestClass()]
    public class SharedTests
    {
        Shared sa = new Shared();

        [TestMethod()]
        public void LoginTest()
        {
            MD5 md5 = new MD5CryptoServiceProvider();
            var rightPare = new Dictionary<string, string> { { "rex", "rex" }, { "david", "david" }, { "celine", "celine" } };
            var wrongPare = new Dictionary<string, string> { { "rex", "rex2" }, { "david", "" }, { "Celine", "celine" } };

            foreach (var p in rightPare)
                Assert.IsNotNull(sa.Login(p.Key, BitConverter.ToString(md5.ComputeHash(Encoding.Default.GetBytes(p.Value))).Replace("-", "")));
            foreach (var p in wrongPare)
                Assert.IsNull(sa.Login(p.Key, BitConverter.ToString(md5.ComputeHash(Encoding.Default.GetBytes(p.Value))).Replace("-", "")));

        }
    }
}