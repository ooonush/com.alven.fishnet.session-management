using System.Linq;
using FishNet.Object;
using UnityEditor;
using UnityEditorInternal;

namespace FishNet.Alven.SessionManagement.Editor
{
    [CustomEditor(typeof(NetworkSessionObject))]
    public class NetworkPlayerObjectEditor : UnityEditor.Editor
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