using System.IO;
using FMOD;
using FMOD.Studio;
using UnityEngine;

namespace FMODUnity
{
    public static class RuntimeUtils
    {
        private const string BankExtension = ".bank";

        public static VECTOR ToFMODVector(this Vector3 vec)
        {
            VECTOR result = default(VECTOR);
            result.x = vec.x;
            result.y = vec.y;
            result.z = vec.z;
            return result;
        }

        public static ATTRIBUTES_3D To3DAttributes(this Vector3 pos)
        {
            ATTRIBUTES_3D result = default(ATTRIBUTES_3D);
            result.forward = Vector3.forward.ToFMODVector();
            result.up = Vector3.up.ToFMODVector();
            result.position = pos.ToFMODVector();
            return result;
        }

        public static ATTRIBUTES_3D To3DAttributes(this Transform transform)
        {
            ATTRIBUTES_3D result = default(ATTRIBUTES_3D);
            result.forward = transform.forward.ToFMODVector();
            result.up = transform.up.ToFMODVector();
            result.position = transform.position.ToFMODVector();
            return result;
        }

        public static ATTRIBUTES_3D To3DAttributes(Transform transform, Rigidbody rigidbody = null)
        {
            ATTRIBUTES_3D result = transform.To3DAttributes();
            if ((bool)rigidbody)
            {
                result.velocity = rigidbody.velocity.ToFMODVector();
            }
            return result;
        }

        public static ATTRIBUTES_3D To3DAttributes(GameObject go, Rigidbody rigidbody = null)
        {
            ATTRIBUTES_3D result = go.transform.To3DAttributes();
            if ((bool)rigidbody)
            {
                result.velocity = rigidbody.velocity.ToFMODVector();
            }
            return result;
        }

        public static ATTRIBUTES_3D To3DAttributes(Transform transform, Rigidbody2D rigidbody)
        {
            ATTRIBUTES_3D result = transform.To3DAttributes();
            if ((bool)rigidbody)
            {
                VECTOR velocity = default(VECTOR);
                velocity.x = rigidbody.velocity.x;
                velocity.y = rigidbody.velocity.y;
                velocity.z = 0f;
                result.velocity = velocity;
            }
            return result;
        }

        public static ATTRIBUTES_3D To3DAttributes(GameObject go, Rigidbody2D rigidbody)
        {
            ATTRIBUTES_3D result = go.transform.To3DAttributes();
            if ((bool)rigidbody)
            {
                VECTOR velocity = default(VECTOR);
                velocity.x = rigidbody.velocity.x;
                velocity.y = rigidbody.velocity.y;
                velocity.z = 0f;
                result.velocity = velocity;
            }
            return result;
        }

        internal static FMODPlatform GetCurrentPlatform()
        {
            return FMODPlatform.Windows;
        }

        internal static string GetBankPath(string bankName)
        {
            string streamingAssetsPath = Application.streamingAssetsPath;
            if (Path.GetExtension(bankName) != ".bank")
            {
                return $"{streamingAssetsPath}/{bankName}.bank";
            }
            return $"{streamingAssetsPath}/{bankName}";
        }

        internal static string GetPluginPath(string pluginName)
        {
            string text = pluginName + ".dll";
            return string.Concat(Application.dataPath + "/Plugins/", text);
        }

        public static void EnforceLibraryOrder()
        {
            Memory.GetStats(out var _, out var _);
            Util.ParseID("", out var _);
        }
    }
}
