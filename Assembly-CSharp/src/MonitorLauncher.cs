using System;
using System.Diagnostics;
using System.IO;
using UnityEngine;

namespace AssemblyCSharp
{
    public class MonitorLauncher : MonoBehaviour
    {
        private const string monitorExecutableFilename = "SubnauticaMonitor.exe";

        private const string monitorInputFilename = "monitor.in";

        private const string monitorOutputFilename = "monitor.out";

        public static MonitorLauncher main;

        public string workingDirectory;

        public int monitoringInterval = 10;

        public int pollingInterval = 1;

        private ProcessInfo processInfo = new ProcessInfo();

        private Process monitorProcess;

        private Stream monitorInput;

        private Stream monitorOutput;

        private BinaryReader monitorReader;

        public ProcessInfo GetProcessInfo()
        {
            return processInfo;
        }

        private void Awake()
        {
            main = this;
            if (Application.isEditor || PlatformUtils.isConsolePlatform)
            {
                global::UnityEngine.Object.Destroy(this);
            }
        }

        private void Start()
        {
            string temporaryCachePath = Application.temporaryCachePath;
            string text = Path.Combine(temporaryCachePath, "monitor.in");
            string text2 = Path.Combine(temporaryCachePath, "monitor.out");
            using (FileUtils.CreateFile(text2))
            {
            }
            monitorInput = new FileStream(text, FileMode.Create, FileAccess.Write, FileShare.Read | FileShare.Delete);
            monitorOutput = new FileStream(text2, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            monitorReader = new BinaryReader(monitorOutput);
            int id = Process.GetCurrentProcess().Id;
            monitorProcess = new Process();
            monitorProcess.StartInfo.WorkingDirectory = workingDirectory;
            monitorProcess.StartInfo.FileName = "SubnauticaMonitor.exe";
            monitorProcess.StartInfo.Arguments = $"{id} {monitoringInterval} {text} {text2}";
            monitorProcess.StartInfo.CreateNoWindow = true;
            monitorProcess.StartInfo.ErrorDialog = false;
            monitorProcess.StartInfo.LoadUserProfile = false;
            monitorProcess.StartInfo.UseShellExecute = false;
            monitorProcess.StartInfo.RedirectStandardInput = true;
            monitorProcess.Start();
            InvokeRepeating("PollMonitorOutput", 0f, pollingInterval);
        }

        private void PollMonitorOutput()
        {
            try
            {
                if (monitorOutput.Length > monitorOutput.Position)
                {
                    processInfo.Deserialize(monitorReader);
                }
            }
            catch (Exception exception)
            {
                global::UnityEngine.Debug.LogException(exception, this);
            }
        }

        private void OnDestroy()
        {
            if (monitorReader != null)
            {
                monitorReader.Close();
                monitorReader = null;
            }
            if (monitorOutput != null)
            {
                monitorOutput.Dispose();
                monitorOutput = null;
            }
            if (monitorInput != null)
            {
                monitorInput.WriteByte(27);
                monitorInput.Flush();
                monitorInput.Dispose();
                monitorInput = null;
            }
            if (monitorProcess != null)
            {
                monitorProcess.Dispose();
                monitorProcess = null;
            }
        }
    }
}
