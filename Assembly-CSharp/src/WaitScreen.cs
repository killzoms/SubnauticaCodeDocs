using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UWE;

namespace AssemblyCSharp
{
    public class WaitScreen : MonoBehaviour
    {
        public interface IWaitItem
        {
            string text { get; }

            double pastSecs { get; }

            float progress { get; }
        }

        public abstract class WaitItemBase : IWaitItem
        {
            private readonly double startMS;

            public string text { get; private set; }

            public double pastSecs => (global::UWE.Utils.GetSystemTime() - startMS) / 1000.0;

            public abstract float progress { get; }

            public WaitItemBase(string text)
            {
                this.text = text;
                startMS = global::UWE.Utils.GetSystemTime();
            }
        }

        public class AsyncRequestItem : WaitItemBase
        {
            private readonly IAsyncRequest request;

            public override float progress => request.progress;

            public AsyncRequestItem(string text, IAsyncRequest request)
                : base(text)
            {
                this.request = request;
            }
        }

        public class AsyncOperationItem : WaitItemBase
        {
            private readonly AsyncOperation operation;

            public override float progress => operation.progress;

            public AsyncOperationItem(string text, AsyncOperation operation)
                : base(text)
            {
                this.operation = operation;
            }
        }

        public class ManualWaitItem : WaitItemBase
        {
            private float _progress;

            public override float progress => _progress;

            public ManualWaitItem(string text)
                : base(text)
            {
            }

            public void SetProgress(float p)
            {
                _progress = p;
            }

            public void SetProgress(int i, int total)
            {
                _progress = (float)i / (float)total;
            }
        }

        private static WaitScreen main;

        public bool debugFullWaitInEditor;

        public int minNumLines = 3;

        private readonly List<IWaitItem> items = new List<IWaitItem>();

        private bool isShown;

        private void Awake()
        {
            main = this;
            Hide();
        }

        private void Show(bool force = false)
        {
            uGUI.main.loading.Begin(force);
            isShown = true;
            FreezeTime.Begin("WaitScreen", dontPauseSound: true);
        }

        public static void ShowImmediately()
        {
            if (!main.isShown)
            {
                main.Show(force: true);
            }
        }

        private void Hide()
        {
            uGUI.main.loading.End();
            isShown = false;
            FreezeTime.End("WaitScreen");
        }

        private void Update()
        {
            if (isShown)
            {
                if (items.Count == 0)
                {
                    Hide();
                    return;
                }
                StringBuilder stringBuilder = new StringBuilder();
                foreach (IWaitItem item in items)
                {
                    stringBuilder.AppendLine(Language.main.GetFormat("LoadingOperationFormat", item.text, item.progress, item.pastSecs));
                }
                for (int i = items.Count; i < minNumLines; i++)
                {
                    stringBuilder.AppendLine();
                }
                uGUI.main.loading.SetLoadingText(stringBuilder.ToString());
            }
            else if (items.Count > 0)
            {
                Show();
            }
        }

        public static ManualWaitItem Add(string name)
        {
            if ((object)main == null)
            {
                return null;
            }
            lock (main)
            {
                ManualWaitItem manualWaitItem = new ManualWaitItem(name);
                main.items.Add(manualWaitItem);
                return manualWaitItem;
            }
        }

        public static AsyncOperationItem Add(string name, AsyncOperation operation)
        {
            if ((object)main == null)
            {
                return null;
            }
            lock (main)
            {
                AsyncOperationItem asyncOperationItem = new AsyncOperationItem(name, operation);
                main.items.Add(asyncOperationItem);
                return asyncOperationItem;
            }
        }

        public static AsyncRequestItem Add(string name, IAsyncRequest request)
        {
            if ((object)main == null)
            {
                return null;
            }
            lock (main)
            {
                AsyncRequestItem asyncRequestItem = new AsyncRequestItem(name, request);
                main.items.Add(asyncRequestItem);
                return asyncRequestItem;
            }
        }

        public static void Remove(IWaitItem item)
        {
            if ((object)main != null)
            {
                lock (main)
                {
                    main.items.Remove(item);
                }
                Debug.LogFormat("'{0}' took {1:0.00} seconds", item.text, item.pastSecs);
            }
        }
    }
}
