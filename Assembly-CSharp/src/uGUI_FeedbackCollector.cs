using System.Collections;
using System.Security.Cryptography;
using System.Text;
using LitJson;
using Steamworks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityStandardAssets.ImageEffects;
using UWE;

namespace AssemblyCSharp
{
    public class uGUI_FeedbackCollector : uGUI_InputGroup, uGUI_IButtonReceiver
    {
        public static uGUI_FeedbackCollector main;

        private const string freezerName = "FeedbackPanel";

        public string url = "http://qa.unknownworlds.com/api/subnautica-feedback";

        public float hintDelay = 70f;

        public float sendTimeout = 60f;

        public int maxChars = 190;

        public Color32 successColor = new Color32(0, byte.MaxValue, 0, byte.MaxValue);

        public Color32 failColor = new Color32(byte.MaxValue, 0, 0, byte.MaxValue);

        public uGUI_FeedbackPanel initialPanel;

        public uGUI_NavigableControlGrid privacyPanel;

        public GameObject inputBlocker;

        public GameObject root;

        public GameObject repliesPanel;

        public ScrollRect repliesScrollRect;

        public Text repliesText;

        public float minPosX = -460f;

        public float maxPosX = 20f;

        public uGUI_PopupMessage status;

        public uGUI_PopupMessage hint;

        public Text charCountIndicator;

        public InputField feedbackInputField;

        public Toggle toggleGeneral;

        public Toggle toggleGameplay;

        public Toggle toggleBug;

        public Toggle toggleFramerate;

        public Toggle toggleIncludeScreenshot;

        public Image screenshotImage;

        public Toggle toggleRememberEmail;

        public InputField emailInputField;

        public GameObject buttonOpenEmailDrawer;

        public GameObject emailDrawer;

        public Toggle infoButton;

        private uGUI_InputGroup restoreGroup;

        private Coroutine sendingRoutine;

        private Coroutine repliesRoutine;

        private WWW www;

        private bool state;

        private RectTransform rootRT;

        private CanvasGroup rootCanvasGroup;

        private RectTransform repliesRT;

        private CanvasGroup repliesCanvasGroup;

        private bool feedbackVisible;

        private Sequence feedbackSequence;

        private Sequence hintSequence;

        private Sequence sendTimeoutSequence;

        private bool mandatoryScreenshot = true;

        private bool includeScreenshot;

        private bool includeEmail;

        private bool dataGeneral;

        private bool dataGameplay;

        private bool dataBug;

        private bool dataFramerate;

        private float dataFPS;

        private Texture2D dataScreenshot;

        private string dataEmail;

        private static Camera[] grayScaleEffectCameras;

        public bool IsEnabled()
        {
            return !XRSettings.enabled;
        }

        protected override void Awake()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            if (main != null)
            {
                Object.Destroy(this);
                return;
            }
            main = this;
            rootRT = root.GetComponent<RectTransform>();
            rootCanvasGroup = rootRT.GetComponent<CanvasGroup>();
            repliesRT = repliesPanel.GetComponent<RectTransform>();
            repliesCanvasGroup = repliesPanel.GetComponent<CanvasGroup>();
            FeedbackInit();
            HintInit();
            sendTimeoutSequence = new Sequence();
            inputBlocker.SetActive(value: false);
            root.SetActive(value: false);
            repliesPanel.SetActive(value: false);
            repliesText.text = "";
            base.Awake();
        }

