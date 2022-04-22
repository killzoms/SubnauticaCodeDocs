using Gendarme;
using UnityEngine;

namespace AssemblyCSharp.UnityEngine.PostProcessing
{
    [SuppressMessage("Gendarme.Rules.Naming", "UseCorrectSuffixRule")]
    public sealed class GetSetAttribute : PropertyAttribute
    {
        public readonly string name;

        public bool dirty;

        public GetSetAttribute(string name)
        {
            this.name = name;
        }
    }
}
