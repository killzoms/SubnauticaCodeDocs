using System.IO;
using UnityEngine;

namespace AssemblyCSharp
{
    public class FileIOTest : MonoBehaviour
    {
        private void Awake()
        {
            StreamWriter streamWriter = new StreamWriter("foo.txt");
            streamWriter.WriteLine("fdsfdshello world");
            streamWriter.Close();
        }

        private void Start()
        {
        }

        private void Update()
        {
        }
    }
}
