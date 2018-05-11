using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UTJ
{
    public class SpringBoneSetupErrorWindow : EditorWindow
    {
        public interface IConfirmAction
        {
            void Perform();
        }

        public static void ShowWindow
        (
            GameObject springBoneRoot,
            GameObject colliderRoot,
            string path, 
            IEnumerable<DynamicsSetup.ParseMessage> errors, 
            IConfirmAction onConfirm
        )
        {
            var window = GetWindow<SpringBoneSetupErrorWindow>("DynamicsSetup");
            window.springBoneRoot = springBoneRoot;
            window.colliderRoot = colliderRoot;
            window.filePath = path;
            window.onConfirmAction = onConfirm;
            window.errors = errors.ToArray();
        }

        // private

        private GameObject springBoneRoot;
        private GameObject colliderRoot;
        private string filePath;
        private IConfirmAction onConfirmAction;
        private DynamicsSetup.ParseMessage[] errors;
        private Vector2 scrollPosition;

        private void OnGUI()
        {
            EditorGUILayout.Space();
            GUILayout.Label("DynamicsSetup中有一些错误，是否只创建正常的部分？");
            EditorGUILayout.Space();
            EditorGUILayout.ObjectField("SpringBone的根节点", springBoneRoot, typeof(GameObject), true);
            EditorGUILayout.ObjectField("Collider的根节点", colliderRoot, typeof(GameObject), true);
            EditorGUILayout.TextField("Path", filePath);
            EditorGUILayout.Space();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("创建"))
            {
                onConfirmAction.Perform();
                Close();
            }
            if (GUILayout.Button("取消")) { Close(); }
            GUILayout.EndHorizontal();
            EditorGUILayout.Space();

            GUILayout.Label("Error");
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, true);
            foreach (var error in errors)
            {
                var errorString = error.Message;
                if (!string.IsNullOrEmpty(error.SourceLine))
                {
                    errorString += "\n" + error.SourceLine;
                }
                GUILayout.Label(errorString);
            }
            GUILayout.EndScrollView();
        }
    }
}