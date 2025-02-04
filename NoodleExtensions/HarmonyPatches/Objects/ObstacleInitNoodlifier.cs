﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Heck;
using Heck.Animation;
using NoodleExtensions.HarmonyPatches.ObjectProcessing;
using SiraUtil.Affinity;
using UnityEngine;
using Zenject;

namespace NoodleExtensions.HarmonyPatches.Objects
{
    internal class ObstacleInitNoodlifier : IAffinity, IDisposable
    {
        private static readonly FieldInfo _worldRotationField = AccessTools.Field(typeof(ObstacleController), "_worldRotation");
        private static readonly FieldInfo _inverseWorldRotationField = AccessTools.Field(typeof(ObstacleController), "_inverseWorldRotation");
        private static readonly MethodInfo _widthGetter = AccessTools.PropertyGetter(typeof(ObstacleData), nameof(ObstacleData.width));
        private static readonly FieldInfo _lengthField = AccessTools.Field(typeof(ObstacleController), "_length");

        private static readonly MethodInfo _invertQuaternion = AccessTools.Method(typeof(Quaternion), nameof(Quaternion.Inverse));

        private readonly CodeInstruction _getWorldRotation;
        private readonly CodeInstruction _getCustomWidth;
        private readonly CodeInstruction _getCustomLength;

        private readonly CustomData _customData;
        private readonly BeatmapObjectSpawnMovementData _movementData;
        private readonly ManagedActiveObstacleTracker _obstacleTracker;

        private ObstacleInitNoodlifier(
            [Inject(Id = NoodleController.ID)] CustomData customData,
            IBeatmapObjectSpawnController spawnController,
            ManagedActiveObstacleTracker obstacleTracker)
        {
            _customData = customData;
            _obstacleTracker = obstacleTracker;
            _movementData = spawnController.beatmapObjectSpawnMovementData;

            _getWorldRotation = InstanceTranspilers.EmitInstanceDelegate<Func<ObstacleData, float, Quaternion>>(GetWorldRotation);
            _getCustomWidth = InstanceTranspilers.EmitInstanceDelegate<Func<float, ObstacleData, float>>(GetCustomWidth);
            _getCustomLength = InstanceTranspilers.EmitInstanceDelegate<Func<float, ObstacleData, float>>(GetCustomLength);
        }

        public void Dispose()
        {
            InstanceTranspilers.DisposeDelegate(_getWorldRotation);
            InstanceTranspilers.DisposeDelegate(_getCustomWidth);
            InstanceTranspilers.DisposeDelegate(_getCustomLength);
        }

        [AffinityTranspiler]
        [AffinityPatch(typeof(ObstacleController), nameof(ObstacleController.Init))]
        private IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return new CodeMatcher(instructions)

                // world rotation
                .MatchForward(false, new CodeMatch(OpCodes.Stfld, _worldRotationField))
                .Insert(
                    new CodeInstruction(OpCodes.Ldarg_1),
                    new CodeInstruction(OpCodes.Ldarg_2),
                    _getWorldRotation)
                .RemoveInstructionsWithOffsets(-4, -1)

                // inverse world rotation
                .MatchForward(false, new CodeMatch(OpCodes.Stfld, _inverseWorldRotationField))
                .Insert(
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldfld, _worldRotationField),
                    new CodeInstruction(OpCodes.Call, _invertQuaternion))
                .RemoveInstructionsWithOffsets(-5, -1)

                // width
                .MatchForward(false, new CodeMatch(OpCodes.Callvirt, _widthGetter))
                .Advance(2)
                .InsertAndAdvance(
                    new CodeInstruction(OpCodes.Ldarg_1),
                    _getCustomWidth)

                // length
                .MatchForward(false, new CodeMatch(OpCodes.Stfld, _lengthField))
                .Insert(
                    new CodeInstruction(OpCodes.Ldarg_1),
                    _getCustomLength)

                .InstructionEnumeration();
        }

        [AffinityPostfix]
        [AffinityPatch(typeof(ObstacleController), nameof(ObstacleController.Init))]
        private void Postfix(ObstacleController __instance, Quaternion ____worldRotation, ObstacleData obstacleData, Vector3 ____startPos, Vector3 ____midPos, Vector3 ____endPos, ref Bounds ____bounds)
        {
            if (!_customData.Resolve(obstacleData, out NoodleObstacleData? noodleData))
            {
                return;
            }

            Quaternion? localRotationQuaternion = noodleData.LocalRotationQuaternion;

            Transform transform = __instance.transform;

            Quaternion localRotation = Quaternion.identity;
            if (localRotationQuaternion.HasValue)
            {
                localRotation = localRotationQuaternion.Value;
                transform.localRotation = ____worldRotation * localRotation;
            }

            transform.localScale = Vector3.one; // This is a fix for animation due to obstacles being recycled

            IEnumerable<Track>? tracks = noodleData.Track;
            if (tracks != null)
            {
                foreach (Track track in tracks)
                {
                    // add to gameobjects
                    track.AddGameObject(__instance.gameObject);
                }
            }

            if (noodleData is { Uninteractable: true })
            {
                ____bounds.size = Vector3.zero;
            }
            else
            {
                _obstacleTracker.AddActive(__instance);
            }

            noodleData.StartPos = ____startPos;
            noodleData.MidPos = ____midPos;
            noodleData.EndPos = ____endPos;
            noodleData.LocalRotation = localRotation;
            noodleData.BoundsSize = ____bounds.size;
        }

        private Quaternion GetWorldRotation(ObstacleData obstacleData, float @default)
        {
            Quaternion worldRotation = Quaternion.Euler(0, @default, 0);

            if (!_customData.Resolve(obstacleData, out NoodleObstacleData? noodleData))
            {
                return worldRotation;
            }

            Quaternion? worldRotationQuaternion = noodleData.WorldRotationQuaternion;
            if (worldRotationQuaternion.HasValue)
            {
                worldRotation = worldRotationQuaternion.Value;
            }

            noodleData.WorldRotation = worldRotation;

            return worldRotation;
        }

        private float GetCustomWidth(float @default, ObstacleData obstacleData)
        {
            _customData.Resolve(obstacleData, out NoodleObstacleData? noodleData);
            return noodleData?.Width ?? @default;
        }

        private float GetCustomLength(float @default, ObstacleData obstacleData)
        {
            _customData.Resolve(obstacleData, out NoodleObstacleData? noodleData);
            return noodleData?.Length * _movementData.noteLinesDistance ?? @default;
        }
    }
}
