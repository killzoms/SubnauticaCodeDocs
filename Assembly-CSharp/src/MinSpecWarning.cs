using System;
using UnityEngine;
using UnityEngine.UI;

namespace AssemblyCSharp
{
    public class MinSpecWarning : MonoBehaviour
    {
        private enum gpuClass
        {
            black,
            white,
            unknown
        }

        private static int minSysRamMBs = 3584;

        public GameObject home;

        public GameObject faq;

        public GameObject ramPanel;

        public GameObject cpuPanel;

        public GameObject gpuPanel;

        public GameObject info;

        public GameObject ramInfo;

        public GameObject cpuInfo;

        public GameObject gpuInfo;

        public Text cpuDetected;

        public Text cpuL;

        public Text gpuDetected;

        public Text ramDetected;

        public GameObject mainMenu;

        private bool debug;

        private string[] whiteList = new string[101]
        {
            "AMD Radeon R9 200 Series", "NVIDIA GeForce GTX 760", "NVIDIA GeForce GTX 970", "NVIDIA GeForce GTX 770", "NVIDIA GeForce GTX 660", "Intel(R) HD Graphics 4600", "NVIDIA GeForce GTX 780", "NVIDIA GeForce GTX 750 Ti", "AMD Radeon HD 7800 Series", "AMD Radeon HD 7900 Series",
            "NVIDIA GeForce GTX 980", "NVIDIA GeForce GTX 670", "NVIDIA GeForce GTX 680", "NVIDIA GeForce GTX 650", "AMD Radeon HD 7700 Series", "NVIDIA GeForce GTX 660 Ti", "NVIDIA GeForce GTX 560 Ti", "NVIDIA GeForce GTX 780 Ti", "AMD Radeon R7 200 Series", "NVIDIA GeForce GTX 860M",
            "AMD Radeon HD 6800 Series", "NVIDIA GeForce GTX 550 Ti", "AMD Radeon HD 6900 Series", "NVIDIA GeForce GTX 650 Ti", "NVIDIA GeForce GTX 750", "NVIDIA GeForce GT 640", "NVIDIA GeForce GTX 560", "NVIDIA GeForce GTX 570", "NVIDIA GeForce GTX 580", "NVIDIA GeForce 840M",
            "NVIDIA GeForce GTX 460", "NVIDIA GeForce GT 650M", "NVIDIA GeForce GTX 960", "NVIDIA GeForce GTS 450", "NVIDIA GeForce GTX 660M", "NVIDIA GeForce GTX 765M", "AMD Radeon HD 5800 Series", "NVIDIA GeForce GTX 850M", "AMD Radeon HD 6700 Series", "NVIDIA GeForce GT 750M",
            "NVIDIA GeForce GTX 745", "NVIDIA GeForce GTX 645", "NVIDIA GeForce GTX 560M", "NVIDIA GeForce GTX 650 Ti BOOST", "NVIDIA GeForce GTX 770M", "NVIDIA GeForce GTX 760 (192-bit)", "NVIDIA GeForce GT 740M", "NVIDIA GeForce GTX 690", "NVIDIA GeForce GTX TITAN", "NVIDIA GeForce GTX 780M",
            "NVIDIA GeForce GTX 870M", "NVIDIA GeForce GTX 970M", "AMD Radeon HD 8570D", "AMD Radeon HD 8670D", "NVIDIA GeForce GTX 760 Ti OEM", "NVIDIA GeForce GTX 670M", "ATI Radeon HD 5700 Series", "NVIDIA GeForce GTX 470", "NVIDIA GeForce GT 730", "NVIDIA GeForce GTX 880M",
            "NVIDIA GeForce GT 740", "NVIDIA GeForce GT 555M", "NVIDIA GeForce GTX 980M", "NVIDIA GeForce GTX 670MX", "NVIDIA GeForce GTX 460M", "NVIDIA GeForce GT 545", "NVIDIA GeForce GTX 480", "AMD Radeon HD 7660D", "AMD Radeon HD 7660G", "NVIDIA GeForce GTX TITAN Black",
            "NVIDIA GeForce GTX 680M", "NVIDIA GeForce GT 755M", "AMD Radeon HD 5670", "NVIDIA GeForce GT 640M", "NVIDIA GeForce GTX 675M", "ATI Radeon HD 5800 Series", "AMD Mobility Radeon HD 5800 Series", "NVIDIA GeForce GT 635M", "NVIDIA GeForce GTX 460 SE", "AMD Radeon(TM) R9 270",
            "NVIDIA GeForce GTX 555", "NVIDIA GeForce GTX 275", "NVIDIA GeForce GTX 590", "ATI Radeon HD 5670", "ASUS R9 280 Series", "NVIDIA GeForce GTX 285", "AMD Radeon HD 6900M Series", "AMD Radeon HD 7670M", "NVIDIA GeForce GTX 465", "NVIDIA GeForce GTX 560 SE",
            "ASUS R9 270X Series", "AMD Radeon HD 7970M", "AMD Radeon(TM) R6 Graphics", "NVIDIA GeForce GTX 760M", "AMD Radeon HD 7500G", "ASUS HD7770 Series", "AMD Radeon R9 255", "NVIDIA GeForce GTX 675MX", "AMD Radeon HD 7000 series", "NVIDIA GeForce GTX 570M",
            "AMD Radeon HD 8570"
        };

