using System.Collections;
using UnityEngine;

namespace AssemblyCSharp
{
    public class SteamEconomy : MonoBehaviour
    {
        public static SteamEconomy main;

        private EconomyItems economyItems;

        private IEnumerator Start()
        {
            main = this;
            PlatformServices services = PlatformUtils.main.GetServices();
            economyItems = services.GetEconomyItems();
            if (economyItems != null)
            {
                yield return economyItems.RefreshAsync();
                UnlockItems();
            }
        }

        public bool HasItem(string classId)
        {
            if (economyItems != null)
            {
                return economyItems.HasItem(classId);
            }
            return false;
        }

        public string GetItemProperty(string classId, string property)
        {
            if (economyItems != null)
            {
                return economyItems.GetItemProperty(classId, property);
            }
            return string.Empty;
        }

        private void UnlockItems()
        {
            if (economyItems.HasItem("1"))
            {
                KnownTech.Add(TechType.SpecialHullPlate, verbose: false);
            }
            if (economyItems.HasItem("1388"))
            {
                KnownTech.Add(TechType.DevTestItem, verbose: false);
            }
            if (economyItems.HasItem("1389"))
            {
                KnownTech.Add(TechType.BikemanHullPlate, verbose: false);
            }
            if (economyItems.HasItem("1390"))
            {
                KnownTech.Add(TechType.EatMyDictionHullPlate, verbose: false);
            }
            if (economyItems.HasItem("1391"))
            {
                KnownTech.Add(TechType.DioramaHullPlate, verbose: false);
            }
            if (economyItems.HasItem("1392"))
            {
                KnownTech.Add(TechType.MarkiplierHullPlate, verbose: false);
            }
            if (economyItems.HasItem("1393"))
            {
                KnownTech.Add(TechType.MuyskermHullPlate, verbose: false);
            }
            if (economyItems.HasItem("1394"))
            {
                KnownTech.Add(TechType.LordMinionHullPlate, verbose: false);
            }
            if (economyItems.HasItem("1395"))
            {
                KnownTech.Add(TechType.JackSepticEyeHullPlate, verbose: false);
            }
            if (economyItems.HasItem("1397"))
            {
                KnownTech.Add(TechType.IGPHullPlate, verbose: false);
            }
            if (economyItems.HasItem("1493"))
            {
                KnownTech.Add(TechType.GilathissHullPlate, verbose: false);
            }
            if (economyItems.HasItem("1494"))
            {
                KnownTech.Add(TechType.Marki1, verbose: false);
            }
            if (economyItems.HasItem("1495"))
            {
                KnownTech.Add(TechType.Marki2, verbose: false);
            }
            if (economyItems.HasItem("1496"))
            {
                KnownTech.Add(TechType.JackSepticEye, verbose: false);
            }
            if (economyItems.HasItem("1497"))
            {
                KnownTech.Add(TechType.EatMyDiction, verbose: false);
            }
        }
    }
}
