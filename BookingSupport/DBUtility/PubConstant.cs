using System;
using System.Configuration;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Web.Security; 
namespace OnlineBookingSupport.DBUtility
{
    public class PubConstant
    {
        /// <summary>
        /// 获取连接字符串
        /// </summary>
        public static string ConnectionString
        {
            get
            {
                string _connectionString = ConfigurationManager.AppSettings["ConnectionString"];
                //string ConStringEncrypt = ConfigurationSettings.AppSettings["ConStringEncrypt"];
                //if (ConStringEncrypt == "true")
                //{
                //    _connectionString = DESEncrypt.Decrypt(_connectionString,"Leo He");
                //}
                return _connectionString;
            }
        }

        /// <summary>
        /// 得到web.config里配置项的数据库连接字符串。
        /// </summary>
        /// <param name="configName"></param>
        /// <returns></returns>
        public static string GetConnectionString(string configName)
        {
            string connectionString = ConfigurationManager.AppSettings[configName];
            return connectionString;
        }

        /// <summary>
        /// 得到web.config里配置项的数据库连接字符串。
        /// </summary>
        /// <param name="configName"></param>
        /// <param name="decryptKey"></param>
        /// <returns></returns>
        public static string GetConnectionString(string configName, string decryptKey)
        {
            string connectionString = ConfigurationManager.AppSettings[configName];
            var des = new DESCryptoServiceProvider();
            int len = connectionString.Length / 2;
            var inputByteArray = new byte[len];
            int x;
            for (x = 0; x < len; x++)
            {
                int i = Convert.ToInt32(connectionString.Substring(x * 2, 2), 16);
                inputByteArray[x] = (byte)i;
            }
            des.Key =
                Encoding.ASCII.GetBytes(
                    FormsAuthentication.HashPasswordForStoringInConfigFile(decryptKey, "md5").Substring(0, 8));
            des.IV =
                Encoding.ASCII.GetBytes(
                    FormsAuthentication.HashPasswordForStoringInConfigFile(decryptKey, "md5").Substring(0, 8));
            var ms = new MemoryStream();
            var cs = new CryptoStream(ms, des.CreateDecryptor(), CryptoStreamMode.Write);
            cs.Write(inputByteArray, 0, inputByteArray.Length);
            cs.FlushFinalBlock();
            return Encoding.Default.GetString(ms.ToArray());
        }
    }

}
 
