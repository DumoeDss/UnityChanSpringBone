using UTJ.GameObjectExtensions;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UTJ
{
    public static class SpringBoneEditorActions
    {
        public static void ShowSpringBoneWindow()
        {
            SpringBoneWindow.ShowWindow();
        }

        public static void AssignSpringBonesRecursively()
        {
            if (Application.isPlaying)
            {
                Debug.LogError("请退出Play模式。");
                return;
            }

            if (Selection.gameObjects.Length < 1)
            {
                Debug.LogError("请至少选择一个对象。");
                return;
            }

            var springManagers = new HashSet<SpringManager>();
            foreach (var gameObject in Selection.gameObjects)
            {
                SpringBoneSetup.AssignSpringBonesRecursively(gameObject.transform);
                var manager = gameObject.GetComponentInParent<SpringManager>();
                if (manager != null)
                {
                    springManagers.Add(manager);
                }
            }

            foreach (var manager in springManagers)
            {
                SpringBoneSetup.FindAndAssignSpringBones(manager, true);
            }

            AssetDatabase.Refresh();
        }

        public static void CreatePivotForSpringBones()
        {
            if (Application.isPlaying)
            {
                Debug.LogError("请退出Play模式。");
                return;
            }

            if (Selection.gameObjects.Length < 1)
            {
                Debug.LogError("请至少选择一个对象。");
                return;
            }

            var selectedSpringBones = Selection.gameObjects
                .Select(gameObject => gameObject.GetComponent<SpringBone>())
                .Where(bone => bone != null);
            foreach (var springBone in selectedSpringBones)
            {
                SpringBoneSetup.CreateSpringPivotNode(springBone);
            }
        }

        public static void AddToOrUpdateSpringManagerInSelection()
        {
            if (Application.isPlaying)
            {
                Debug.LogError("请退出Play模式。");
                return;
            }

            if (Selection.gameObjects.Length <= 0)
            {
                Debug.LogError("请至少选择一个对象。");
                return;
            }

            foreach (var gameObject in Selection.gameObjects)
            {
                var manager = gameObject.GetComponent<SpringManager>();
                if (manager == null) { manager = gameObject.AddComponent<SpringManager>(); }
                SpringBoneSetup.FindAndAssignSpringBones(manager, true);
            }
        }

        public static void SelectChildSpringBones()
        {
            var springBoneObjects = Selection.gameObjects
                .SelectMany(gameObject => gameObject.GetComponentsInChildren<SpringBone>(true))
                .Select(bone => bone.gameObject)
                .Distinct()
                .ToArray();
            Selection.objects = springBoneObjects;
        }

        public static void DeleteSpringBonesAndManagers()
        {
            if (Application.isPlaying)
            {
                Debug.LogError("请退出Play模式。");
                return;
            }

            if (Selection.gameObjects.Length != 1)
            {
                Debug.LogError("只可以选择一个Root对象");
                return;
            }

            var rootObject = Selection.gameObjects.First();
            var queryMessage = 
            "确定要把以下对象及其子对象中的\n"
            + "SpringBone与SpringManager都删除吗？\n\n"
            +rootObject.name
            ;
            if (EditorUtility.DisplayDialog(
                "考虑清楚，真的要删掉嘛", queryMessage, "删除", "取消"))
            {
                SpringBoneSetup.DestroySpringManagersAndBones(rootObject);
                AssetDatabase.Refresh();
            }
        }

        public static void DeleteSelectedBones()
        {
            var springBonesToDelete = GameObjectUtil.FindComponentsOfType<SpringBone>()
                .Where(bone => Selection.gameObjects.Contains(bone.gameObject))
                .ToArray();
            var springManagersToUpdate = GameObjectUtil.FindComponentsOfType<SpringManager>()
                .Where(manager => manager.springBones.Any(bone => springBonesToDelete.Contains(bone)))
                .ToArray();
            Undo.RecordObjects(springManagersToUpdate, "Delete selected bones");
            foreach (var boneToDelete in springBonesToDelete)
            {
                Undo.DestroyObjectImmediate(boneToDelete);
            }
            foreach (var manager in springManagersToUpdate)
            {
                manager.FindSpringBones(true);
            }
        }

        public static void PromptToUpdateSpringBonesFromList()
        {
            if (Application.isPlaying)
            {
                Debug.LogError("Play过程中无法更新");
                return;
            }

            var selectedSpringManagers = Selection.gameObjects
                .Select(gameObject => gameObject.GetComponent<SpringManager>())
                .Where(manager => manager != null)
                .ToArray();
            if (!selectedSpringManagers.Any())
            {
                selectedSpringManagers = GameObjectUtil.FindComponentsOfType<SpringManager>().ToArray();
            }

            if (selectedSpringManagers.Count() != 1)
            {
                Debug.LogError("只可以选择一个SpringManager");
                return;
            }

            var springManager = selectedSpringManagers.First();
            var queryMessage = "要根据所选Manager的列表更新SpringBone？\n\n"
                + "不在列表中的SpringBone信息会被删除、\n"
                + "列表中存在，但是模型上没有的SpringBone将被添加。\n\n"
                + "SpringManager: " + springManager.name;
            if (EditorUtility.DisplayDialog("从所选Manager的列表更新", queryMessage, "更新", "取消"))
            {
                AutoSpringBoneSetup.UpdateSpringManagerFromBoneList(springManager);
            }
        }
    }
}