using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace AssemblyCSharp
{
    public class MainMenuEmailHandler : MonoBehaviour
    {
        private string email;

        public string emailUrl;

        public GameObject inputfield;

        public GameObject subscribing;

        public GameObject success;

        public GameObject error;

        public void Subscribe()
        {
            subscribing.SetActive(value: true);
            Debug.Log("Main Menu: Beginning email subscription process");
            email = inputfield.GetComponent<InputField>().text;
            WWWForm wWWForm = new WWWForm();
            wWWForm.AddField("email", email);
            wWWForm.AddField("platform", PlatformUtils.isConsolePlatform ? "xone" : "pc");
            WWW w = new WWW(emailUrl, wWWForm);
            StartCoroutine(sendEmail(w));
        }

        private IEnumerator sendEmail(WWW w)
        {
            yield return w;
            if (!string.IsNullOrEmpty(w.error))
            {
                subscribing.SetActive(value: false);
                error.SetActive(value: true);
                Debug.LogFormat("Main Menu: Error in sending new email subscription from main menu! - {0}", w.error);
            }
            else
            {
                subscribing.SetActive(value: false);
                success.SetActive(value: true);
                Debug.LogFormat("Main Menu: Backend response to email subscription: {0}", w.text);
            }
        }
    }
}
