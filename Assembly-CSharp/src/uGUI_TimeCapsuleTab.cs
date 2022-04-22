using System;
using Gendarme;
using UnityEngine;
using UnityEngine.UI;

namespace AssemblyCSharp
{
    public class uGUI_TimeCapsuleTab : uGUI_PDATab
    {
        public Color colorValid = Color.green;

        public Color colorInvalid = Color.red;

        [AssertNotNull]
        public Text timeCapsuleLabel;

        [AssertNotNull]
        public Text statusLabel;

        [AssertNotNull]
        public Text statusText;

        [AssertNotNull]
        public Text storageLabel;

        [AssertNotNull]
        public uGUI_ItemsContainerView storage;

        [AssertNotNull]
        public Toggle submitToggle;

        [AssertNotNull]
        public Text submitText;

        [AssertNotNull]
        public Text imageLabel;

        [AssertNotNull]
        public RawImage image;

        [AssertNotNull]
        public uGUI_InputField inputFieldText;

        [AssertNotNull]
        public Text inputFieldTextPlaceholderText;

        [AssertNotNull]
        public Texture2D defaultTexture;

        [AssertNotNull]
        public uGUI_NavigableControlGrid navigableControlGrid;

        [AssertNotNull]
        public GameObject inputFieldIconValid;

        [AssertNotNull]
        public GameObject inputFieldIconInvalid;

        [AssertNotNull]
        public GameObject itemsIconValid;

        [AssertNotNull]
        public GameObject itemsIconInvalid;

        [AssertNotNull]
        public GameObject screenShotValid;

        [AssertNotNull]
        public GameObject screenShotInvalid;

        [AssertNotNull]
        public GameObject capsuleIconValid;

        [AssertNotNull]
        public GameObject capsuleIconInvalid;

        private int cachedErrorLevel = -1;

        private int cachedHasItems = -1;

        private bool expectingImageSelection;

        private Material imageMaterial;

        private float imageAspect = 1f;

        public override int notificationsCount => 0;

        protected override void Awake()
        {
            base.Awake();
            imageMaterial = new Material(image.material);
            image.material = imageMaterial;
            Rect rect = image.rectTransform.rect;
            imageAspect = rect.width / rect.height;
            inputFieldText.characterLimit = 1000;
            inputFieldText.onValueChanged.AddListener(OnTextChanged);
            submitToggle.onValueChanged.AddListener(OnSubmitChanged);
            SetImageTexture(null);
            OnLanguageChanged();
        }

        private void Update()
        {
            if (pda.currentTabType == PDATab.TimeCapsule)
            {
                UpdateStatus();
            }
        }

        [SuppressMessage("Gendarme.Rules.Naming", "AvoidRedundancyInMethodNameRule")]
        public override bool OnButtonDown(GameInput.Button button)
        {
            PDA pDA = Player.main.GetPDA();
            switch (button)
            {
                case GameInput.Button.UINextTab:
                    return true;
                case GameInput.Button.UIPrevTab:
                    return true;
                case GameInput.Button.UICancel:
                    pDA.Close();
                    return true;
                default:
                    return false;
            }
        }

        private void UpdateCheckBoxes()
        {
            PlayerTimeCapsule main = PlayerTimeCapsule.main;
            inputFieldIconValid.SetActive(main.hasText);
            inputFieldIconInvalid.SetActive(!main.hasText);
            itemsIconValid.SetActive(main.hasItems);
            itemsIconInvalid.SetActive(!main.hasItems);
            screenShotValid.SetActive(main.hasImage);
            screenShotInvalid.SetActive(!main.hasImage);
            capsuleIconValid.SetActive(main.IsValid());
            capsuleIconInvalid.SetActive(!main.IsValid());
        }

        public override void Open()
        {
            base.Open();
            PlayerTimeCapsule main = PlayerTimeCapsule.main;
            if (expectingImageSelection)
            {
                main.SetImage(null);
            }
            inputFieldText.text = main.text;
            submitToggle.isOn = main.submit;
            storage.Init(main.container);
            SetImageTexture(main.imageTexture);
            main.onTextureChanged = (PlayerTimeCapsule.OnTextureChanged)Delegate.Combine(main.onTextureChanged, new PlayerTimeCapsule.OnTextureChanged(SetImageTexture));
            UpdateCheckBoxes();
        }

