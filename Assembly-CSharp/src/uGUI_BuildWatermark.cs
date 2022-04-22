using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace AssemblyCSharp
{
    [RequireComponent(typeof(Text))]
    public class uGUI_BuildWatermark : MonoBehaviour
    {
        private IEnumerator Start()
        {
            UpdateText();
            Language.main.OnLanguageChanged += OnLanguageChanged;
            while (!LightmappedPrefabs.main || LightmappedPrefabs.main.IsWaitingOnLoads() || !uGUI.main || !uGUI.main.loading || uGUI.main.loading.IsLoading || !PAXTerrainController.main || PAXTerrainController.main.isWorking)
            {
                yield return null;
            }
            base.gameObject.SetActive(value: false);
        }

        private void OnDestroy()
        {
            Language.main.OnLanguageChanged -= OnLanguageChanged;
        }

        private void OnLanguageChanged()
        {
            UpdateText();
        }

        private void UpdateText()
        {
            Text component = GetComponent<Text>();
            string plasticChangeSetOfBuild = SNUtils.GetPlasticChangeSetOfBuild();
            DateTime dateTimeOfBuild = SNUtils.GetDateTimeOfBuild();
            component.text = Language.main.GetFormat("EarlyAccessWatermarkFormat", dateTimeOfBuild, plasticChangeSetOfBuild);
        }
    }
}
