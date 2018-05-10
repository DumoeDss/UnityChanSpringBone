using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UTJ
{
    public class SaveSpringBoneSetupWindow : EditorWindow
    {
        public static void ShowWindow()
        {
            var editorWindow = GetWindow<SaveSpringBoneSetupWindow>(
                "导出SpringBone数据");
            if (editorWindow != null)
            {
                editorWindow.SelectObjectsFromSelection();
            }
        }

        // private

        private GameObject springBoneRoot;
        private SpringBoneSerialization.ExportSettings exportSettings;

        private void SelectObjectsFromSelection()
        {
            springBoneRoot = null;

            if (Selection.objects.Length > 0)
            {
                springBoneRoot = Selection.objects[0] as GameObject;
            }

            if (springBoneRoot == null)
            {
                var characterRootComponentTypes = new System.Type[] {
                    typeof(SpringManager),
                    typeof(Animation),
                    typeof(Animator)
                };
                springBoneRoot = characterRootComponentTypes
                    .Select(type => FindObjectOfType(type) as Component)
                    .Where(component => component != null)
                    .Select(component => component.gameObject)
                    .FirstOrDefault();
            }
        }

        private void ShowExportSettingsUI(ref Rect uiRect)
        {
            if (exportSettings == null)
            {
                exportSettings = new SpringBoneSerialization.ExportSettings();
            }

            GUI.Label(uiRect, "导出设置", SpringBoneGUIStyles.HeaderLabelStyle);
            uiRect.y += uiRect.height;
            exportSettings.ExportSpringBones = GUI.Toggle(uiRect, exportSettings.ExportSpringBones, "SpringBone", SpringBoneGUIStyles.ToggleStyle);
            uiRect.y += uiRect.height;
            exportSettings.ExportCollision = GUI.Toggle(uiRect, exportSettings.ExportCollision, "Collider", SpringBoneGUIStyles.ToggleStyle);
            uiRect.y += uiRect.height;
        }

        private void OnGUI()
        {
            SpringBoneGUIStyles.ReacquireStyles();

            const int ButtonHeight = 30;
            const int UISpacing = 8;
            const int UIRowHeight = 24;

            var uiWidth = (int)position.width - UISpacing * 2;
            var yPos = UISpacing;

            springBoneRoot = LoadSpringBoneSetupWindow.DoObjectPicker(
                "SpringBone的根节点", springBoneRoot, uiWidth, UIRowHeight, ref yPos);
            var buttonRect = new Rect(UISpacing, yPos, uiWidth, ButtonHeight);
            if (GUI.Button(buttonRect, "选择当前所选物体", SpringBoneGUIStyles.ButtonStyle))
            {
                SelectObjectsFromSelection();
            }
            yPos += ButtonHeight + UISpacing;
            buttonRect.y = yPos;

            ShowExportSettingsUI(ref buttonRect);
            if (springBoneRoot != null)
            {
                if (GUI.Button(buttonRect, "导出到CSV文件", SpringBoneGUIStyles.ButtonStyle))
                {
                    BrowseAndSaveSpringSetup();
                }
            }
        }

        private void BrowseAndSaveSpringSetup()
        {
            if (springBoneRoot == null) { return; }

            var initialFileName = springBoneRoot.name + "_Dynamics.csv";

            var path = EditorUtility.SaveFilePanel(
                "导出SpringBone数据", "", initialFileName, "csv");
            if (path.Length == 0) { return; }

            if (System.IO.File.Exists(path))
            {
                var overwriteMessage = "文件已存在，是否覆盖？\n\n" + path;
                if (!EditorUtility.DisplayDialog("Save SpringBone", overwriteMessage, "覆盖", "取消"))
                {
                    return;
                }
            }

            var sourceText = SpringBoneSerialization.BuildDynamicsSetupString(springBoneRoot, exportSettings);
            if (FileUtil.WriteAllText(path, sourceText))
            {
                AssetDatabase.Refresh();
                Debug.Log("数据已保存: " + path);
            }
        }
    }
}