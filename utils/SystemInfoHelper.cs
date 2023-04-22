using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;


namespace RS.Snail.JJJ.utils
{
    internal class SystemInfoHelper
    {

        public static double GetMemory()
        {
            Process proc = Process.GetCurrentProcess();
            double b = proc.PrivateMemorySize64;
            for (int i = 0; i < 2; i++)
            {
                b /= 1024d;
            }
            return b;
        }

        private static PerformanceCounter? _cpuCounter;
        private static PerformanceCounter? _ramCounter;
        private static bool _counterInited;

        [SupportedOSPlatform("windows")]
        private static void InitSystemInfoCounter()
        {
            _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            _ramCounter = new PerformanceCounter("Memory", "Available MBytes");
            _counterInited = true;
        }
        [SupportedOSPlatform("windows")]
        public static string GetSystemInfo()
        {
            if (!_counterInited) InitSystemInfoCounter();
            var ret = new List<string>();
            _cpuCounter?.NextValue();
            var cpuUsage = _cpuCounter?.NextValue() ?? 0;
            string cpuUsageStr = $"{cpuUsage:f2} %";
            var ramAvailable = _ramCounter?.NextValue() ?? 0;
            string ramAvaiableStr = $"{ramAvailable} MB";
            ret.Add($"CPU占用: {cpuUsageStr}");
            ret.Add($"RAM占用: {ramAvailable} MB");
            ret.Add($"RAM私有: {GetMemory():N2} MB");

            return string.Join("\n", ret);
        }
    }
}
