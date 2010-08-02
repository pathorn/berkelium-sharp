using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Berkelium.Managed;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Runtime.InteropServices;

namespace AutomatedTests {
    public interface ITestFixture {
        void Setup ();
        void Teardown ();
    }

    public static class Program {
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool TerminateProcess (IntPtr hProcess, int uExitCode);

        public static void Main () {
            int exitCode = 0;

            var dataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "BerkeliumAutomatedTests"
            );

            if (Directory.Exists(dataPath))
                Directory.Delete(dataPath, true);

            Directory.CreateDirectory(dataPath);

            BerkeliumSharp.Init(dataPath);

            RunTestFixture<BasicTests>(ref exitCode);
            RunTestFixture<ProtocolHandlerTests>(ref exitCode);

            // Crashes :(
            // BerkeliumSharp.Destroy();

            // Hangs :(
            // Environment.Exit(exitCode);

            if ((exitCode != 0) && Environment.CommandLine.Contains("--pause"))
                Console.ReadLine();

            Environment.ExitCode = exitCode;
            TerminateProcess(Process.GetCurrentProcess().Handle, exitCode);
        }

        static void RunTestFixture<T> (ref int exitCode)
            where T : ITestFixture, new() {
            foreach (var method in typeof(T).GetMethods()) {
                var ca = method.GetCustomAttributes(typeof(TestAttribute), true);
                if ((ca == null) || (ca.Length == 0))
                    continue;

                var fixture = new T();
                Console.WriteLine(method.Name);
                try {
                    fixture.Setup();
                    method.Invoke(fixture, null);
                    Console.WriteLine("PASS");
                } catch (Exception ex) {
                    Console.WriteLine("FAIL: {0}", ex.ToString());
                    exitCode += 1;
                } finally {
                    fixture.Teardown();
                }
            }
        }
    }
}
