using Gendarme;
using UnityEngine;

namespace AssemblyCSharp
{
    public class uGUI_PDATab : MonoBehaviour
    {
        protected uGUI_PDA pda;

        protected GameObject go;

        public virtual int notificationsCount => 0;

        protected virtual void Awake()
        {
            go = base.gameObject;
        }

        public void Register(uGUI_PDA pda)
        {
            this.pda = pda;
        }

        public virtual void OnOpenPDA()
        {
        }

        public virtual void OnClosePDA()
        {
        }

        public virtual uGUI_INavigableIconGrid GetInitialGrid()
        {
            return null;
        }

        public virtual void Open()
        {
            go.SetActive(value: true);
        }

        public virtual void Close()
        {
            go.SetActive(value: false);
        }

        public virtual void OnLanguageChanged()
        {
        }

        [SuppressMessage("Gendarme.Rules.Naming", "AvoidRedundancyInMethodNameRule")]
        public virtual bool OnButtonDown(GameInput.Button button)
        {
            switch (button)
            {
                case GameInput.Button.UINextTab:
                    pda.OpenTab(pda.GetNextTab());
                    return true;
                case GameInput.Button.UIPrevTab:
                    pda.OpenTab(pda.GetPreviousTab());
                    return true;
                case GameInput.Button.UICancel:
                    ClosePDA();
                    return true;
                default:
                    return false;
            }
        }

        protected void ClosePDA()
        {
            Player.main.GetPDA().Close();
        }
    }
}
