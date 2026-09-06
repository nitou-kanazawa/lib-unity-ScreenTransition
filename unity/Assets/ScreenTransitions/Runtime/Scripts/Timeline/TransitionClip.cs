using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Waribashi.ScreenTransitions
{
    public class TransitionClip : PlayableAsset, ITimelineClipAsset
    {
        public TransitionBehaviour template = new TransitionBehaviour();

        public ClipCaps clipCaps => ClipCaps.Blending;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            return ScriptPlayable<TransitionBehaviour>.Create(graph, template);
        }
    }
}
