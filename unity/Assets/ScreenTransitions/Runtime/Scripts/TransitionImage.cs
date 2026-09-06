using UnityEngine;
using UnityEngine.UI;

namespace Waribashi.ScreenTransitions
{
    /// <summary>
    /// ルール画像 + カットオフ値でトランジションを描画する uGUI コンポーネント。
    /// Cutoff 0 = 完全に開いた状態（非表示）、1 = 完全に閉じた状態（全面塗り）。
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Image))]
    public class TransitionImage : MonoBehaviour
    {
        static readonly int RuleTexId = Shader.PropertyToID("_RuleTex");
        static readonly int CutoffId = Shader.PropertyToID("_Cutoff");
        static readonly int SoftnessId = Shader.PropertyToID("_Softness");

        const string ShaderName = "ScreenTransitions/UIRuleTransition";

        [SerializeField] Texture2D ruleTexture;
        [SerializeField, Range(0f, 1f)] float cutoff;
        [SerializeField, Range(0.001f, 1f)] float softness = 0.08f;

        Image _image;
        Material _material;

        public Texture2D RuleTexture
        {
            get => ruleTexture;
            set { ruleTexture = value; Apply(); }
        }

        public float Cutoff
        {
            get => cutoff;
            set { cutoff = Mathf.Clamp01(value); Apply(); }
        }

        public float Softness
        {
            get => softness;
            set { softness = Mathf.Max(0.001f, value); Apply(); }
        }

        void OnEnable()
        {
            _image = GetComponent<Image>();
            EnsureMaterial();
            Apply();
        }

        void OnDisable()
        {
            if (_image != null)
                _image.material = null;

            if (_material != null)
            {
                if (Application.isPlaying)
                    Destroy(_material);
                else
                    DestroyImmediate(_material);
                _material = null;
            }
        }

        void OnValidate()
        {
            Apply();
        }

        void EnsureMaterial()
        {
            if (_material != null)
                return;

            var shader = Shader.Find(ShaderName);
            if (shader == null)
            {
                Debug.LogError($"[ScreenTransitions] Shader '{ShaderName}' not found.", this);
                return;
            }

            _material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            _image.material = _material;
        }

        void Apply()
        {
            if (_material == null || _image == null)
                return;

            _material.SetTexture(RuleTexId, ruleTexture);
            _material.SetFloat(CutoffId, cutoff);
            _material.SetFloat(SoftnessId, softness);

            // 完全に開いた状態では描画もレイキャストも止める
            bool visible = cutoff > 0.0005f;
            if (_image.enabled != visible)
                _image.enabled = visible;
            _image.raycastTarget = visible;
        }
    }
}