        private void OnEnable()
        {
            if (IsEnabled())
            {
                repliesRoutine = StartCoroutine(RepliesRoutine());
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (repliesRoutine != null)
            {
                StopCoroutine(repliesRoutine);
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode loadMode)
        {
            FeedbackInit();
            HintInit();
        }

        protected override void Update()
        {
            if (!state || base.focused)
            {
                if (IsEnabled() && Input.GetKeyDown(KeyCode.F8))
                {
                    HintHide();
                    Toggle();
                }
                else if (state && Input.GetKeyDown(KeyCode.Escape))
                {
                    Close();
                }
            }
            hintSequence.Update();
            FeedbackUpdate();
            sendTimeoutSequence.Update(Time.unscaledDeltaTime);
        }

        private void OnDestroy()
        {
            if (main == this)
            {
                main = null;
            }
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private IEnumerator RepliesRoutine()
        {
            yield return new WaitForSeconds(3f);
            bool firstInit = true;
            while (true)
            {
                string SteamID = GetPlayerOnlineId();
                if (!string.IsNullOrEmpty(SteamID))
                {
                    WWW www = new WWW($"https://subnautica.unknownworlds.com/api/feedback/replies/user/{getHashSha256(ref SteamID)}");
                    yield return www;
                    if (string.IsNullOrEmpty(www.error))
                    {
                        ICollection collection = JsonMapper.ToObject(www.text);
                        StringBuilder sb = new StringBuilder(repliesText.text);
                        bool flag = false;
                        foreach (JsonData item in collection)
                        {
                            JsonData jsonData2 = item["is_read"];
                            bool flag2 = jsonData2.IsBoolean && (bool)jsonData2;
                            if (firstInit || !flag2)
                            {
                                string arg = item["ticket"]["text"].ToString();
                                string arg2 = item["text"].ToString();
                                sb.AppendFormat("<color=#ffb645ff>Feedback: </color>{0}\n<color=#ffb645ff>Developer: </color><color=#ebebebff>{1}</color>\n\n", arg, arg2);
                                string arg3 = item["_id"].ToString();
                                if (!flag2)
                                {
                                    yield return new WWW($"https://subnautica.unknownworlds.com/api/feedback/replies/{arg3}/mark-as-read");
                                    string.IsNullOrEmpty(www.error);
                                    flag = true;
                                }
                            }
                        }
                        repliesText.text = sb.ToString();
                        firstInit = false;
                        if (flag)
                        {
                            ShowReplyNotification();
                        }
                    }
                }
                yield return new WaitForSeconds(300f);
            }
        }

        private void ShowReplyNotification()
        {
            if (!feedbackVisible)
            {
                hint.ox = 31f;
                hint.oy = 0f;
                string message = ((GameInput.GetPrimaryDevice() == GameInput.Device.Controller || PlatformUtils.isConsolePlatform) ? Language.main.Get("FeedbackNewReplyGamepad") : LanguageCache.GetButtonFormat("FeedbackNewReply", GameInput.Button.Feedback));
                hint.anchor = TextAnchor.MiddleRight;
                hint.SetText(message, TextAnchor.MiddleLeft);
                hint.Show();
            }
        }

        private void HintInit()
        {
            if (hintSequence == null)
            {
                hintSequence = new Sequence();
            }
            else
            {
                hintSequence.Reset();
            }
            if (uGUI.isMainLevel && IsEnabled())
            {
                hintSequence.Set(hintDelay, target: true, HintShow);
            }
        }

        private void HintShow()
        {
            hint.ox = 31f;
            hint.oy = 0f;
            string message = ((GameInput.GetPrimaryDevice() == GameInput.Device.Controller || PlatformUtils.isConsolePlatform) ? Language.main.Get("FeedbackInstructionsGamepad") : LanguageCache.GetButtonFormat("FeedbackInstructions", GameInput.Button.Feedback));
            hint.anchor = TextAnchor.MiddleRight;
            hint.SetText(message, TextAnchor.MiddleLeft);
            hint.Show();
        }

        private void HintHide()
        {
            hintSequence.Reset();
            hint.Hide();
        }

        public void ToggleGeneral(bool value)
        {
            dataGeneral = value;
        }

        public void ToggleGameplay(bool value)
        {
            dataGameplay = value;
        }

        public void ToggleBug(bool value)
        {
            dataBug = value;
        }

        public void ToggleFramerate(bool value)
        {
            dataFramerate = value;
        }

        public void ToggleScreenshot(bool value)
        {
            includeScreenshot = value;
        }

        public void ToggleEmail(bool value)
        {
            includeEmail = value;
        }

        public void OnValueChange(string value)
        {
            int length = value.Length;
            if (length > maxChars - 1)
            {
                charCountIndicator.color = Color.red;
            }
            else if (length > maxChars - 10)
            {
                charCountIndicator.color = Color.yellow;
            }
            else
            {
                charCountIndicator.color = Color.white;
            }
            charCountIndicator.text = length + "/" + maxChars;
        }

        public void Emotion(int emotion)
        {
            if (state)
            {
                Submit(emotion);
                Reset();
                Close();
            }
        }

        public void ShowWebPage(string url)
        {
            SteamFriends.ActivateGameOverlayToWebPage(url);
        }

        private void FeedbackInit()
        {
            if (feedbackSequence == null)
            {
                feedbackSequence = new Sequence(initialState: false);
            }
            else
            {
                feedbackSequence.Reset();
            }
            SetGrayscaleValue(0f);
            FeedbackVisible(state: false, forced: true);
        }

        private void FeedbackVisible(bool state, bool forced = false)
        {
            if (feedbackVisible != state || forced)
            {
                feedbackVisible = state;
                if (EventSystem.current != null)
                {
                    EventSystem.current.SetSelectedGameObject(null);
                }
                float num3 = (repliesCanvasGroup.alpha = (rootCanvasGroup.alpha = (state ? 1f : 0f)));
                CanvasGroup canvasGroup = repliesCanvasGroup;
                bool interactable = (rootCanvasGroup.interactable = state);
                canvasGroup.interactable = interactable;
                CanvasGroup canvasGroup2 = repliesCanvasGroup;
                interactable = (rootCanvasGroup.blocksRaycasts = state);
                canvasGroup2.blocksRaycasts = interactable;
            }
        }

        private void FeedbackUpdate()
        {
            feedbackSequence.Update(Time.unscaledDeltaTime);
            if (feedbackSequence.active)
            {
                float t = feedbackSequence.t;
                SetGrayscaleValue(t);
                SetPanelPosition(t);
            }
            if (www != null)
            {
                string message = $"{Mathf.RoundToInt(www.uploadProgress * 100f)}% ({Mathf.RoundToInt(sendTimeoutSequence.t * sendTimeoutSequence.time)} sec)";
                string message2 = FormatMessage(Language.main.Get("FeedbackSending"), message, Color.green, Color.white, 30, 30);
                status.SetText(message2, TextAnchor.MiddleCenter);
            }
        }

        private void Toggle()
        {
            if (state)
            {
                Close();
            }
            else
            {
                Open();
            }
        }

        public void Open()
        {
            if (!state && www == null)
            {
                status.Hide();
                state = true;
                StartCoroutine(BeginTransition());
            }
        }

        private void Close()
        {
            if (state)
            {
                state = false;
                buttonOpenEmailDrawer.SetActive(value: true);
                emailDrawer.SetActive(value: false);
                infoButton.isOn = false;
                feedbackSequence.Set(0.25f, target: false, EndTransition);
            }
        }

        private void Reset()
        {
            feedbackInputField.text = "";
            toggleGeneral.isOn = false;
            toggleGameplay.isOn = false;
            toggleBug.isOn = false;
            toggleFramerate.isOn = false;
            toggleIncludeScreenshot.isOn = false;
        }

        private IEnumerator BeginTransition()
        {
            if (!feedbackSequence.active)
            {
                dataFPS = Time.timeScale / Time.smoothDeltaTime;
                yield return new WaitForEndOfFrame();
                dataScreenshot = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, mipChain: true);
                dataScreenshot.name = "Feedback.DataScreenshot";
                dataScreenshot.ReadPixels(new Rect(0f, 0f, Screen.width, Screen.height), 0, 0);
                dataScreenshot.Apply();
                FeedbackVisible(state: true);
                screenshotImage.sprite = Sprite.Create(dataScreenshot, new Rect(0f, 0f, dataScreenshot.width, dataScreenshot.height), new Vector2(0.5f, 0.5f), 1f, 0u, SpriteMeshType.FullRect);
                dataEmail = MiscSettings.email;
                includeEmail = MiscSettings.rememberEmail;
                emailInputField.text = dataEmail;
                toggleRememberEmail.isOn = includeEmail;
                FreezeTime.Begin("FeedbackPanel");
                inputBlocker.SetActive(value: true);
                root.SetActive(value: true);
                repliesPanel.SetActive(value: true);
                restoreGroup = Select(lockMovement: true);
            }
            feedbackSequence.Set(0.25f, target: true, EndTransition);
        }

        private void EndTransition()
        {
            if (feedbackSequence.target)
            {
                SetGrayscaleValue(1f);
                feedbackInputField.Select();
                feedbackInputField.ActivateInputField();
            }
            else
            {
                SetGrayscaleValue(0f);
                if (dataScreenshot != null)
                {
                    Object.Destroy(dataScreenshot);
                    dataScreenshot = null;
                }
                if ((bool)screenshotImage.sprite)
                {
                    Object.Destroy(screenshotImage.sprite);
                    screenshotImage.sprite = null;
                }
                FreezeTime.End("FeedbackPanel");
                Deselect(restoreGroup);
                MiscSettings.email = (includeEmail ? emailInputField.text : "");
                MiscSettings.rememberEmail = includeEmail;
                FeedbackVisible(state: false);
            }
            SetPanelPosition(feedbackSequence.target ? 1f : 0f);
        }

        private string GetPlayerOnlineId()
        {
            string text = null;
            if (Application.isEditor)
            {
                return "76561197971817336";
            }
            return PlatformUtils.main.GetCurrentUserId();
        }

        private void Submit(int emotion)
        {
            Vector3 vector = Vector3.zero;
            Quaternion quaternion = Quaternion.identity;
            Player player = Player.main;
            if (player != null)
            {
                Transform component = player.viewModelCamera.GetComponent<Transform>();
                vector = component.position;
                quaternion = component.rotation;
            }
            WWWForm wWWForm = new WWWForm();
            string SteamID = GetPlayerOnlineId();
            if (!string.IsNullOrEmpty(SteamID))
            {
                string hashSha = getHashSha256(ref SteamID);
                wWWForm.AddField("unique_id", hashSha);
            }
            string text = feedbackInputField.text;
            if (!string.IsNullOrEmpty(text))
            {
                wWWForm.AddField("text", text);
            }
            wWWForm.AddField("emotion", emotion);
            wWWForm.AddField("position_x", vector.x.ToString());
            wWWForm.AddField("position_y", vector.y.ToString());
            wWWForm.AddField("position_z", vector.z.ToString());
            wWWForm.AddField("orientation_w", quaternion.w.ToString());
            wWWForm.AddField("orientation_x", quaternion.x.ToString());
            wWWForm.AddField("orientation_y", quaternion.y.ToString());
            wWWForm.AddField("orientation_z", quaternion.z.ToString());
            if (dataGeneral)
            {
                wWWForm.AddField("categories[]", 1);
            }
            if (dataGameplay)
            {
                wWWForm.AddField("categories[]", 2);
            }
            if (dataBug)
            {
                wWWForm.AddField("categories[]", 3);
            }
            if (dataFramerate)
            {
                wWWForm.AddField("categories[]", 4);
            }
            wWWForm.AddField("cpu", $"{SystemInfo.processorType} ({SystemInfo.processorCount} logical processors)");
            wWWForm.AddField("gpu", $"{SystemInfo.graphicsDeviceName} ({SystemInfo.graphicsMemorySize}MB)");
            wWWForm.AddField("ram", SystemInfo.systemMemorySize);
            wWWForm.AddField("os", SystemInfo.operatingSystem);
            string value = "pc";
            if (PlatformUtils.isPS4Platform)
            {
                value = "ps4";
            }
            else if (PlatformUtils.isXboxOnePlatform)
            {
                value = "xone";
            }
            wWWForm.AddField("platform", value);
            wWWForm.AddField("fps", dataFPS.ToString());
            if (mandatoryScreenshot || includeScreenshot)
            {
                byte[] contents = dataScreenshot.EncodeToJPG();
                wWWForm.AddBinaryData("screenshot", contents, "ingameScreenshot.jpg", "image/jpeg");
            }
            string plasticChangeSetOfBuild = SNUtils.GetPlasticChangeSetOfBuild();
            if (!string.IsNullOrEmpty(plasticChangeSetOfBuild))
            {
                wWWForm.AddField("csid", plasticChangeSetOfBuild);
            }
            wWWForm.AddField("email", dataEmail);
            status.Show(-1f);
            status.SetBackgroundColor(successColor);
            sendingRoutine = StartCoroutine(AsyncSend(wWWForm));
        }

        private static string EmotionToString(int emotion)
        {
            return emotion switch
            {
                1 => "Happy", 
                2 => "Okay", 
                3 => "Unhappy", 
                4 => "Angry", 
                _ => "Unknown", 
            };
        }

        private IEnumerator AsyncSend(WWWForm form)
        {
            sendTimeoutSequence.Set(sendTimeout, current: false, target: true, SendingTimeout);
            string text = url;
            Debug.Log("Feedback Collector: URL: " + text);
            www = new WWW(text, form);
            yield return www;
            if (string.IsNullOrEmpty(www.error))
            {
                Debug.LogFormat("Feedback Collector: Feedback sent. Server response: {0}\n", www.text);
                MessageFeedbackSent();
            }
            else
            {
                Debug.LogErrorFormat("Feedback Collector: Error sending feedback: {0}\n", www.error);
                MessageStreamingError();
            }
            SendingCleanup();
        }

        private void SendingTimeout()
        {
            StopCoroutine(sendingRoutine);
            MessageStreamingError();
            SendingCleanup();
        }

        private void SendingCleanup()
        {
            sendTimeoutSequence.Reset();
            sendingRoutine = null;
            www = null;
        }

        private void MessageStreamingError()
        {
            string message = FormatMessage(Language.main.Get("FeedbackError"), PlatformUtils.isConsolePlatform ? Language.main.Get("FeedbackSendingErrorConsoles") : Language.main.Get("FeedbackSendingError"), Color.red, Color.white, 30, 17);
            status.SetBackgroundColor(failColor);
            status.SetText(message);
            status.Show();
        }

        private void MessageFeedbackSent()
        {
            string message = FormatMessage(Language.main.Get("FeedbackSuccess"), string.Empty, Color.white, Color.white, 30, 17);
            status.SetBackgroundColor(successColor);
            status.SetText(message, TextAnchor.MiddleCenter);
            status.Show(2f);
        }

        public override void OnSelect(bool lockMovement)
        {
            base.OnSelect(lockMovement);
            GamepadInputModule.current.SetCurrentGrid(initialPanel);
        }

        public override void OnDeselect()
        {
            inputBlocker.SetActive(value: false);
            root.SetActive(value: false);
            repliesPanel.SetActive(value: false);
            Close();
            base.OnDeselect();
        }

        private void SetPanelPosition(float t)
        {
            t = Mathf.Clamp01(t);
            rootRT.anchoredPosition = new Vector2(Mathf.Lerp(minPosX, maxPosX, MathExtensions.EaseOutSine(t)), rootRT.anchoredPosition.y);
            repliesRT.anchoredPosition = new Vector2(Mathf.Lerp(maxPosX, minPosX, MathExtensions.EaseOutSine(t)), repliesRT.anchoredPosition.y);
        }

        private string FormatMessage(string title, string message, Color titleColor, Color messageColor, int titleSize, int messageSize)
        {
            Language language = Language.main;
            if (language != null)
            {
                title = language.Get(title);
                message = language.Get(message);
            }
            return string.Format("<size={0}><color=#{1}>{2}{3}</color></size><size={4}><color=#{5}>{6}</color></size>", titleSize, MathExtensions.Color2Hex(titleColor), title, (message.Length == 0) ? string.Empty : "\n", messageSize, MathExtensions.Color2Hex(messageColor), message);
        }

        private void SetGrayscaleValue(float amount)
        {
            int allCamerasCount = Camera.allCamerasCount;
            if (grayScaleEffectCameras == null || grayScaleEffectCameras.Length < allCamerasCount)
            {
                grayScaleEffectCameras = new Camera[allCamerasCount];
            }
            Camera.GetAllCameras(grayScaleEffectCameras);
            for (int i = 0; i < grayScaleEffectCameras.Length; i++)
            {
                Camera camera = grayScaleEffectCameras[i];
                if (!(camera == null))
                {
                    Grayscale component = camera.GetComponent<Grayscale>();
                    if (!(component == null))
                    {
                        component.effectAmount = amount;
                        component.enabled = amount > 0f;
                    }
                }
            }
        }

        private string getHashSha256(ref string SteamID)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(SteamID + "7g4M9a");
            byte[] array = new SHA256Managed().ComputeHash(bytes);
            string text = string.Empty;
            byte[] array2 = array;
            foreach (byte b in array2)
            {
                text += $"{b:x2}";
            }
            return text;
        }

        public bool OnButtonDown(GameInput.Button button)
        {
            if (button == GameInput.Button.UICancel && state)
            {
                if (privacyPanel.gameObject.activeInHierarchy)
                {
                    ClosePrivacyPanel();
                }
                else
                {
                    Close();
                }
                return true;
            }
            return false;
        }

        public void OpenPrivacyPanel()
        {
            privacyPanel.gameObject.SetActive(value: true);
            GamepadInputModule.current.SetCurrentGrid(privacyPanel);
        }

        public void ClosePrivacyPanel()
        {
            privacyPanel.gameObject.SetActive(value: false);
            GamepadInputModule.current.SetCurrentGrid(initialPanel);
        }
    }
}
