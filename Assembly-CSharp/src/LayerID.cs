using UnityEngine;

namespace AssemblyCSharp
{
    public class LayerID
    {
        private static LayerID _instance;

        private int _Default;

        private int _Useable;

        private int _NotUseable;

        private int _Player;

        private int _TerrainCollider;

        private int _UI;

        private int _Interior;

        private static LayerID instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new LayerID
                    {
                        _Default = LayerMask.NameToLayer("Default"),
                        _Useable = LayerMask.NameToLayer("Useable"),
                        _NotUseable = LayerMask.NameToLayer("NotUseable"),
                        _Player = LayerMask.NameToLayer("Player"),
                        _TerrainCollider = LayerMask.NameToLayer("TerrainCollider"),
                        _UI = LayerMask.NameToLayer("UI"),
                        _Interior = LayerMask.NameToLayer("Interior")
                    };
                }
                return _instance;
            }
        }

        public static int Default => instance._Default;

        public static int Useable => instance._Useable;

        public static int NotUseable => instance._NotUseable;

        public static int Player => instance._Player;

        public static int TerrainCollider => instance._TerrainCollider;

        public static int UI => instance._UI;

        public static int Interior => instance._Interior;
    }
}
