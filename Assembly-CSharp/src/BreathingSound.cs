using UnityEngine;

namespace AssemblyCSharp
{
    [RequireComponent(typeof(FMOD_StudioEventEmitter))]
    public class BreathingSound : MonoBehaviour
    {
        public FMOD_StudioEventEmitter loopingBreathingSound;

        private Player player;

        private int fmodIndexDepth = -1;

        private int fmodIndexRebreather = -1;

        private void Start()
        {
            Utils.GetLocalPlayer().GetComponent<Player>().tookBreathEvent.AddHandler(this, OnTookBreath);
            InvokeRepeating("UpdateSound", 0.25f, 0.25f);
        }

        private void UpdateSound()
        {
            if (!base.gameObject.activeInHierarchy)
            {
                return;
            }
            if (player == null)
            {
                player = Player.main;
            }
            bool flag = player.IsUnderwater();
            OxygenManager oxygenMgr = Player.main.oxygenMgr;
            bool num = oxygenMgr != null && oxygenMgr.HasOxygenTank();
            Player.Mode mode = player.GetMode();
            if (num && mode == Player.Mode.Normal && flag)
            {
                if (!loopingBreathingSound.GetIsStartingOrPlaying())
                {
                    loopingBreathingSound.StartEvent();
                }
                if (fmodIndexDepth < 0)
                {
                    fmodIndexDepth = loopingBreathingSound.GetParameterIndex("depth");
                }
                if (fmodIndexRebreather < 0)
                {
                    fmodIndexRebreather = loopingBreathingSound.GetParameterIndex("rebreather");
                }
                loopingBreathingSound.SetParameterValue(fmodIndexDepth, player.transform.position.y);
                float value = ((Inventory.Get().equipment.GetCount(TechType.Rebreather) > 0) ? 1f : 0f);
                loopingBreathingSound.SetParameterValue(fmodIndexRebreather, value);
            }
            else if (loopingBreathingSound.GetIsPlaying())
            {
                loopingBreathingSound.Stop();
            }
        }

        public void OnTookBreath(Player player)
        {
        }
    }
}
