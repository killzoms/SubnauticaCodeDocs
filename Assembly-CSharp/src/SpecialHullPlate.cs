using System;
using UnityEngine;
using UnityEngine.UI;

namespace AssemblyCSharp
{
    public class SpecialHullPlate : MonoBehaviour
    {
        public Text serialNumber;

        public Text date;

        private void Start()
        {
            string classId = "1";
            serialNumber.text = SteamEconomy.main.GetItemProperty(classId, "serial_number").PadLeft(5, '0');
            DateTime.TryParse(SteamEconomy.main.GetItemProperty(classId, "created_at"), out var result);
            date.text = $"{result:dd/MM/yyyy}";
        }
    }
}
