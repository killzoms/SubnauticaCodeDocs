using System.Collections;
using LitJson;
using UnityEngine;

namespace AssemblyCSharp
{
    public class EconomyItemsSteam : EconomyItems
    {
        private readonly string assetServerUrl = "https://economy.unknownworlds.com/";

        private Hashtable itemList = new Hashtable();

        private string steamId;

        public EconomyItemsSteam(string _steamId)
        {
            steamId = _steamId;
        }

        public bool HasItem(string id)
        {
            return itemList.Contains(id);
        }

        public string GetItemProperty(string class_id, string property)
        {
            if (!HasItem(class_id))
            {
                return "";
            }
            Hashtable hashtable = (Hashtable)itemList[class_id];
            if (hashtable.Contains(property))
            {
                return hashtable[property].ToString();
            }
            return "";
        }

        public IEnumerator RefreshAsync()
        {
            string url = assetServerUrl + "api/GetContexts/v0001?appid=848450&steamid=" + steamId + "&parent=0";
            WWW www = new WWW(url);
            yield return www;
            if (www.isDone && www.error == null && JsonMapper.ToObject(www.text)["result"]["contexts"].Count > 0)
            {
                JsonData context = JsonMapper.ToObject(www.text)["result"]["contexts"][0];
                yield return UpdateItemsListAsync(context);
            }
        }

        private IEnumerator UpdateItemsListAsync(JsonData context)
        {
            string url = string.Concat(assetServerUrl, "api/GetContextContents/v0001?appid=848450&steamid=", steamId, "&contextid=", context["id"], "&include_dates=true");
            WWW www = new WWW(url);
            yield return www;
            if (!www.isDone || www.error != null)
            {
                yield break;
            }
            foreach (JsonData item in (IEnumerable)JsonMapper.ToObject(www.text)["result"]["assets"])
            {
                string text = null;
                Hashtable hashtable = new Hashtable();
                foreach (JsonData item2 in (IEnumerable)item["class"])
                {
                    if (item2["name"].ToString() == "base_class")
                    {
                        text = item2["value"].ToString();
                    }
                    else if (item2["name"].ToString() == "class_id" && text == null)
                    {
                        text = item2["value"].ToString();
                    }
                    hashtable.Add(item2["name"].ToString(), item2["value"].ToString());
                }
                if (!itemList.Contains(text))
                {
                    itemList.Add(text, hashtable);
                }
            }
        }
    }
}
