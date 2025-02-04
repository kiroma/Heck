﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Heck;
using JetBrains.Annotations;
using UnityEngine;
using Zenject;

namespace NoodleExtensions.Managers
{
    [UsedImplicitly]
    internal class FakePatchesManager : IDisposable
    {
        private static readonly MethodInfo _currentGetter = AccessTools.PropertyGetter(typeof(List<ObstacleController>.Enumerator), nameof(List<ObstacleController>.Enumerator.Current));

        private readonly CodeInstruction _obstacleFakeCheck;
        private readonly CustomData _customData;

        private FakePatchesManager([Inject(Id = NoodleController.ID)] CustomData customData)
        {
            _customData = customData;
            _obstacleFakeCheck = InstanceTranspilers.EmitInstanceDelegate<Func<ObstacleController, bool>>(BoundsNullCheck);
        }

        public void Dispose()
        {
            InstanceTranspilers.DisposeDelegate(_obstacleFakeCheck);
        }

        internal bool BoundsNullCheck(ObstacleController obstacleController)
        {
            if (obstacleController.bounds.size == Vector3.zero)
            {
                return true;
            }

            _customData.Resolve(obstacleController.obstacleData, out NoodleObstacleData? noodleData);
            return noodleData?.Fake is true;
        }

        internal IEnumerable<CodeInstruction> BoundsNullCheckTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            CodeMatcher codeMatcher = new CodeMatcher(instructions)
                .MatchForward(false, new CodeMatch(OpCodes.Brtrue));

            object label = codeMatcher.Operand;

            return codeMatcher
                .MatchForward(false, new CodeMatch(OpCodes.Call, _currentGetter))
                .Advance(2)
                .Insert(
                    new CodeInstruction(OpCodes.Ldloc_1),
                    _obstacleFakeCheck,
                    new CodeInstruction(OpCodes.Brtrue_S, label))
                .InstructionEnumeration();
        }

        internal bool GetFakeNote(NoteController noteController)
        {
            _customData.Resolve(noteController.noteData, out NoodleNoteData? noodleData);
            return noodleData?.Fake is not true;
        }

        internal bool GetCuttable(NoteData noteData)
        {
            _customData.Resolve(noteData, out NoodleNoteData? noodleData);
            return noodleData?.Uninteractable is not true;
        }
    }
}
