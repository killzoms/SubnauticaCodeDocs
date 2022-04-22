using System.Collections.Generic;
using UnityEngine;
using UWE;

namespace AssemblyCSharp
{
    public class CreatureDebugger : MonoBehaviour
    {
        private class CreatureLabel
        {
            public Creature creature;

            public GUIText guiText;
        }

        public static CreatureDebugger main;

        public bool debug;

        public float debugRadius = 20f;

        private float timeSinceFindCreatures;

        private TriggerStayTracker tracker;

        private int numDebugStrings;

        private List<CreatureLabel> labels = new List<CreatureLabel>();

        private void Start()
        {
            main = this;
            DevConsole.RegisterConsoleCommand(this, "debugcreatures");
            DevConsole.RegisterConsoleCommand(this, "dbc");
            GUIText[] componentsInChildren = base.gameObject.GetComponentsInChildren<GUIText>();
            foreach (GUIText guiText in componentsInChildren)
            {
                CreatureLabel creatureLabel = new CreatureLabel();
                creatureLabel.guiText = guiText;
                labels.Add(creatureLabel);
            }
        }

        public void OnConsoleCommand_dbc()
        {
            OnConsoleCommand_debugcreatures();
        }

        public void OnConsoleCommand_debugcreatures()
        {
            debug = !debug;
            ErrorMessage.AddDebug("Creature debugger now " + debug);
            if (debug)
            {
                GameObject gameObject = new GameObject();
                gameObject.name = "CreatureDebugger";
                gameObject.transform.parent = Player.main.transform;
                gameObject.transform.localPosition = Vector3.zero;
                tracker = gameObject.AddComponent<TriggerStayTracker>();
                tracker.componentFilter = "Creature";
                tracker.includeTriggers = true;
                SphereCollider sphereCollider = gameObject.AddComponent<SphereCollider>();
                sphereCollider.radius = 20f;
                sphereCollider.isTrigger = true;
            }
            else
            {
                Object.Destroy(tracker.gameObject);
                for (int i = 0; i < labels.Count; i++)
                {
                    labels[i].guiText.text = "";
                }
            }
        }

        private void AssignCreaturesToLabels()
        {
            for (int i = 0; i < labels.Count; i++)
            {
                CreatureLabel creatureLabel = labels[i];
                creatureLabel.creature = null;
                creatureLabel.guiText.text = "";
            }
            int num = 0;
            foreach (GameObject item in tracker.Get())
            {
                if (num < labels.Count)
                {
                    Creature component = item.GetComponent<Creature>();
                    if (component != null)
                    {
                        labels[num].creature = component;
                        num++;
                    }
                    continue;
                }
                break;
            }
            timeSinceFindCreatures = Time.time;
        }

        private void Update()
        {
            if (debug)
            {
                numDebugStrings = 0;
                if (Time.time > timeSinceFindCreatures + 0.5f)
                {
                    AssignCreaturesToLabels();
                }
                for (int i = 0; i < labels.Count; i++)
                {
                    UpdateCreatureLabel(labels[i], ref numDebugStrings);
                }
            }
        }

        private static void UpdateCreatureLabel(CreatureLabel label, ref int numDebugStrings)
        {
            if (!label.creature)
            {
                return;
            }
            CreatureAction lastAction = label.creature.GetLastAction();
            if (lastAction != null)
            {
                string text = lastAction.GetType().ToString();
                Vector3 vector = MainCamera.camera.WorldToScreenPoint(label.creature.gameObject.transform.position);
                float num = vector.x / (float)Screen.width;
                float num2 = vector.y / (float)Screen.height;
                label.guiText.transform.localPosition = new Vector3(num, num2, 0f);
                float a = 0.5f + (1f - Mathf.Clamp01((Time.time - label.creature.GetTimeLastActionSet()) / 2f)) * 0.5f;
                if (vector.z < 0f)
                {
                    a = 0f;
                }
                else if (numDebugStrings > 3)
                {
                    a = 0f;
                }
                else
                {
                    numDebugStrings++;
                }
                label.guiText.color = new Color(1f, 1f, 1f, a);
                label.guiText.text = text;
                if (Mathf.Abs(0.5f - num) < 0.2f && Mathf.Abs(0.5f - num2) < 0.2f)
                {
                    label.guiText.text = label.guiText.text + "\n" + label.creature.GetLastActionDebugString();
                }
            }
        }
    }
}