        public override void Close()
        {
            base.Close();
            PlayerTimeCapsule main = PlayerTimeCapsule.main;
            if (main != null)
            {
                main.onTextureChanged = (PlayerTimeCapsule.OnTextureChanged)Delegate.Remove(main.onTextureChanged, new PlayerTimeCapsule.OnTextureChanged(SetImageTexture));
            }
        }

        public override void OnOpenPDA()
        {
        }

        public override void OnClosePDA()
        {
            expectingImageSelection = false;
            storage.Uninit();
        }

        public override uGUI_INavigableIconGrid GetInitialGrid()
        {
            return navigableControlGrid;
        }

        private void OnTextChanged(string text)
        {
            PlayerTimeCapsule.main.text = text;
        }

        private void OnSubmitChanged(bool submit)
        {
            PlayerTimeCapsule.main.submit = submit;
        }

        private void UpdateStatus()
        {
            PlayerTimeCapsule main = PlayerTimeCapsule.main;
            int num = 0;
            num += (main.hasItems ? 1 : 0);
            num += (main.hasText ? 1 : 0);
            num += (main.hasImage ? 1 : 0);
            if (cachedErrorLevel != num)
            {
                cachedErrorLevel = num;
                UpdateCheckBoxes();
                switch (num)
                {
                    case 0:
                        statusText.text = Language.main.Get("TimeCapsuleNotValid");
                        statusText.color = colorInvalid;
                        break;
                    case 1:
                        statusText.text = Language.main.Get("TimeCapsuleErrorOne");
                        statusText.color = colorInvalid;
                        break;
                    case 2:
                        statusText.text = Language.main.Get("TimeCapsuleErrorTwo");
                        statusText.color = colorInvalid;
                        break;
                    case 3:
                        statusText.text = Language.main.Get("TimeCapsuleValid");
                        statusText.color = colorValid;
                        break;
                }
            }
            bool hasItems = main.hasItems;
            int num2 = (hasItems ? 1 : 0);
            if (num2 != cachedHasItems)
            {
                cachedHasItems = num2;
                storageLabel.enabled = !hasItems;
            }
        }

        public override void OnLanguageChanged()
        {
            statusLabel.text = Language.main.Get("TimeCapsuleStatusLabel");
            timeCapsuleLabel.text = Language.main.Get("TimeCapsuleLabel");
            storageLabel.text = Language.main.Get("TimeCapsuleStorageEmpty");
            submitText.text = Language.main.Get("TimeCapsuleSubmit");
            imageLabel.text = Language.main.Get("TimeCapsuleNoImage");
            inputFieldTextPlaceholderText.text = Language.main.Get("TimeCapsuleTextPlaceholder");
        }

        public void SelectImage(string fileName)
        {
            expectingImageSelection = false;
            PlayerTimeCapsule.main.SetImage(fileName);
            Player.main.GetPDA().ui.OpenTab(PDATab.TimeCapsule);
        }

        public void SetImageTexture(Texture2D texture)
        {
            if (texture != null)
            {
                imageLabel.enabled = false;
            }
            else
            {
                texture = defaultTexture;
                imageLabel.enabled = true;
            }
            image.texture = texture;
            MathExtensions.RectFit(image.texture.width, image.texture.height, imageAspect, RectScaleMode.Envelope, out var scale, out var offset);
            image.uvRect = new Rect(offset.x, offset.y, scale.x, scale.y);
            UpdateCheckBoxes();
        }

        public void OnImageClick()
        {
            uGUI_PDA ui = Player.main.GetPDA().ui;
            expectingImageSelection = true;
            ui.OpenTab(PDATab.Gallery);
            ui.backButtonText.text = Language.main.Get("TimeCapsuleBackButton");
            ui.backButton.onClick.AddListener(OnBack);
            ui.backButton.gameObject.SetActive(value: true);
        }

        public void OnContainerClick()
        {
            uGUI_PDA ui = Player.main.GetPDA().ui;
            ui.OpenTab(PDATab.Inventory);
            ui.backButtonText.text = Language.main.Get("TimeCapsuleBackButton");
            ui.backButton.onClick.AddListener(OnBack);
            ui.backButton.gameObject.SetActive(value: true);
        }

        public void OnBack()
        {
            Player.main.GetPDA().ui.OpenTab(PDATab.TimeCapsule);
        }
    }
}
