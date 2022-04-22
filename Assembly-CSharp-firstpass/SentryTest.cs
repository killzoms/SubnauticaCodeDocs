using System;
using Sentry;
using UnityEngine;

public class SentryTest : MonoBehaviour
{
    private int _counter;

    private void Update()
    {
        _counter++;
        if (_counter % 100 == 0)
        {
            SentrySdk.AddBreadcrumb("Frame number: " + _counter);
        }
    }

    private new void SendMessage(string message)
    {
        switch (message)
        {
        case "exception":
            throw new DivideByZeroException();
        case "message":
            SentrySdk.CaptureMessage("this is a message");
            break;
        case "event":
            SentrySdk.CaptureEvent(new SentryEvent("Event message")
            {
                level = "debug"
            });
            break;
        case "assert":
            break;
        }
    }
}
