using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AssemblyCSharp
{
    public class EndCreditsManager : MonoBehaviour
    {
        [AssertNotNull]
        public Image logo;

        [AssertNotNull]
        public Transform creditsText;

        [AssertNotNull]
        public FMOD_CustomEmitter endMusic;

        [AssertNotNull]
        public GameObject easterEggHolder;

        [AssertNotNull]
        public FMOD_CustomEmitter easterEggVO;

        public AnimationCurve fadeCurve;

        public float secondsUntilScrollComplete;

        public int scrollMaxValue = 10275;

        public Text leftText;

        public Text centerText;

        public Text rightText;

        [TextArea(3, 10)]
        public string ps4CreditsLeft;

        [TextArea(3, 10)]
        public string ps4CreditsCenter;

        [TextArea(3, 10)]
        public string ps4CreditsRight;

        private bool scrollCredits;

        private bool fadeLogo;

        private Vector3 startPos;

        private Vector3 goToPos;

        private float startFadeTime;

        private float startScrollTime;

        private void Start()
        {
            if (PlatformUtils.isPS4Platform)
            {
                leftText.text = ps4CreditsLeft;
                centerText.text = ps4CreditsCenter;
                rightText.text = ps4CreditsRight;
            }
            fadeLogo = true;
            startFadeTime = Time.time;
            Invoke("PlayMusic", 0.5f);
            goToPos = new Vector3(0f, scrollMaxValue, 0f);
            StartCoroutine(ReturnToMainMenu());
        }

        private void EscHeld()
        {
            SceneManager.LoadSceneAsync("Cleaner", LoadSceneMode.Single);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Invoke("EscHeld", 1.5f);
            }
            else if (Input.GetKeyUp(KeyCode.Escape))
            {
                CancelInvoke("EscHeld");
            }
            if (fadeLogo)
            {
                float num = fadeCurve.Evaluate(Time.time - startFadeTime);
                logo.color = new Color(1f, 1f, 1f, num);
                if (num <= 0f && Time.time - startFadeTime > 1f)
                {
                    logo.gameObject.SetActive(value: false);
                    fadeLogo = false;
                    scrollCredits = true;
                    startScrollTime = Time.time;
                    startPos = creditsText.localPosition;
                }
            }
            if (scrollCredits)
            {
                float t = (Time.time - startScrollTime) / (secondsUntilScrollComplete - (startScrollTime - startFadeTime));
                creditsText.transform.localPosition = Vector3.Lerp(startPos, goToPos, t);
            }
        }

        private IEnumerator ReturnToMainMenu()
        {
            yield return new WaitForSeconds(secondsUntilScrollComplete - 3f);
            easterEggHolder.SetActive(value: true);
            easterEggVO.Play();
            yield return new WaitForSeconds(10.5f);
            yield return SceneManager.LoadSceneAsync("Cleaner", LoadSceneMode.Single);
        }

        private void PlayMusic()
        {
            endMusic.Play();
        }
    }
}
