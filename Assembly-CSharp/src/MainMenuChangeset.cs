using UnityEngine;
using UnityEngine.UI;

namespace AssemblyCSharp
{
    public class MainMenuChangeset : MonoBehaviour
    {
        private void Start()
        {
            string plasticChangeSetOfBuild = SNUtils.GetPlasticChangeSetOfBuild();
            if (!string.IsNullOrEmpty(plasticChangeSetOfBuild))
            {
                base.gameObject.GetComponent<Text>().text = "Changeset #" + plasticChangeSetOfBuild;
            }
        }
    }
}
