using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Waribashi.ScreenTransitions
{
    [TrackColor(0.95f, 0.5f, 0.15f)]
    [TrackClipType(typeof(TransitionClip))]
    [TrackBindingType(typeof(TransitionImage))]
    public class TransitionTrack : TrackAsset
    {
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            return ScriptPlayable<TransitionMixerBehaviour>.Create(graph, inputCount);
        }
    }
}
