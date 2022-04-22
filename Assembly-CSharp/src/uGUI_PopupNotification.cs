using System;
using Gendarme;
using UnityEngine;
using UnityEngine.UI;

namespace AssemblyCSharp
{
    public class uGUI_PopupNotification : uGUI_PopupMessage
    {
        [AssertNotNull]
        public Image image;

        [AssertNotNull]
        public Sprite defaultSprite;

        [AssertNotNull]
        public Text titleText;

        [AssertNotNull]
        public Text controlsText;

        public Color32 colorGroup = new Color32(byte.MaxValue, 166, 33, byte.MaxValue);

        public Color32 colorEntry = new Color32(119, 205, byte.MaxValue, byte.MaxValue);

        public float delay = 5f;

        [AssertNotNull]
        public FMODAsset soundEncyUnlock;

        [AssertNotNull]
        public FMODAsset soundTimeCapsuleUnlock;

        [AssertNotNull]
        public Sprite timeCapsulePopup;

        public static string formatSingleLine;

        public static string formatMultiLine;

        private int frameSet = -1;

        public static uGUI_PopupNotification main { get; private set; }

        public PDATab tabId { get; private set; }

        public string notificationId { get; private set; }

        protected override void Awake()
        {
            if (main != null)
            {
                global::UnityEngine.Object.Destroy(base.gameObject);
                return;
            }
            main = this;
            base.Awake();
            formatSingleLine = $"<color=#{colorEntry.r:X}{colorEntry.g:X}{colorEntry.b:X}>{{0}}</color>";
            formatMultiLine = $"<color=#{colorGroup.r:X}{colorGroup.g:X}{colorGroup.b:X}>{{0}}</color>\n<color=#{colorEntry.r:X}{colorEntry.g:X}{colorEntry.b:X}>{{1}}</color>";
            SetBackgroundColor(new Color(1f, 1f, 1f, 1f));
        }

        [SuppressMessage("Gendarme.Rules.Concurrency", "WriteStaticFieldFromInstanceMethodRule")]
        protected override void OnEnable()
        {
            base.OnEnable();
            PDAEncyclopedia.onAdd = (PDAEncyclopedia.OnAdd)Delegate.Combine(PDAEncyclopedia.onAdd, new PDAEncyclopedia.OnAdd(OnEncyclopediaAdd));
            KnownTech.onAnalyze += OnAnalyze;
        }

        [SuppressMessage("Gendarme.Rules.Concurrency", "WriteStaticFieldFromInstanceMethodRule")]
        protected override void OnDisable()
        {
            base.OnDisable();
            PDAEncyclopedia.onAdd = (PDAEncyclopedia.OnAdd)Delegate.Remove(PDAEncyclopedia.onAdd, new PDAEncyclopedia.OnAdd(OnEncyclopediaAdd));
            KnownTech.onAnalyze -= OnAnalyze;
        }

        public void Set(PDATab tab, string data, string title, string text, string controls, Sprite sprite)
        {
            if (tabId != tab || frameSet != Time.frameCount)
            {
                frameSet = Time.frameCount;
                tabId = tab;
                notificationId = data;
                titleText.text = title;
                image.sprite = ((sprite != null) ? sprite : defaultSprite);
                SetText(text, TextAnchor.MiddleCenter);
                controlsText.text = controls;
                Show(delay, 0f, 0.25f, 0.25f, OnTimeout);
            }
        }

        private void OnTimeout()
        {
            tabId = PDATab.None;
            notificationId = null;
        }

        private void TryPlaySound(FMODAsset sound)
        {
            if (!(sound == null))
            {
                try
                {
                    PDASounds.queue.PlayQueued(sound);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }
        }

        [SuppressMessage("Gendarme.Rules.Maintainability", "AvoidUnnecessarySpecializationRule")]
        public void OnEncyclopediaAdd(CraftNode node, bool verbose)
        {
            if (node != null && verbose)
            {
                string id = node.id;
                bool flag = false;
                string empty = string.Empty;
                string text = null;
                string buttonFormat = LanguageCache.GetButtonFormat("EncyNotificationPressPDAToView", GameInput.Button.PDA);
                Sprite sprite = null;
                FMODAsset fMODAsset = soundEncyUnlock;
                CraftNode craftNode = node.topmost as CraftNode;
                if (PDAEncyclopedia.GetEntryData(id, out var entryData))
                {
                    flag = entryData.timeCapsule;
                }
                if (flag)
                {
                    empty = Language.main.Get("EncyNotificationTimeCapsule");
                    sprite = timeCapsulePopup;
                    fMODAsset = soundTimeCapsuleUnlock;
                }
                else
                {
                    empty = Language.main.Get("EncyNotificationEntryUnlocked");
                    sprite = entryData.popup;
                    fMODAsset = entryData.sound;
                }
                if (craftNode == null || craftNode == node)
                {
                    text = string.Format(formatSingleLine, Language.main.Get(id));
                }
                else
                {
                    string empty2 = string.Empty;
                    empty2 = ((entryData == null || !entryData.timeCapsule) ? Language.main.Get($"Ency_{id}") : TimeCapsuleContentProvider.GetTitle(entryData.key));
                    string arg = Language.main.Get($"EncyPath_{craftNode.id}");
                    text = string.Format(formatMultiLine, arg, empty2);
                }
                Set(PDATab.Encyclopedia, id, empty, text, buttonFormat, sprite);
                TryPlaySound(fMODAsset);
            }
        }

        public void OnAnalyze(KnownTech.AnalysisTech analysis, bool verbose)
        {
            if (verbose)
            {
                string title = Language.main.Get(analysis.unlockMessage);
                string text = string.Format(formatSingleLine, Language.main.Get(analysis.techType.AsString()));
                Set(PDATab.None, string.Empty, title, text, null, analysis.unlockPopup);
                TryPlaySound(analysis.unlockSound);
            }
        }
    }
}
