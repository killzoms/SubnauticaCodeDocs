using System;
using Gendarme;
using UnityEngine;

namespace AssemblyCSharp.UnityEngine.PostProcessing
{
    [Serializable]
    [SuppressMessage("Gendarme.Rules.Naming", "AvoidRedundancyInTypeNameRule")]
    public abstract class PostProcessingModel
    {
        [SerializeField]
        [GetSet("enabled")]
        private bool m_Enabled;

        public bool enabled
        {
            get
            {
                return m_Enabled;
            }
            set
            {
                m_Enabled = value;
                if (value)
                {
                    OnValidate();
                }
            }
        }

        public abstract void Reset();

        public virtual void OnValidate()
        {
        }
    }
}
