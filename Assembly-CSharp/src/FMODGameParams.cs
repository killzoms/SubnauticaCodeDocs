using System;
using UnityEngine;

namespace AssemblyCSharp
{
    public class FMODGameParams : MonoBehaviour, ICompileTimeCheckable
    {
        public enum InteriorState
        {
            Always,
            OnlyOutside,
            OnlyInside,
            OnlyInSubOrBase,
            OnlyInSub,
            OnlyInBase
        }

        [AssertNotNull]
        public FMOD_CustomLoopingEmitter loopingEmitter;

        public bool alwaysActive;

        [Tooltip("Prefix matching, ignoring case")]
        public string onlyInBiome = "";

        public InteriorState interiorState = InteriorState.OnlyOutside;

        public bool debug;

        public bool isPlaying;

        private int depthParamIndex = -1;

        private int playerDamageParamIndex = -1;

        private int oxygenLeftParamIndex = -1;

        private int timeParamIndex = -1;

        private const string depthParamName = "depth";

        private const string playerDamageParamName = "playerDamage";

        private const string oxygenLeftParamName = "oxygenLeft";

        private const string timeParamName = "time";

        private void Start()
        {
            depthParamIndex = loopingEmitter.GetParameterIndex("depth");
            playerDamageParamIndex = loopingEmitter.GetParameterIndex("playerDamage");
            oxygenLeftParamIndex = loopingEmitter.GetParameterIndex("oxygenLeft");
            timeParamIndex = loopingEmitter.GetParameterIndex("time");
            if (oxygenLeftParamIndex != -1)
            {
                Utils.GetLocalPlayerComp().tookBreathEvent.AddHandler(base.gameObject, OnTookBreath);
            }
            InvokeRepeating("UpdateParams", 0f, 0.5f);
        }

        private void OnTookBreath(Player player)
        {
            if ((bool)player)
            {
                float oxygenAvailable = Player.main.GetOxygenAvailable();
                loopingEmitter.SetParameterValue(oxygenLeftParamIndex, oxygenAvailable);
                if (debug)
                {
                    Debug.Log(base.gameObject.name + ".FMODGameParams() - Setting \"oxygenLeft\" to " + oxygenAvailable);
                }
            }
        }

        private void UpdateParams()
        {
            if (!(loopingEmitter != null) || !base.gameObject.activeInHierarchy)
            {
                return;
            }
            bool flag = isPlaying;
            isPlaying = false;
            Player localPlayerComp = Utils.GetLocalPlayerComp();
            switch (interiorState)
            {
                case InteriorState.Always:
                    isPlaying = true;
                    break;
                case InteriorState.OnlyOutside:
                    isPlaying = !localPlayerComp.IsInsideWalkable();
                    break;
                case InteriorState.OnlyInside:
                    isPlaying = localPlayerComp.IsInsideWalkable();
                    break;
                case InteriorState.OnlyInSubOrBase:
                    isPlaying = localPlayerComp.IsInSub() && localPlayerComp.currentWaterPark == null;
                    break;
                case InteriorState.OnlyInSub:
                    isPlaying = localPlayerComp.IsInSubmarine();
                    break;
                case InteriorState.OnlyInBase:
                    isPlaying = localPlayerComp.IsInBase() && localPlayerComp.currentWaterPark == null;
                    break;
                default:
                    isPlaying = false;
                    break;
            }
            if (onlyInBiome.Length > 0)
            {
                string biomeString = localPlayerComp.GetBiomeString();
                isPlaying &= biomeString.StartsWith(onlyInBiome, StringComparison.OrdinalIgnoreCase);
            }
            isPlaying |= alwaysActive;
            if (isPlaying)
            {
                loopingEmitter.Play();
            }
            else
            {
                loopingEmitter.Stop();
            }
            DebugSoundConsoleCommand main = DebugSoundConsoleCommand.main;
            if (isPlaying != flag && (bool)main && main.debugMusic)
            {
                string arg = "<unknown>";
                if ((bool)loopingEmitter.asset)
                {
                    arg = loopingEmitter.asset.name;
                }
                ErrorMessage.AddDebug(string.Format("{0} {1} ('{2}*')", isPlaying ? "Playing" : "Stopping", arg, onlyInBiome));
            }
            if (!isPlaying)
            {
                return;
            }
            if (depthParamIndex != -1)
            {
                float y = Utils.GetLocalPlayerComp().transform.position.y;
                loopingEmitter.SetParameterValue(depthParamIndex, y);
                if (debug)
                {
                    Debug.Log(base.gameObject.name + ".FMODGameParams() - Setting \"depth\" to " + y);
                }
            }
            if (playerDamageParamIndex != -1)
            {
                float healthFraction = Utils.GetLocalPlayerComp().gameObject.GetComponent<LiveMixin>().GetHealthFraction();
                loopingEmitter.SetParameterValue(playerDamageParamIndex, healthFraction);
                if (debug)
                {
                    Debug.Log(base.gameObject.name + ".FMODGameParams() - Setting \"playerDamage\" to " + healthFraction);
                }
            }
            if (timeParamIndex != -1 && DayNightCycle.main != null)
            {
                loopingEmitter.SetParameterValue(timeParamIndex, DayNightCycle.main.GetDayScalar() * 24f);
            }
        }

        public string CompileTimeCheck()
        {
            return null;
        }
    }
}
