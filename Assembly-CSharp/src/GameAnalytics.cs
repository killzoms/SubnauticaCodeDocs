using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using LitJson;
using UnityEngine;

namespace AssemblyCSharp
{
    public class GameAnalytics
    {
        public enum Event
        {
            None,
            NewGameStarted,
            GameLoaded,
            FeedbackSent,
            MusicPlayed,
            TechConstructed,
            TechCrafted,
            Death,
            BlueprintUnlocked,
            FirstUnlockedCreate,
            Goal,
            LegacyFeedback,
            LegacyMusic,
            LegacyConstruct,
            LegacyGoal
        }

        public struct EventInfo
        {
            public string name;

            public TelemetryEventCategory category;

            public EventFlag flags;

            public bool HasFlag(EventFlag check)
            {
                return (flags & check) != 0;
            }
        }

        [Flags]
        public enum EventFlag
        {
            None = 0x0,
            Disabled = 0x1,
            EditorDisableOnCheat = 0x2,
            BuildDisableOnCheat = 0x4,
            DisableOnCheat = 0x6
        }

        [Serializable]
        public class EventInfoCollection : Dictionary<Event, EventInfo>
        {
            public void Add(Event eventId, string name, TelemetryEventCategory category, EventFlag flags = EventFlag.None)
            {
                Add(eventId, new EventInfo
                {
                    name = name,
                    category = category,
                    flags = flags
                });
            }

            public EventInfoCollection()
            {
            }

            protected EventInfoCollection(SerializationInfo info, StreamingContext context)
                : base(info, context)
            {
            }
        }

        public class EventData : IDisposable
        {
            private static readonly EventData singleton = new EventData();

            private Event eventId;

            private StringBuilder sb;

            private JsonWriter writer;

            private EventData()
            {
                sb = new StringBuilder(255);
                writer = new JsonWriter(sb);
            }

            public static EventData Initialize(Event eventId)
            {
                singleton.Reset();
                singleton.eventId = eventId;
                singleton.writer.WriteObjectStart();
                return singleton;
            }

            public void Add(string name, bool value)
            {
                writer.WritePropertyName(name);
                writer.Write(value);
            }

            public void Add(string name, decimal value)
            {
                writer.WritePropertyName(name);
                writer.Write(value);
            }

            public void Add(string name, double value)
            {
                writer.WritePropertyName(name);
                writer.Write(value);
            }

            public void Add(string name, int value)
            {
                writer.WritePropertyName(name);
                writer.Write(value);
            }

            public void Add(string name, long value)
            {
                writer.WritePropertyName(name);
                writer.Write(value);
            }

            public void Add(string name, string value)
            {
                writer.WritePropertyName(name);
                writer.Write(value);
            }

            public void Add(string name, ulong value)
            {
                writer.WritePropertyName(name);
                writer.Write(value);
            }

            public void AddPosition(Vector3 position)
            {
                Add("x", position.x);
                Add("y", position.y);
                Add("z", position.z);
            }

            private void Reset()
            {
                eventId = Event.None;
                writer.Reset();
                sb.Length = 0;
            }

            public void Dispose()
            {
                try
                {
                    writer.WriteObjectEnd();
                    Send(eventId, sb.ToString());
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
                finally
                {
                    Reset();
                }
            }
        }

        private static readonly EventInfoCollection definitions = new EventInfoCollection
        {
            {
                Event.NewGameStarted,
                "NewGameStarted",
                TelemetryEventCategory.Sessions
            },
            {
                Event.GameLoaded,
                "GameLoaded",
                TelemetryEventCategory.Sessions
            },
            {
                Event.FeedbackSent,
                "FeedbackSent",
                TelemetryEventCategory.Other
            },
            {
                Event.MusicPlayed,
                "MusicPlayed",
                TelemetryEventCategory.Music,
                EventFlag.DisableOnCheat
            },
            {
                Event.TechConstructed,
                "TechConstructed",
                TelemetryEventCategory.Tech,
                EventFlag.DisableOnCheat
            },
            {
                Event.TechCrafted,
                "TechCrafted",
                TelemetryEventCategory.Tech,
                EventFlag.DisableOnCheat
            },
            {
                Event.Death,
                "Death",
                TelemetryEventCategory.Other,
                EventFlag.DisableOnCheat
            },
            {
                Event.BlueprintUnlocked,
                "BlueprintUnlocked",
                TelemetryEventCategory.Tech,
                EventFlag.DisableOnCheat
            },
            {
                Event.FirstUnlockedCreate,
                "FirstUnlockedCreate",
                TelemetryEventCategory.Tech,
                EventFlag.DisableOnCheat
            },
            {
                Event.Goal,
                "Goal",
                TelemetryEventCategory.Story,
                EventFlag.DisableOnCheat
            },
            {
                Event.LegacyFeedback,
                "Feedback_{0}",
                TelemetryEventCategory.Other,
                EventFlag.DisableOnCheat | EventFlag.Disabled
            },
            {
                Event.LegacyMusic,
                "MusicPlayed_{0}",
                TelemetryEventCategory.Music,
                EventFlag.DisableOnCheat | EventFlag.Disabled
            },
            {
                Event.LegacyConstruct,
                "Tech_{0}_3_Constructed",
                TelemetryEventCategory.Tech,
                EventFlag.DisableOnCheat | EventFlag.Disabled
            },
            {
                Event.LegacyGoal,
                "StoryGoal_{0}",
                TelemetryEventCategory.Story,
                EventFlag.DisableOnCheat | EventFlag.Disabled
            }
        };

        public static EventData CustomEvent(Event eventId)
        {
            return EventData.Initialize(eventId);
        }

        public static void SimpleEvent(Event eventId)
        {
            Send(eventId);
        }

        private static void Send(Event eventId, string data = null)
        {
            if (definitions.TryGetValue(eventId, out var value))
            {
                Send(value, data);
                return;
            }
            Debug.LogErrorFormat("No event definition found for analytics event '{0}' - it should be defined in GameAnalytics.definitions!", eventId);
        }

        private static void Send(EventInfo eventInfo, string data)
        {
            bool num = eventInfo.HasFlag(EventFlag.Disabled);
            bool flag = eventInfo.HasFlag(Application.isEditor ? EventFlag.EditorDisableOnCheat : EventFlag.BuildDisableOnCheat);
            bool flag2 = !GameModeUtils.AllowsAchievements() || DevConsole.HasUsedConsole();
            bool num2 = num || (flag && flag2);
            if (data == null)
            {
                data = "null";
            }
            if (!num2)
            {
                try
                {
                    Telemetry.Instance.SendAnalyticsEvent(eventInfo.category, eventInfo.name, data);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }
        }

        public static void LegacyEvent(Event eventId, string data)
        {
            if (definitions.TryGetValue(eventId, out var value))
            {
                value.name = string.Format(value.name, data);
                Send(value, "null");
            }
            else
            {
                Debug.LogErrorFormat("No event definition found for analytics event '{0}' - it should be defined in GameAnalytics.definitions!", eventId);
            }
        }
    }
}
