using System;
using System.Collections;
using UnityEngine;

namespace AssemblyCSharp
{
    public sealed class TestSentry : MonoBehaviour
    {
        private SentrySdk sentry;

        private IEnumerator Start()
        {
            sentry = base.gameObject.AddComponent<SentrySdk>();
            sentry.Dsn = "https://78c9008680d647c7946aa2ab5dc6805a@sentry.unknownworlds.com/2";
            sentry.Version = "unit-test";
            sentry.SendDefaultPii = false;
            yield return new WaitForSeconds(1f);
            SendTestMessage();
            yield return new WaitForSeconds(1f);
            RaiseTestAssertion();
            yield return new WaitForSeconds(1f);
            ThrowTestException();
        }

        [ContextMenu("Send test message")]
        private void SendTestMessage()
        {
            Debug.LogError("TestSentry.SendTestMessage", this);
        }

        [ContextMenu("Raise test assertion")]
        private void RaiseTestAssertion()
        {
        }

        [ContextMenu("Throw test exception")]
        private void ThrowTestException()
        {
            throw new InvalidOperationException("TestSentry");
        }
    }
}
