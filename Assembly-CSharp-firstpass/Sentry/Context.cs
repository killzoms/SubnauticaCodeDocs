using System;
using UnityEngine;

namespace Sentry
{
    [Serializable]
    public class Context
    {
        public App app;

        public Gpu gpu;

        public OperatingSystem os;

        public Device device;

        public Context()
        {
            os = new OperatingSystem
            {
                name = SystemInfo.operatingSystem
            };
            device = new Device();
            switch (Input.deviceOrientation)
            {
            case DeviceOrientation.Portrait:
            case DeviceOrientation.PortraitUpsideDown:
                device.orientation = "portrait";
                break;
            case DeviceOrientation.LandscapeLeft:
            case DeviceOrientation.LandscapeRight:
                device.orientation = "landscape";
                break;
            }
            string deviceModel = SystemInfo.deviceModel;
            if (deviceModel != "n/a" && deviceModel != "System Product Name (System manufacturer)")
            {
                device.model = deviceModel;
            }
            device.battery_level = SystemInfo.batteryLevel * 100f;
            device.battery_status = SystemInfo.batteryStatus.ToString();
            if (SystemInfo.systemMemorySize != 0)
            {
                device.memory_size = (long)SystemInfo.systemMemorySize * 1048576L;
            }
            device.device_type = SystemInfo.deviceType.ToString();
            device.cpu_description = SystemInfo.processorType;
            device.simulator = false;
            gpu = new Gpu
            {
                id = SystemInfo.graphicsDeviceID,
                name = SystemInfo.graphicsDeviceName,
                vendor_id = SystemInfo.graphicsDeviceVendorID,
                vendor_name = SystemInfo.graphicsDeviceVendor,
                memory_size = SystemInfo.graphicsMemorySize,
                multi_threaded_rendering = SystemInfo.graphicsMultiThreaded,
                npot_support = SystemInfo.npotSupport.ToString(),
                version = SystemInfo.graphicsDeviceVersion,
                api_type = SystemInfo.graphicsDeviceType.ToString()
            };
            app = new App();
            app.app_start_time = DateTimeOffset.UtcNow.AddSeconds(0f - Time.realtimeSinceStartup).ToString("yyyy-MM-ddTHH\\:mm\\:ssZ");
            if (Debug.isDebugBuild)
            {
                app.build_type = "debug";
            }
            else
            {
                app.build_type = "release";
            }
        }
    }
}
