using System;
using System.Collections;
using LitJson;
using UnityEngine;
using UnityEngine.UI;

namespace AssemblyCSharp
{
    public class ConsoleMainMenuNewsController : MonoBehaviour
    {
        public ConsoleMainMenuNews newsPrefab;

        private GameObject newsGameObject;

        public void Reload(string url)
        {
            global::UnityEngine.Object.Destroy(newsGameObject);
            if (!PlatformUtils.isPS4Platform)
            {
                StartCoroutine(Load(url));
            }
        }

        private IEnumerator Start()
        {
            string url = (PlatformUtils.isPS4Platform ? "https://subnautica.unknownworlds.com/api/news/ps4" : ((!PlatformUtils.isXboxOnePlatform) ? "https://subnautica.unknownworlds.com/api/news/pc-new" : "https://subnautica.unknownworlds.com/api/news/xone"));
            yield return Load(url);
        }

        private IEnumerator Load(string url)
        {
            WWW rawData = new WWW(url);
            yield return rawData;
            if (rawData.error != null)
            {
                yield break;
            }
            IEnumerator enumerator = ((IEnumerable)JsonMapper.ToObject(rawData.text)).GetEnumerator();
            try
            {
                if (enumerator.MoveNext())
                {
                    JsonData jsonData = (JsonData)enumerator.Current;
                    ConsoleMainMenuNews consoleMainMenuNews = global::UnityEngine.Object.Instantiate(newsPrefab);
                    consoleMainMenuNews.transform.SetParent(base.transform, worldPositionStays: false);
                    consoleMainMenuNews.header.text = jsonData["header"].ToString();
                    consoleMainMenuNews.text.text = jsonData["text"].ToString();
                    consoleMainMenuNews.date.text = jsonData["created_at"].ToString();
                    consoleMainMenuNews.URL = jsonData["read_more_url"].ToString();
                    StartCoroutine(LoadImage(consoleMainMenuNews.image, jsonData["image_url"].ToString()));
                    newsGameObject = consoleMainMenuNews.gameObject;
                }
            }
            finally
            {
                IDisposable disposable = enumerator as IDisposable;
                if (disposable != null)
                {
                    disposable.Dispose();
                }
            }
        }

        private IEnumerator LoadImage(RawImage image, string URL)
        {
            WWW www = new WWW(URL);
            yield return www;
            image.texture = www.texture;
        }
    }
}
