using System;
using System.IO;
using System.Linq;
using IPBan;

using NUnit.Framework;

namespace IPBanTests
{
    [TestFixture]
    public class IPBanDBTests
    {
        [Test]
        public void TestDB()
        {
            IPBanDB db = new IPBanDB();
            db.Truncate(true);
            const string ip = "10.10.10.10";
            DateTime dt1 = new DateTime(2018, 1, 1, 1, 1, 1, 1, DateTimeKind.Utc);
            DateTime dt2 = new DateTime(2019, 1, 1, 1, 1, 1, 1, DateTimeKind.Utc);
            int count = db.IncrementFailedLoginCount(ip, dt1, 1);
            Assert.AreEqual(1, count);
            count = db.IncrementFailedLoginCount(ip, dt2, 2);
            Assert.AreEqual(3, count);
            Assert.IsTrue(db.SetBanDate(ip, dt2));
            Assert.IsFalse(db.SetBanDate(ip, dt2 + TimeSpan.FromDays(1.0))); // no effect
            IPBanDB.IPAddressEntry e = db.GetIPAddress(ip);
            Assert.AreEqual(ip, e.IPAddress);
            Assert.AreEqual(dt2, e.LastFailedLogin);
            Assert.AreEqual(3, e.FailedLoginCount);
            Assert.AreEqual(dt2, e.BanDate);
            count = db.IncrementFailedLoginCount("5.5.5.5", dt1, 2);
            Assert.AreEqual(2, count);
            count = db.GetIPAddressCount();
            Assert.AreEqual(2, count);
            count = db.GetBannedIPAddressCount();
            Assert.AreEqual(1, count);
            DateTime? banDate = db.GetBanDate(ip);
            Assert.IsNotNull(banDate);
            Assert.AreEqual(dt2, banDate);
            banDate = db.GetBanDate("5.5.5.5");
            Assert.IsNull(banDate);
            count = db.SetBannedIPAddresses(new string[] { ip, "5.5.5.5", "5.5.5.6", "::5.5.5.5", "6.6.6.6", "11.11.11.11", "12.12.12.12", "11.11.11.11" }, dt2);
            Assert.AreEqual(6, count);
            count = db.GetBannedIPAddressCount();
            Assert.AreEqual(7, count);
            IPAddressRange range = IPAddressRange.Parse("5.5.5.0/24");
            count = 0;
            foreach (string ipAddress in db.DeleteIPAddresses(range))
            {
                Assert.IsTrue(ipAddress == "5.5.5.5" || ipAddress == "5.5.5.6");
                count++;
            }
            db.SetBannedIPAddresses(new string[] { "5.5.5.5", "5.5.5.6" }, dt2);
            count = db.IncrementFailedLoginCount("9.9.9.9", dt2, 1);
            Assert.AreEqual(1, count);
            count = 0;
            range = new IPAddressRange { Begin = System.Net.IPAddress.Parse("::5.5.5.0"), End = System.Net.IPAddress.Parse("::5.5.5.255") };
            foreach (string ipAddress in db.DeleteIPAddresses(range))
            {
                Assert.AreEqual(ipAddress, "::5.5.5.5");
                count++;
            }
            Assert.AreEqual(1, count);
            IPBanDB.IPAddressEntry[] ipAll = db.EnumerateIPAddresses().ToArray();
            Assert.AreEqual(7, ipAll.Length);
            IPBanDB.IPAddressEntry[] bannedIpAll = db.EnumerateBannedIPAddresses().ToArray();
            Assert.AreEqual(6, bannedIpAll.Length);
            string[] ips = new string[65536];
            int index = 0;
            for (int i = 0; i < 256; i++)
            {
                for (int j = 0; j < 256; j++)
                {
                    ips[index++] = "255." + i + ".255." + j;
                }
            }
            DateTime now = IPBanService.UtcNow;
            count = db.SetBannedIPAddresses(ips, dt2);
            Assert.AreEqual(65536, count);
            TimeSpan span = (IPBanService.UtcNow - now);

            // make sure performance is good
            Assert.Less(span, TimeSpan.FromSeconds(10.0));
        }
    }
}