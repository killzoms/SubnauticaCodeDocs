using UnityEngine;
using UnityEngine.UI;

namespace AssemblyCSharp
{
    public class CyclopsDecoyLaunchButton : MonoBehaviour, ICyclopsAbility
    {
        [AssertNotNull]
        public SubRoot subRoot;

        [AssertNotNull]
        public Animator animator;

        [AssertNotNull]
        public Button button;

        [AssertNotNull]
        public FMOD_CustomEmitter soundFX;

        [AssertNotNull]
        public CyclopsDecoyManager decoyManager;

        [AssertNotNull]
        public Text decoyCountText;

        [AssertNotNull]
        public Image buttonImage;

        private float cooldown;

        private bool active = true;

        private bool mouseHover;

        private void Update()
        {
            if (mouseHover)
            {
                HandReticle.main.SetInteractText("CyclopsLaunchDecoy");
            }
        }

        public void OnMouseEnter()
        {
            if (!(Player.main.currentSub != subRoot) && active)
            {
                mouseHover = true;
            }
        }

        public void OnMouseExit()
        {
            if (!(Player.main.currentSub != subRoot))
            {
                HandReticle.main.SetIcon(HandReticle.IconType.Default);
                mouseHover = false;
            }
        }

        private void Start()
        {
            cooldown = subRoot.decoyCD;
            UpdateText();
        }

        public void OnClick()
        {
            if (!(Player.main.currentSub != subRoot) && decoyManager.TryLaunchDecoy())
            {
                StartCooldown();
            }
        }

        public void UpdateText()
        {
            decoyCountText.text = GetDecoyString();
            if (decoyManager.decoyCount == 0)
            {
                buttonImage.color = new Color(1f, 1f, 1f, 0.25f);
                button.enabled = false;
                active = false;
            }
            else
            {
                buttonImage.color = new Color(1f, 1f, 1f, 1f);
                ResetCooldown();
            }
        }

        public void StartCooldown()
        {
            button.enabled = false;
            active = false;
            mouseHover = false;
            animator.SetBool("DisabledState", value: true);
            Invoke("DoResetCooldown", cooldown);
        }

        private void DoResetCooldown()
        {
            ResetCooldown();
        }

        public void ResetCooldown()
        {
            if (decoyManager.decoyCount > 0)
            {
                active = true;
                animator.SetBool("DisabledState", value: false);
                animator.SetTrigger("Normal");
                button.enabled = true;
            }
        }

        private string GetDecoyString()
        {
            int decoyCount = decoyManager.decoyCount;
            int decoyMax = decoyManager.decoyMax;
            return $"<color=#FFFFFFFF><size=60>{decoyCount}</size></color><color=#F6AF2BFF><size=45>/{decoyMax}</size></color>";
        }
    }
}
