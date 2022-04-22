using AssemblyCSharp.Story;
using UnityEngine;

namespace AssemblyCSharp
{
    public class IntroLifepodDirector : MonoBehaviour
    {
        public GameObject[] toggleActiveObjects = new GameObject[5];

        public BoxCollider fireExtinguisherPickupVolume;

        public LightingController lightingController;

        public FMOD_CustomLoopingEmitter music;

        [AssertNotNull]
        public EscapePod escapePod;

        [AssertNotNull]
        public LiveMixin escapePodLiveMixin;

        public GameObject repairRadioNode;

        public static IntroLifepodDirector main;

        private static bool debugIntro;

        private bool playingMusic;

        private void Awake()
        {
            main = this;
            if (debugIntro)
            {
                EnableIntroSequence();
            }
            else
            {
                ToggleActiveObjects(on: false);
            }
        }

        private void Update()
        {
            if (playingMusic)
            {
                music.Play();
            }
            else
            {
                music.Stop();
            }
        }

        public void OnProtoSerializerObjectTree(ProtobufSerializer serializer)
        {
        }

        public void OnProtoDeserializeObjectTree(ProtobufSerializer serializer)
        {
            if (!(escapePodLiveMixin.GetHealthFraction() > 0.99f))
            {
                escapePod.ShowDamagedEffects();
                lightingController.SnapToState(2);
                uGUI_EscapePod.main.SetHeader(Language.main.Get("IntroEscapePod3Header"), new Color32(243, 201, 63, byte.MaxValue), 2f);
                uGUI_EscapePod.main.SetContent(Language.main.Get("IntroEscapePod3Content"), new Color32(233, 63, 27, byte.MaxValue));
                uGUI_EscapePod.main.SetPower(Language.main.Get("IntroEscapePod3Power"), new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue));
            }
        }

        public void PlayMusic()
        {
            playingMusic = true;
        }

        public void EnableIntroSequence()
        {
            uGUI.main.barsPanel.SetActive(value: false);
            uGUI.main.quickSlots.gameObject.SetActive(value: false);
            playingMusic = true;
            IntroVignette.isIntroActive = true;
            escapePod.DamagePlayer();
            ToggleActiveObjects(on: true);
        }

        public void ConcludeIntroSequence()
        {
            Invoke("SetHudToActive", 30f);
            playingMusic = false;
            lightingController.LerpToState(2, 5f);
            if ((bool)Player.main.playerAnimator)
            {
                Player.main.playerAnimator.SetBool("holster_extinguisher_first", value: true);
                Player.main.playerAnimator.SetBool("holding_fireextinguisher", value: false);
            }
            Invoke("ResetExtinguisherFirst", 4f);
            Invoke("OpenPDA", 4.1f);
            Invoke("ResetFirstUse", 8f);
            if ((bool)fireExtinguisherPickupVolume)
            {
                fireExtinguisherPickupVolume.enabled = false;
            }
            ToggleActiveObjects(on: false);
            uGUI_EscapePod.main.SetHeader(Language.main.Get("IntroEscapePod3Header"), new Color32(243, 201, 63, byte.MaxValue), 2f);
            uGUI_EscapePod.main.SetContent(Language.main.Get("IntroEscapePod3Content"), new Color32(233, 63, 27, byte.MaxValue));
            uGUI_EscapePod.main.SetPower(Language.main.Get("IntroEscapePod3Power"), new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue));
        }

        private void SetHudToActive()
        {
            uGUI.main.barsPanel.SetActive(value: true);
            uGUI.main.quickSlots.gameObject.SetActive(value: true);
        }

        private void OpenPDA()
        {
            Player.main.GetPDA().OpenFirst(OnClosePDA);
            Player.main.playerAnimator.SetBool("using_tool_first", value: true);
            Player.main.playerAnimator.SetBool("using_pda", value: true);
            StoryGoalManager storyGoalManager = StoryGoalManager.main;
            if ((bool)storyGoalManager)
            {
                storyGoalManager.OnGoalComplete("Trigger_PDAIntroBegin");
            }
        }

        private void OnClosePDA(PDA pda)
        {
            IntroVignette.isIntroActive = false;
            StoryGoalManager storyGoalManager = StoryGoalManager.main;
            if ((bool)storyGoalManager)
            {
                storyGoalManager.OnGoalComplete("Trigger_PDAIntroEnd");
            }
        }

        private void ResetFirstUse()
        {
            if ((bool)Player.main.playerAnimator)
            {
                Player.main.playerAnimator.SetBool("using_tool_first", value: false);
            }
        }

        private void ResetExtinguisherFirst()
        {
            if ((bool)Player.main.playerAnimator)
            {
                Player.main.playerAnimator.SetBool("holster_extinguisher_first", value: false);
            }
        }

        private void ToggleActiveObjects(bool on)
        {
            for (int i = 0; i < toggleActiveObjects.Length; i++)
            {
                GameObject gameObject = toggleActiveObjects[i];
                if ((bool)gameObject)
                {
                    gameObject.SetActive(on);
                }
            }
        }

        private void RepairRadio()
        {
            if ((bool)repairRadioNode)
            {
                repairRadioNode.SendMessage("PlayClip");
            }
        }
    }
}
