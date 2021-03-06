﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

using IPBan;

using NUnit.Framework;

namespace IPBanTests
{
    [TestFixture]
    public class LogFileParserTest : IFailedLogin
    {
        private static readonly string tempPath = Path.Combine(Path.GetTempPath(), "LogFileParserTest");
        private static readonly string pathAndMask = Path.Combine(tempPath, "test1*.txt");
        private static readonly List<IPAddressLogInfo> foundIps = new List<IPAddressLogInfo>();

        private static FileStream CreateFile(string name)
        {
            return new FileStream(Path.Combine(tempPath, name), FileMode.CreateNew, FileAccess.ReadWrite, FileShare.ReadWrite, 8192);
        }

        private static void Cleanup()
        {
            foundIps.Clear();
            if (Directory.Exists(tempPath))
            {
                Directory.Delete(tempPath, true);
            }
        }

        [SetUp]
        public void TestSetup()
        {
            Cleanup();
            Directory.CreateDirectory(tempPath);
        }

        [TearDown]
        public void TestCleanup()
        {
            Cleanup();
        }

        [Test]
        public void SimpleLogParseTest()
        {
            string fullPath = Path.Combine(tempPath, "test1.txt");
            using (IPBanLogFileScanner scanner = new IPBanIPAddressLogFileScanner(this, TestDnsLookup.Instance,
                source: "SSH",
                pathAndMask: pathAndMask,
                recursive: false,
                regex: "prefix__(?<ipaddress>.+)__suffix(__(?<username>.*?)__end)?",
                pingIntervalMilliseconds: 0))
            {
                StreamWriter writer = new StreamWriter(CreateFile(fullPath), Encoding.UTF8)
                {
                    AutoFlush = true
                };

                // scan once before writing any data, otherwise scanner starts at aned of file and will miss
                // the first data written
                scanner.PingFiles();

                // start off with one ip, do not write the last newline, we will do that later
                writer.Write("asdasdasdasdasdasd ");
                writer.Write("prefix__1.1.1.1__suffix message repeated 3 times");

                scanner.PingFiles();

                Assert.AreEqual(0, foundIps.Count, "Should not have found ip address yet");

                // now write a newline, this should make it pickup the line
                writer.WriteLine(" aaa ");
                writer.WriteLine();

                scanner.PingFiles();

                Assert.AreEqual(1, foundIps.Count, "Did not find all expected ip addresses");
                Assert.AreEqual("1.1.1.1", foundIps[0].IPAddress, "First ip address is wrong");
                Assert.AreEqual("SSH", foundIps[0].Source, "First ip source is wrong");
                Assert.AreEqual(3, foundIps[0].Count, "Repeat count is wrong");
                Assert.IsNull(foundIps[0].UserName, "First user name should be null");

                scanner.PingFiles();

                Assert.AreEqual(1, foundIps.Count, "Should not have found more ip address yet");

                writer.WriteLine("aowefjapweojfopaejfpaoe4231    343240-302843 -204 8-23084 -0");
                writer.WriteLine("prefix__2.2.2.2__suffix__THISUSER__end");

                scanner.PingFiles();

                Assert.AreEqual(2, foundIps.Count, "Did not find all expected ip addresses");
                Assert.AreEqual("2.2.2.2", foundIps[1].IPAddress, "Second ip address is wrong");
                Assert.AreEqual("SSH", foundIps[1].Source, "First ip source is wrong");
                Assert.AreEqual("THISUSER", foundIps[1].UserName, "Second user name is wrong");
                Assert.AreEqual(1, foundIps[1].Count, "Repeat count is wrong");

                writer.Close();

                File.Delete(fullPath);

                writer = new StreamWriter(CreateFile(fullPath), Encoding.UTF8)
                {
                    AutoFlush = true
                };
                writer.WriteLine("prefix__3.3.3.3__suffix message repeated 4 times");

                scanner.PingFiles();

                Assert.AreEqual(3, foundIps.Count, "Did not find all expected ip addresses");
                Assert.AreEqual("3.3.3.3", foundIps[2].IPAddress, "Second ip address is wrong");
                Assert.AreEqual("SSH", foundIps[2].Source, "First ip source is wrong");
                Assert.AreEqual(4, foundIps[2].Count, "Repeat count is wrong");

                writer.Close();
            }
        }

        void IFailedLogin.AddFailedLogin(IPAddressLogInfo info) { foundIps.Add(info); }
    }
}
