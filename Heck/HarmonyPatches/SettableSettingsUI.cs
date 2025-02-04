﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Heck.SettingsSetter;
using HMUI;
using IPA.Utilities;
using JetBrains.Annotations;
using SiraUtil.Affinity;

namespace Heck.HarmonyPatches
{
    [HeckPatch]
    [HarmonyPatch(typeof(SinglePlayerLevelSelectionFlowCoordinator))]
    internal class SettableSettingsUI : IAffinity
    {
        private static readonly Action<FlowCoordinator, ViewController?, ViewController.AnimationType> _setLeftScreenViewController = MethodAccessor<FlowCoordinator, Action<FlowCoordinator, ViewController?, ViewController.AnimationType>>.GetDelegate("SetLeftScreenViewController");
        private static readonly Action<FlowCoordinator, ViewController?, ViewController.AnimationType> _setRightScreenViewController = MethodAccessor<FlowCoordinator, Action<FlowCoordinator, ViewController?, ViewController.AnimationType>>.GetDelegate("SetRightScreenViewController");
        private static readonly Action<FlowCoordinator, ViewController?, ViewController.AnimationType> _setBottomScreenViewController = MethodAccessor<FlowCoordinator, Action<FlowCoordinator, ViewController?, ViewController.AnimationType>>.GetDelegate("SetBottomScreenViewController");
        private static readonly Action<FlowCoordinator, string?, ViewController.AnimationType> _setTitle = MethodAccessor<FlowCoordinator, Action<FlowCoordinator, string?, ViewController.AnimationType>>.GetDelegate("SetTitle");
        private static readonly PropertyAccessor<FlowCoordinator, bool>.Setter _showBackButtonSetter = PropertyAccessor<FlowCoordinator, bool>.GetSetter("showBackButton");

        private static readonly Action<FlowCoordinator, ViewController, ViewController.AnimationDirection, Action?, bool> _dismissViewController = MethodAccessor<FlowCoordinator, Action<FlowCoordinator, ViewController, ViewController.AnimationDirection, Action?, bool>>.GetDelegate("DismissViewController");

        private static readonly ConstructorInfo _standardLevelParametersCtor = AccessTools.FirstConstructor(typeof(StartStandardLevelParameters), _ => true);

        private static readonly MethodInfo _startStandardLevel = AccessTools.Method(
            typeof(MenuTransitionsHelper),
            nameof(MenuTransitionsHelper.StartStandardLevel),
            new[]
            {
                typeof(string), typeof(IDifficultyBeatmap), typeof(IPreviewBeatmapLevel),
                typeof(OverrideEnvironmentSettings), typeof(ColorScheme), typeof(GameplayModifiers),
                typeof(PlayerSpecificSettings), typeof(PracticeSettings), typeof(string), typeof(bool), typeof(bool),
                typeof(Action), typeof(Action<StandardLevelScenesTransitionSetupDataSO, LevelCompletionResults>)
            });

        private readonly SettingsSetterViewController _setterViewController;

        private SettableSettingsUI(SettingsSetterViewController setterViewController)
        {
            _setterViewController = setterViewController;
        }

        // Get all the parameters used to make a StartStandardLevelParameters
        [HarmonyReversePatch]
        [HarmonyPatch(nameof(SinglePlayerLevelSelectionFlowCoordinator.StartLevel))]
        private static StartStandardLevelParameters GetParameters(
            SinglePlayerLevelSelectionFlowCoordinator instance, Action beforeSceneSwitchCallback, bool practice)
        {
            _ = Transpiler(null!);
            throw new NotImplementedException("Reverse patch has not been executed.");

            [UsedImplicitly]
            IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                return new CodeMatcher(instructions)
                    .Start()
                    .RemoveInstructions(2)
                    .MatchForward(false, new CodeMatch(OpCodes.Callvirt, _startStandardLevel))
                    .SetInstruction(new CodeInstruction(OpCodes.Newobj, _standardLevelParametersCtor))
                    .InstructionEnumeration();
            }
        }

        [AffinityPrefix]
        [AffinityPatch(typeof(SinglePlayerLevelSelectionFlowCoordinator), nameof(SinglePlayerLevelSelectionFlowCoordinator.StartLevel))]
        private bool StartLevelPrefix(SinglePlayerLevelSelectionFlowCoordinator __instance, Action beforeSceneSwitchCallback, bool practice)
        {
            StartStandardLevelParameters parameters = GetParameters(__instance, beforeSceneSwitchCallback, practice);
            _setterViewController.Init(__instance, parameters);
            return !_setterViewController.DoPresent;
        }

        [AffinityPrefix]
        [AffinityPatch(typeof(SinglePlayerLevelSelectionFlowCoordinator), "HandleBasicLevelCompletionResults")]
        private bool HandleBasicLevelCompletionResultsPrefix(LevelCompletionResults levelCompletionResults, ref bool __result)
        {
            if (levelCompletionResults.levelEndAction != LevelCompletionResults.LevelEndAction.Restart ||
                !_setterViewController.DoPresent)
            {
                return true;
            }

            __result = true;
            _setterViewController.ForceStartLevel();
            return false;
        }

        [AffinityPrefix]
        [AffinityPatch(typeof(SinglePlayerLevelSelectionFlowCoordinator), "LevelSelectionFlowCoordinatorTopViewControllerWillChange")]
        private bool LevelSelectionFlowCoordinatorTopViewControllerWillChangePrefix(
            SinglePlayerLevelSelectionFlowCoordinator __instance,
            ViewController newViewController,
            ViewController.AnimationType animationType)
        {
            if (newViewController != _setterViewController)
            {
                return true;
            }

            _setLeftScreenViewController(__instance, null, animationType);
            _setRightScreenViewController(__instance, null, animationType);
            _setBottomScreenViewController(__instance, null, animationType);
            _setTitle(__instance, "information", animationType);
            FlowCoordinator flowCoordinator = __instance;
            _showBackButtonSetter(ref flowCoordinator, true);
            return false;
        }

        [AffinityPrefix]
        [AffinityPatch(typeof(SinglePlayerLevelSelectionFlowCoordinator), "BackButtonWasPressed")]
        private bool BackButtonWasPressedPrefix(SinglePlayerLevelSelectionFlowCoordinator __instance)
        {
            if (__instance.topViewController != _setterViewController)
            {
                return true;
            }

            _dismissViewController(__instance, _setterViewController, ViewController.AnimationDirection.Horizontal, null, false);
            return false;
        }

        [AffinityPrefix]
        [AffinityPatch(typeof(MenuTransitionsHelper), "HandleMainGameSceneDidFinish")]
        private void HandleMainGameSceneDidFinishPrefix(LevelCompletionResults levelCompletionResults)
        {
            if (levelCompletionResults.levelEndAction != LevelCompletionResults.LevelEndAction.Restart)
            {
                _setterViewController.RestoreCached();
            }
        }
    }
}
