using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Waribashi.ScreenTransitions
{
    /// <summary>
    /// ルール画像系トランジションの再生役。閉じ / 開き / ループ（任意）の Timeline を切り替えて再生する。
    /// - TransitionTrack のバインディングは再生時に自動解決
    /// - Closed 中に loopTimeline が設定されていれば DirectorWrapMode.Loop で回し続ける
    /// - DirectorUpdateMode.UnscaledGameTime で timeScale の影響を受けない
    /// </summary>
    [RequireComponent(typeof(PlayableDirector))]
    public class TransitionPlayer : MonoBehaviour, IScreenTransition
    {
        [SerializeField] TransitionImage target;
        [SerializeField] TimelineAsset closeTimeline;
        [SerializeField] TimelineAsset openTimeline;
        [SerializeField] TimelineAsset loopTimeline;

        PlayableDirector _director;

        public TransitionImage Target { get => target; set => target = value; }
        public TimelineAsset CloseTimeline { get => closeTimeline; set => closeTimeline = value; }
        public TimelineAsset OpenTimeline { get => openTimeline; set => openTimeline = value; }
        public TimelineAsset LoopTimeline { get => loopTimeline; set => loopTimeline = value; }

        public TransitionState State { get; private set; } = TransitionState.Open;

        void Awake()
        {
            _director = GetComponent<PlayableDirector>();
            _director.playOnAwake = false;
            _director.timeUpdateMode = DirectorUpdateMode.UnscaledGameTime;
        }

        public void SetPattern(Texture2D ruleTexture)
        {
            if (target != null)
                target.RuleTexture = ruleTexture;
        }

        public async UniTask CloseAsync(CancellationToken ct = default)
        {
            if (State != TransitionState.Open)
                return;

            State = TransitionState.Closing;
            try
            {
                await PlayOnce(closeTimeline, 1f, ct);
                State = TransitionState.Closed;
                StartLoop();
            }
            catch (OperationCanceledException)
            {
                if (this != null)
                    State = TransitionState.Open;
                throw;
            }
        }

        public async UniTask OpenAsync(CancellationToken ct = default)
        {
            if (State != TransitionState.Closed)
                return;

            _director.Stop(); // ループを止める
            State = TransitionState.Opening;
            try
            {
                await PlayOnce(openTimeline, 0f, ct);
                State = TransitionState.Open;
            }
            catch (OperationCanceledException)
            {
                if (this != null)
                {
                    State = TransitionState.Closed;
                    StartLoop();
                }
                throw;
            }
        }

        void StartLoop()
        {
            if (loopTimeline == null || target == null)
                return;

            Bind(loopTimeline);
            _director.playableAsset = loopTimeline;
            _director.extrapolationMode = DirectorWrapMode.Loop;
            _director.time = 0.0;
            _director.Play();
        }

        async UniTask PlayOnce(TimelineAsset timeline, float endValue, CancellationToken ct)
        {
            if (timeline == null || target == null)
            {
                Debug.LogWarning("[ScreenTransitions] Timeline or target is not set.", this);
                if (target != null)
                    target.Cutoff = endValue;
                return;
            }

            using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, this.GetCancellationTokenOnDestroy());

            Bind(timeline);
            _director.extrapolationMode = DirectorWrapMode.None;
            _director.playableAsset = timeline;
            _director.time = 0.0;
            _director.Play();

            // DirectorWrapMode.None なので末尾到達で Paused になる
            while (_director.state == PlayState.Playing)
                await UniTask.Yield(PlayerLoopTiming.Update, linked.Token);

            // 終端フレームの取りこぼし対策として最終値を確定させる
            target.Cutoff = endValue;
        }

        void Bind(TimelineAsset timeline)
        {
            foreach (var output in timeline.GetOutputTracks())
            {
                if (output is TransitionTrack)
                    _director.SetGenericBinding(output, target);
            }
        }
    }
}
