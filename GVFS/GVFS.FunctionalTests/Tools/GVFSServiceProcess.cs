﻿using GVFS.Tests.Should;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Threading;

namespace GVFS.FunctionalTests.Tools
{
    public static class GVFSServiceProcess
    {
        public const string TestServiceName = "Test.GVFS.Service";
        private const string ServiceNameArgument = "--servicename=" + TestServiceName;

        public static void InstallService(string pathToService)
        {
            UninstallService();

            // Wait for delete to complete. If the services control panel is open, this will never complete.
            while (RunScCommand("query", TestServiceName).ExitCode == 0)
            {
                Thread.Sleep(1000);
            }

            // Install service
            string createServiceArguments = string.Format(
                "{0} binPath= \"{1}\"",
                TestServiceName,
                pathToService);

            ProcessResult result = RunScCommand("create", createServiceArguments);
            result.ExitCode.ShouldEqual(0, "Failure while running sc create " + createServiceArguments + "\r\n" + result.Output);

            StartService();
        }

        public static void UninstallService()
        {
            StopService();

            RunScCommand("delete", TestServiceName);

            // Make sure to delete any test service data state
            string serviceData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "GVFS", TestServiceName);
            DirectoryInfo serviceDataDir = new DirectoryInfo(serviceData);
            if (serviceDataDir.Exists)
            {
                serviceDataDir.Delete(true);
            }
        }

        private static void StartService()
        {
            using (ServiceController controller = new ServiceController(TestServiceName))
            {
                controller.Start(new[] { ServiceNameArgument });
                controller.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(10));
                controller.Status.ShouldEqual(ServiceControllerStatus.Running);
            }
        }

        private static void StopService()
        {
            try
            {
                ServiceController testService = ServiceController.GetServices().SingleOrDefault(service => service.ServiceName == TestServiceName);
                if (testService != null)
                {
                    if (testService.Status == ServiceControllerStatus.Running)
                    {
                        testService.Stop();
                    }

                    testService.WaitForStatus(ServiceControllerStatus.Stopped);
                }
            }
            catch (InvalidOperationException)
            {
                return;
            }
        }

        private static ProcessResult RunScCommand(string command, string parameters)
        {
            ProcessStartInfo processInfo = new ProcessStartInfo("sc");
            processInfo.WindowStyle = ProcessWindowStyle.Hidden;
            processInfo.UseShellExecute = false;
            processInfo.RedirectStandardOutput = true;

            processInfo.Arguments = command + " " + parameters;

            return ProcessHelper.Run(processInfo);
        }
    }
}
