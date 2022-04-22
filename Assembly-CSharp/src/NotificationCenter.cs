using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AssemblyCSharp
{
    public class NotificationCenter : MonoBehaviour
    {
        public class Notification
        {
            public Component sender;

            public string name;

            public Hashtable data;

            public C GetSenderComponent<C>() where C : Component
            {
                return sender.gameObject.GetComponent<C>();
            }

            public Notification(Component aSender, string aName)
            {
                sender = aSender;
                name = aName;
                data = null;
            }

            public Notification(Component aSender, string aName, Hashtable aData)
            {
                sender = aSender;
                name = aName;
                data = aData;
            }
        }

        private static NotificationCenter defaultCenter;

        private Hashtable notifications = new Hashtable();

        public static NotificationCenter DefaultCenter
        {
            get
            {
                if (!defaultCenter)
                {
                    defaultCenter = new GameObject("Default Notification Center").AddComponent<NotificationCenter>();
                }
                return defaultCenter;
            }
        }

        public void AddObserver(Component observer, string name)
        {
            AddObserver(observer, name, null);
        }

        public void AddObserver(Component observer, string name, Component sender)
        {
            if (string.IsNullOrEmpty(name))
            {
                Debug.Log("Null name specified for notification in AddObserver.");
                return;
            }
            if (notifications[name] == null)
            {
                notifications[name] = new List<Component>();
            }
            List<Component> list = notifications[name] as List<Component>;
            if (!list.Contains(observer))
            {
                list.Add(observer);
            }
        }

        public void RemoveObserver(Component observer, string name)
        {
            List<Component> list = (List<Component>)notifications[name];
            if (list != null)
            {
                if (list.Contains(observer))
                {
                    list.Remove(observer);
                }
                if (list.Count == 0)
                {
                    notifications.Remove(name);
                }
            }
        }

        public void PostNotification(Component aSender, string aName)
        {
            PostNotification(aSender, aName, null);
        }

        public void PostNotification(Component aSender, string aName, Hashtable aData)
        {
            PostNotification(new Notification(aSender, aName, aData));
        }

        public void PostNotification(Notification aNotification)
        {
            if (string.IsNullOrEmpty(aNotification.name))
            {
                Debug.Log("Null name sent to PostNotification.");
                return;
            }
            List<Component> list = (List<Component>)notifications[aNotification.name];
            if (list == null)
            {
                Debug.Log("Notify list not found in PostNotification: " + aNotification.name);
                return;
            }
            List<Component> list2 = new List<Component>();
            foreach (Component item in list)
            {
                if (!item)
                {
                    list2.Add(item);
                }
                else
                {
                    item.SendMessage(aNotification.name, aNotification, SendMessageOptions.DontRequireReceiver);
                }
            }
            foreach (Component item2 in list2)
            {
                list.Remove(item2);
            }
        }
    }
}