        private string[] blackList = new string[97]
        {
            "Intel(R) HD Graphics 4000", "Intel(R) HD Graphics Family", "Intel(R) HD Graphics", "NVIDIA GeForce GT 630", "Intel(R) HD Graphics 3000", "AMD Radeon HD 6670", "NVIDIA GeForce GT 610", "NVIDIA GeForce GT 540M", "AMD Radeon(TM) R5 Graphics", "NVIDIA GeForce GT 630M",
            "ATI Radeon HD 4800 Series", "NVIDIA GeForce GT 620", "AMD Radeon HD 7640G", "AMD Radeon HD 8400", "AMD Radeon HD 7560D", "NVIDIA GeForce 820M", "AMD RADEON HD 6450", "NVIDIA GeForce GTS 250", "AMD Radeon HD 6570", "NVIDIA GeForce GT 430",
            "AMD Radeon HD 7520G", "NVIDIA GeForce GT 520", "Intel(R) HD Graphics 4400", "AMD Radeon HD 8330", "NVIDIA GeForce GT 720", "AMD Radeon(TM) HD 6520G", "NVIDIA GeForce GT 440", "NVIDIA GeForce 9800 GT", "NVIDIA GeForce GPU", "NVIDIA GeForce 9600 GT",
            "AMD Radeon HD 7570", "AMD Radeon HD 6310 Graphics", "AMD Radeon(TM) R7 Graphics", "AMD Radeon(TM) R4 Graphics", "NVIDIA GeForce GT 635", "NVIDIA GeForce 210", "NVIDIA GeForce 610M", "ATI Radeon HD 5450", "NVIDIA GeForce GT 525M", "AMD Radeon HD 5450",
            "ATI Mobility Radeon HD 5650", "NVIDIA GeForce GT 530", "NVIDIA GeForce GT 240", "NVIDIA GeForce 9500 GT", "NVIDIA GeForce GT 220", "ATI Radeon HD 4200", "NVIDIA GeForce GT 330M", "ATI Radeon HD 4600 Series", "ATI Mobility Radeon HD 5470", "AMD Radeon HD 7500 Series",
            "NVIDIA GeForce 310M", "NVIDIA GeForce GT 720M", "AMD Radeon HD 8550G", "Mobile Intel(R) 4 Series Express Chipset Family", "AMD Radeon HD 8610G", "AMD Radeon HD 6530D Graphics", "AMD Radeon(TM) HD 8610G", "NVIDIA GeForce GT 520M", "AMD Radeon HD 7340 Graphics", "AMD Radeon(TM) HD 6620G",
            "AMD Radeon HD 7500M/7600M Series", "AMD Radeon HD 6530D", "AMD Radeon HD 7600G", "AMD Radeon HD 6520G", "Parallels Display Adapter (WDDM)", "AMD Radeon HD 7310 Graphics", "AMD Radeon(TM) HD 6480G", "AMD Mobility Radeon HD 5000 Series", "AMD Radeon HD 7540D", "AMD Radeon HD 5570",
            "AMD Radeon HD 7480D", "AMD Radeon HD 6320 Graphics", "NVIDIA GeForce GTX 260M", "ATI Radeon HD 5570", "ATI Mobility Radeon HD 4200 Series", "AMD Radeon HD 7450", "AMD Radeon HD 8510G", "Intel(R) Iris(TM) Graphics 5100", "AMD Radeon HD 8240", "NVIDIA GeForce 8800 GT",
            "Intel(R) HD Graphics 5000", "AMD Radeon HD 8470D", "AMD Radeon(TM) R3 Graphics", "AMD Radeon HD 8210", "Mobile Intel(R) HD Graphics", "Intel(R) G33/G31 Express Chipset Family", "ATI Radeon HD 4300/4500 Series", "NVIDIA GeForce GT 320", "Microsoft Basic Render Driver", "NVIDIA GeForce 8600 GT",
            "AMD Radeon(TM) R2 Graphics", "NVIDIA GeForce 8400GS", "NVIDIA GeForce GT 240M", "AMD Radeon HD 7420G", "AMD Radeon(TM) HD 8510G", "AMD Radeon HD 8370D", "NVIDIA GeForce 9800 GTX/9800 GTX+"
        };

