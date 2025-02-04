﻿using System.Collections.Generic;
using Chroma.Lighting;
using Heck;
using Heck.Animation;
using UnityEngine;

namespace Chroma
{
    internal class ChromaCustomEventData : ICustomEventCustomData
    {
        public ChromaCustomEventData(Track track)
        {
            Track = track;
        }

        internal Track Track { get; }
    }

    internal class ChromaNoteData : ChromaObjectData
    {
        internal bool? SpawnEffect { get; set; }
    }

    internal class ChromaObjectData : IObjectCustomData
    {
        internal Color? Color { get; set; }

        internal List<Track>? Track { get; set; }

        internal PointDefinition? LocalPathColor { get; set; }
    }

    internal class ChromaEventData : IEventCustomData
    {
        internal ChromaEventData(
            object? propID,
            Color? colorData,
            Functions? easing,
            LerpType? lerpType,
            bool lockPosition,
            string? nameFilter,
            int? direction,
            bool? counterSpin,
            bool? reset,
            float? step,
            float? prop,
            float? speed,
            float? rotation,
            float stepMult,
            float propMult,
            float speedMult)
        {
            PropID = propID;
            ColorData = colorData;
            Easing = easing;
            LerpType = lerpType;
            LockPosition = lockPosition;
            NameFilter = nameFilter;
            Direction = direction;
            CounterSpin = counterSpin;
            Reset = reset;
            Step = step;
            Prop = prop;
            Speed = speed;
            Rotation = rotation;
            StepMult = stepMult;
            PropMult = propMult;
            SpeedMult = speedMult;
        }

        internal IEnumerable<int>? LightID { get; set; }

        internal object? PropID { get; }

        internal Color? ColorData { get; }

        internal GradientObjectData? GradientObject { get; set; }

        internal Functions? Easing { get; }

        internal LerpType? LerpType { get; }

        internal bool LockPosition { get; }

        internal string? NameFilter { get; }

        internal int? Direction { get; }

        internal bool? CounterSpin { get; }

        internal bool? Reset { get; }

        internal float? Step { get; }

        internal float? Prop { get; }

        internal float? Speed { get; }

        internal float? Rotation { get; }

        internal float StepMult { get; }

        internal float PropMult { get; }

        internal float SpeedMult { get; }

        internal Dictionary<int, BasicBeatmapEventData>? NextSameTypeEvent { get; set; }

        internal class GradientObjectData
        {
            internal GradientObjectData(float duration, Color startColor, Color endColor, Functions easing)
            {
                Duration = duration;
                StartColor = startColor;
                EndColor = endColor;
                Easing = easing;
            }

            internal float Duration { get; }

            internal Color StartColor { get; }

            internal Color EndColor { get; }

            internal Functions Easing { get; }
        }
    }
}
