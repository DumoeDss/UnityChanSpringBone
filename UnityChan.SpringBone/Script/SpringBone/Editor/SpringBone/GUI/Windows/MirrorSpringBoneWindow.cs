﻿using UTJ.GameObjectExtensions;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UTJ
{
    public class MirrorSpringBoneWindow : EditorWindow
    {
        public static void ShowWindow()
        {
            var window = GetWindow<MirrorSpringBoneWindow>("SpringBone镜像");
            window.Show();
            window.OnShow();
        }

        // private

        public enum Axis { X, Y, Z,Null }

        private class BoneEntry
        {
            public BoneEntry(SpringBone newSourceBone, SpringBone newTargetBone)
            {
                sourceBone = newSourceBone;
                targetBone = newTargetBone;
            }

            public SpringBone sourceBone;
            public SpringBone targetBone;

            public void ShowEntryUI(Rect rect)
            {
                var halfWidth = 0.5f * rect.width;
                var sourceRect = new Rect(rect.x, rect.y, halfWidth, rect.height);
                sourceBone = (SpringBone)EditorGUI.ObjectField(sourceRect, sourceBone, typeof(SpringBone), true);
                var targetRect = new Rect(rect.x + halfWidth, rect.y, halfWidth, rect.height);
                targetBone = (SpringBone)EditorGUI.ObjectField(targetRect, targetBone, typeof(SpringBone), true);
            }
        }

        private const float Spacing = 8f;
        private const float RowHeight = 30f;

        public Axis mirrorAxis = Axis.Null;
        private List<BoneEntry> boneEntries;
        private Vector2 scrollPosition;

        private void OnGUI()
        {
            SpringBoneGUIStyles.ReacquireStyles();

            if (boneEntries == null) { AcquireBonesFromSelection(); }

            var uiRect = new Rect(Spacing, Spacing, position.width - Spacing * 2f, RowHeight);
            uiRect = ShowUtilityButtons(uiRect);
            uiRect = ShowBoneList(uiRect);
            if (GUI.Button(uiRect, "执行镜像", SpringBoneGUIStyles.ButtonStyle))
            {
                PerformMirror();
            }
        }
        bool  isX = false, isY = false, isZ = false;
        private Rect ShowUtilityButtons(Rect uiRect)
        {
            var buttonOffset = uiRect.height + Spacing;
            if (GUI.Button(uiRect, "添加当前所选SpringBone到复制源列表", SpringBoneGUIStyles.ButtonStyle))
            {
                AcquireBonesFromSelection();
            }
            uiRect.y += buttonOffset;
            var quarterRectWidth = 0.15f * (uiRect.width - Spacing);
            var threeToggleRect = new Rect(uiRect.x, uiRect.y, quarterRectWidth, uiRect.height);
            string strAxis = "";
            GUI.Label(threeToggleRect,"选择镜像轴:");
            threeToggleRect.x += quarterRectWidth + Spacing;
            if (isX= GUI.Toggle(threeToggleRect, isX, "X", SpringBoneGUIStyles.ToggleStyle))
            {
                isY= isZ = false;
                mirrorAxis = Axis.X;
                strAxis = "X";
            }
            threeToggleRect.x += quarterRectWidth + Spacing;
            if (isY=GUI.Toggle(threeToggleRect, isY, "Y", SpringBoneGUIStyles.ToggleStyle))
            {
                isX = isZ = false;
                mirrorAxis = Axis.Y;
                strAxis = "Y";
            }
            threeToggleRect.x += quarterRectWidth + Spacing;
            if (isZ=GUI.Toggle(threeToggleRect, isZ, "Z", SpringBoneGUIStyles.ToggleStyle))
            {
                isY = isX = false;
                mirrorAxis = Axis.Z;
                strAxis = "Z";
            }
            if (!isX && !isY && !isZ)
                mirrorAxis = Axis.Null;
            uiRect.y += buttonOffset;
     
            var halfRectWidth = 0.5f * (uiRect.width - Spacing);
            var halfButtonRect = new Rect(uiRect.x, uiRect.y, halfRectWidth, uiRect.height);
            if(mirrorAxis== Axis.Null)
            {
                if (GUI.Button(halfButtonRect, "请选择镜像轴", SpringBoneGUIStyles.ButtonStyle))
                {
                }
            }
            else
            {
                if (GUI.Button(halfButtonRect, "选择 " + strAxis + "<0 的骨骼", SpringBoneGUIStyles.ButtonStyle))
                {
                    AcquireSourceBonesOnSideOfAxis(true);
                }
                halfButtonRect.x += halfRectWidth + Spacing;
                if (GUI.Button(halfButtonRect, "选择 " + strAxis + ">0 的骨骼", SpringBoneGUIStyles.ButtonStyle))
                {
                    AcquireSourceBonesOnSideOfAxis(false);
                }
            }
            
            uiRect.y += buttonOffset;

            halfButtonRect.x = uiRect.x;
            halfButtonRect.y = uiRect.y;
            if (GUI.Button(halfButtonRect, "在Hierarchy中选择源列表中的SpringBone", SpringBoneGUIStyles.ButtonStyle))
            {
                var sourceBones = boneEntries.Select(entry => entry.sourceBone).Where(bone => bone != null);
                if (sourceBones.Any()) { Selection.objects = sourceBones.Select(bone => bone.gameObject).ToArray(); }
            }
            halfButtonRect.x += halfRectWidth + Spacing;
            if (GUI.Button(halfButtonRect, "在Hierarchy中选择目标列表中的SpringBone", SpringBoneGUIStyles.ButtonStyle))
            {
                var targetBones = boneEntries.Select(entry => entry.targetBone).Where(bone => bone != null);
                if (targetBones.Any()) { Selection.objects = targetBones.Select(bone => bone.gameObject).ToArray(); }
            }
            uiRect.y += buttonOffset;

            if (GUI.Button(uiRect, "选择列表中所有的SpringBone", SpringBoneGUIStyles.ButtonStyle))
            {
                var bonesToSelect = new List<SpringBone>();
                bonesToSelect.AddRange(boneEntries.Select(entry => entry.sourceBone).Where(bone => bone != null));
                bonesToSelect.AddRange(boneEntries.Select(entry => entry.targetBone).Where(bone => bone != null));
                if (bonesToSelect.Any()) { Selection.objects = bonesToSelect.Select(bone => bone.gameObject).ToArray(); }
            }
            uiRect.y += buttonOffset;

            return uiRect;
        }

        private Rect ShowBoneList(Rect uiRect)
        {
            var listBoxBottom = position.height - (Spacing * 2f + RowHeight);

            var headerRowRect = new Rect(uiRect.x, uiRect.y, uiRect.width * 0.5f, uiRect.height);
            GUI.Label(headerRowRect, "复制源SpringBone", SpringBoneGUIStyles.LabelStyle);
            headerRowRect.x += headerRowRect.width;
            GUI.Label(headerRowRect, "→ 复制目标SpringBone", SpringBoneGUIStyles.LabelStyle);
            uiRect.y += uiRect.height;

            const float ScrollbarWidth = 20f;
            const float EntryHeight = 24f;
            var listBoxRect = new Rect(uiRect.x, uiRect.y, uiRect.width, listBoxBottom - uiRect.y);
            var entryCount = boneEntries.Count;
            var listContentsRect = new Rect(0f, 0f, uiRect.width - ScrollbarWidth, entryCount * EntryHeight);

            scrollPosition = GUI.BeginScrollView(listBoxRect, scrollPosition, listContentsRect);
            var entryRect = new Rect(0f, 0f, listContentsRect.width, EntryHeight);
            for (int entryIndex = 0; entryIndex < entryCount; entryIndex++)
            {
                boneEntries[entryIndex].ShowEntryUI(entryRect);
                entryRect.y += entryRect.height;
            }
            GUI.EndScrollView();

            uiRect.y = listBoxBottom + Spacing;
            return uiRect;
        }

        private static Vector3 MirrorPosition(Vector3 originalPosition, Axis mirrorAxis)
        {
            var targetPosition = originalPosition;
            switch (mirrorAxis)
            {
                case Axis.X: targetPosition.x = -targetPosition.x; break;
                case Axis.Y: targetPosition.y = -targetPosition.y; break;
                case Axis.Z: targetPosition.z = -targetPosition.z; break;
            }
            return targetPosition;
        }

        private static T FindMirroredComponent<T>(GameObject rootObject, T sourceObject, Axis mirrorAxis, float threshold)
            where T : Component
        {
            if (rootObject == null 
                || sourceObject == null)
            {
                return null;
            }

            var possibleTargets = rootObject.GetComponentsInChildren<T>(true);
            var targetPosition = MirrorPosition(sourceObject.transform.position, mirrorAxis);
            var squaredThreshold = threshold * threshold;
            return possibleTargets
                .Where(item => (item.transform.position - targetPosition).sqrMagnitude <= squaredThreshold)
                .FirstOrDefault();
        }

        private static IEnumerable<T> FindMirroredComponents<T>
        (
            GameObject rootObject, 
            IEnumerable<T> sourceObjects, 
            Axis mirrorAxis, 
            float threshold = 0.001f
        ) where T : Component
        {
            return sourceObjects.Select(item => FindMirroredComponent(rootObject, item, mirrorAxis, threshold));
        }

        private static SpringBone FindMirroredSpringBone(SpringBone sourceBone, Axis mirrorAxis, float threshold = 0.001f)
        {
            var manager = sourceBone.GetComponentInParent<SpringManager>();
            return FindMirroredComponent(manager.gameObject, sourceBone, mirrorAxis, threshold);
        }

        private void AcquireBonesFromSelection()
        {
            if (boneEntries == null) { boneEntries = new List<BoneEntry>(); }

            var selectedBones = Selection.gameObjects
                .SelectMany(gameObject => gameObject.GetComponents<SpringBone>());
            var newBoneEntries = selectedBones
                .Select(bone => new BoneEntry(bone, FindMirroredSpringBone(bone, mirrorAxis)));
            foreach (var entry in newBoneEntries.Where(entry => entry.sourceBone == entry.targetBone))
            {
                entry.targetBone = null;
            }
            boneEntries = newBoneEntries.ToList();
        }

        private void AcquireSourceBonesOnSideOfAxis(bool getLessThanZero)
        {
            if (boneEntries == null) { boneEntries = new List<BoneEntry>(); }

            // Is there a better way of defining this?
            System.Func<SpringBone, bool> boneMatches = item => false;
            if (getLessThanZero)
            {
                switch (mirrorAxis)
                {
                    case Axis.X: boneMatches = item => item.transform.position.x < 0f; break;
                    case Axis.Y: boneMatches = item => item.transform.position.y < 0f; break;
                    case Axis.Z: boneMatches = item => item.transform.position.z < 0f; break;
                }
            }
            else
            {
                switch (mirrorAxis)
                {
                    case Axis.X: boneMatches = item => item.transform.position.x > 0f; break;
                    case Axis.Y: boneMatches = item => item.transform.position.y > 0f; break;
                    case Axis.Z: boneMatches = item => item.transform.position.z > 0f; break;
                }
            }

            var allSpringBones = GameObjectUtil.FindComponentsOfType<SpringBone>();
            var selectedBones = allSpringBones.Where(boneMatches);
            var newBoneEntries = selectedBones
                .Select(bone => new BoneEntry(bone, FindMirroredSpringBone(bone, mirrorAxis)))
                .Where(entry => entry.targetBone != null
                    && entry.targetBone != entry.sourceBone);
            boneEntries = newBoneEntries.ToList();
        }

        private void MirrorPivot(SpringBone sourceBone, SpringBone targetBone)
        {
            var sourcePivot = sourceBone.pivotNode;
            var targetPivot = targetBone.pivotNode;
            var targetPosition = MirrorPosition(sourcePivot.position, mirrorAxis);

            var pivotDirection = -sourcePivot.right;
            pivotDirection = MirrorPosition(pivotDirection, mirrorAxis);
            var upDirection = MirrorPosition(sourcePivot.forward, mirrorAxis);

            targetPivot.position = targetPosition;
            targetPivot.LookAt(targetPosition + pivotDirection, upDirection);
            targetPivot.Rotate(-90f, 90f, 0f, Space.Self);
        }

        private void PerformMirror()
        {
            var mirrorItems = boneEntries.Where(
                item => item.sourceBone != null 
                    && item.targetBone != null
                    && item.sourceBone != item.targetBone);
            var undoItems = mirrorItems.Select(item => (Component)item.targetBone).ToList();

            var allSkinBones = GameObjectUtil.FindComponentsOfType<SkinnedMeshRenderer>()
                .SelectMany(renderer => renderer.bones)
                .Distinct()
                .ToArray();

            var editablePivots = mirrorItems
                .Select(item => item.targetBone.pivotNode)
                .Where(pivotNode => pivotNode != null
                    && SpringBoneSetup.IsPivotProbablySafeToDestroy(pivotNode, allSkinBones))
                .ToArray();

            undoItems.AddRange(editablePivots);
            Undo.RecordObjects(undoItems.ToArray(), "Mirror SpringBones");

            foreach (var mirrorItem in mirrorItems)
            {
                var sourceBone = mirrorItem.sourceBone;
                var targetBone = mirrorItem.targetBone;
                var rootManager = targetBone.GetComponentInParent<SpringManager>();
                if (rootManager == null) { continue; }

                targetBone.stiffnessForce = sourceBone.stiffnessForce;
                targetBone.dragForce = sourceBone.dragForce;
                targetBone.springForce = sourceBone.springForce;
                targetBone.windInfluence = sourceBone.windInfluence;

                if (editablePivots.Contains(targetBone.pivotNode))
                {
                    MirrorPivot(sourceBone, targetBone);
                }

                targetBone.angularStiffness = sourceBone.angularStiffness;
                sourceBone.yAngleLimits.CopyTo(targetBone.yAngleLimits);
                sourceBone.zAngleLimits.CopyTo(targetBone.zAngleLimits);
                targetBone.yAngleLimits.min = -sourceBone.yAngleLimits.max;
                targetBone.yAngleLimits.max = -sourceBone.yAngleLimits.min;

                targetBone.lengthLimitTargets = FindMirroredComponents(
                    rootManager.gameObject, sourceBone.lengthLimitTargets, mirrorAxis)
                    .Where(item => item != null)
                    .ToArray();

                targetBone.radius = sourceBone.radius;
                targetBone.sphereColliders = FindMirroredComponents(
                    rootManager.gameObject, sourceBone.sphereColliders, mirrorAxis)
                    .Where(item => item != null)
                    .ToArray();
                targetBone.capsuleColliders = FindMirroredComponents(
                    rootManager.gameObject, sourceBone.capsuleColliders, mirrorAxis)
                    .Where(item => item != null)
                    .ToArray();
                targetBone.panelColliders = FindMirroredComponents(
                    rootManager.gameObject, sourceBone.panelColliders, mirrorAxis)
                    .Where(item => item != null)
                    .ToArray();
            }
        }

        private void OnShow()
        {
            AcquireBonesFromSelection();
        }
    }
}