        private void Start()
        {
            string text = SystemInfo.graphicsDeviceName.Trim();
            int systemMemorySize = SystemInfo.systemMemorySize;
            int processorCount = SystemInfo.processorCount;
            bool num = Array.IndexOf(blackList, text) >= 0;
            bool flag = Array.IndexOf(whiteList, text) >= 0;
            gpuClass gpuClass = gpuClass.unknown;
            if (num)
            {
                gpuClass = gpuClass.black;
            }
            if (flag)
            {
                gpuClass = gpuClass.white;
            }
            GameObject gameObject = ramPanel.transform.Find("OK").gameObject;
            GameObject gameObject2 = ramPanel.transform.Find("NotOK").gameObject;
            GameObject gameObject3 = cpuPanel.transform.Find("OK").gameObject;
            GameObject gameObject4 = cpuPanel.transform.Find("NotOK").gameObject;
            GameObject gameObject5 = gpuPanel.transform.Find("OK").gameObject;
            GameObject gameObject6 = gpuPanel.transform.Find("NotOK").gameObject;
            GameObject gameObject7 = gpuPanel.transform.Find("Unknown").gameObject;
            if (systemMemorySize < minSysRamMBs || processorCount < 2 || gpuClass == gpuClass.black || debug)
            {
                Debug.Log("MinSpec Warning: Machine appears to be below minimum specification. Presenting warning in main menu.");
                Debug.Log("CPU logical processors: " + processorCount);
                Debug.Log("CPU name: " + SystemInfo.processorType.Trim());
                Debug.Log("GPU name: " + text);
                Debug.Log("GPU class: " + gpuClass);
                Debug.Log("RAM: " + systemMemorySize);
                mainMenu.SetActive(value: false);
                OpenPanel("Home");
                cpuDetected.text = SystemInfo.processorType.Trim();
                cpuL.text = processorCount.ToString();
                gpuDetected.text = text;
                ramDetected.text = systemMemorySize.ToString("#,##0") + " megabytes";
                if (systemMemorySize < minSysRamMBs)
                {
                    gameObject.SetActive(value: false);
                    gameObject2.SetActive(value: true);
                }
                else
                {
                    gameObject.SetActive(value: true);
                    gameObject2.SetActive(value: false);
                }
                if (processorCount < 2)
                {
                    gameObject3.SetActive(value: false);
                    gameObject4.SetActive(value: true);
                }
                else
                {
                    gameObject3.SetActive(value: true);
                    gameObject4.SetActive(value: false);
                }
                switch (gpuClass)
                {
                    case gpuClass.black:
                        gameObject5.SetActive(value: false);
                        gameObject6.SetActive(value: true);
                        gameObject7.SetActive(value: false);
                        break;
                    case gpuClass.white:
                        gameObject5.SetActive(value: true);
                        gameObject6.SetActive(value: false);
                        gameObject7.SetActive(value: false);
                        break;
                    case gpuClass.unknown:
                        gameObject5.SetActive(value: false);
                        gameObject6.SetActive(value: false);
                        gameObject7.SetActive(value: true);
                        break;
                }
            }
            else
            {
                global::UnityEngine.Object.Destroy(base.gameObject);
            }
        }

        public void OpenPanel(string target)
        {
            GameObject[] array = new GameObject[5] { home, faq, ramInfo, cpuInfo, gpuInfo };
            foreach (GameObject gameObject in array)
            {
                if (gameObject.name == target)
                {
                    gameObject.SetActive(value: true);
                }
                else
                {
                    gameObject.SetActive(value: false);
                }
            }
        }

        public void Dismiss()
        {
            mainMenu.SetActive(value: true);
            global::UnityEngine.Object.Destroy(base.gameObject);
            uGUI_MainMenu.main.Select();
        }
    }
}
