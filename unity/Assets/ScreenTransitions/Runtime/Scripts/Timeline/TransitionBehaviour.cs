using System;
using UnityEngine;
using UnityEngine.Playables;

namespace Waribashi.ScreenTransitions
{
    /// <summary>
    /// トランジションクリップの再生データ。from -> to をイージングカーブで補間する。
    /// </summary>
    [Serializable]
    public class TransitionBehaviour : PlayableBehaviour
    {
        public float from;
        public float to = 1f;
        public AnimationCurve easing = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        public float Evaluate(double time, double duration)
        {
            float t = duration > 0.0 ? Mathf.Clamp01((float)(time / duration)) : 1f;
            return Mathf.LerpUnclamped(from, to, easing.Evaluate(t));
        }
    }

    /// <summary>
    /// TransitionTrack のミキサー。クリップのウェイト合成結果を TransitionImage.Cutoff に書き込む。
    /// </summary>
    public class TransitionMixerBehaviour : PlayableBehaviour
    {
        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            var target = playerData as TransitionImage;
            if (target == null)
                return;

            int count = playable.GetInputCount();
            float value = 0f;
            float totalWeight = 0f;

            for (int i = 0; i < count; i++)
            {
                float weight = playable.GetInputWeight(i);
                if (weight <= 0f)
                    continue;

                var input = (ScriptPlayable<TransitionBehaviour>)playable.GetInput(i);
                value += weight * input.GetBehaviour().Evaluate(input.GetTime(), input.GetDuration());
                totalWeight += weight;
            }

            if (totalWeight > 0f)
                target.Cutoff = value / totalWeight;
        }
    }
}
