using System.Collections;
using UnityEngine;

namespace AssemblyCSharp
{
    public sealed class TestTelemetry : MonoBehaviour
    {
        private Telemetry telemetry;

        private IEnumerator Start()
        {
            base.gameObject.AddComponent<PlatformUtils>();
            yield return null;
            base.gameObject.AddComponent<SaveLoadManager>();
            yield return null;
            base.gameObject.AddComponent<Language>().Initialize("English");
            yield return null;
            telemetry = base.gameObject.AddComponent<Telemetry>();
            yield return new WaitForSeconds(5f);
            SendTestEvent();
        }

        [ContextMenu("Send test event")]
        private void SendTestEvent()
        {
            telemetry.SendAnalyticsEvent(TelemetryEventCategory.Other, "unit-test", "TestTelemetry");
        }
    }
}
