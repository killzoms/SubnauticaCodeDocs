using Steamworks;
using UnityEngine;

namespace AssemblyCSharp
{
    public class econ_0001_testPlaque_auth : MonoBehaviour
    {
        private SteamEconomy steamEconomy;

        public TextMesh hullNumber;

        public TextMesh hullDate;

        private string strHullNumber;

        private string strHullDate;

        private void Start()
        {
            steamEconomy = GameObject.FindWithTag("SteamEconomy").GetComponent<SteamEconomy>();
            string classId = "1";
            if (steamEconomy.HasItem(classId))
            {
                Debug.Log("Steamworks | Economy: User has ownership of item econ_001_testPlaque.");
                strHullNumber = steamEconomy.GetItemProperty(classId, "serial_number").ToString() + " / 10,000";
                hullNumber.text = strHullNumber;
                strHullDate = steamEconomy.GetItemProperty(classId, "created_at");
                strHullDate = strHullDate.Substring(0, 4) + " / " + strHullDate.Substring(5, 2) + " / " + strHullDate.Substring(8, 2);
                hullDate.text = strHullDate;
            }
            else
            {
                Debug.Log("Steamworks | Economy: User with Steam ID " + SteamUser.GetSteamID().ToString() + " does not have ownership of item econ_001_testPlaque.");
                hullDate.text = "Error";
                hullNumber.text = "Error";
            }
        }
    }
}
