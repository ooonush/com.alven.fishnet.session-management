using System.Linq;
using FishNet.Object;
using UnityEditor;
using UnityEditorInternal;

namespace FishNet.Alven.SessionManagement
{
    [CustomEditor(typeof(NetworkSessionObject))]
    public class NetworkPlayerObjectEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var networkPlayerObject = (NetworkSessionObject)target;
            while (networkPlayerObject.GetComponents<NetworkBehaviour>().ToList().IndexOf(networkPlayerObject) != 0)
            {
                ComponentUtility.MoveComponentUp(networkPlayerObject);
            }

            base.OnInspectorGUI();
        }
    }
}