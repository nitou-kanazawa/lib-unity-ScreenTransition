using UnityEngine;
using UnityEngine.UI;

namespace Waribashi.ScreenTransitions.Immersive
{
    /// <summary>
    /// 「切り抜き風」の人物シルエットを手続き生成するファクトリ。
    /// ルートの pivot は (0.5, 0)（= 足元基準）。
    /// </summary>
    public static class SilhouetteFactory
    {
        public struct Person
        {
            public RectTransform Root;
            public RectTransform Body;   // 上下バウンド用（頭・胴・腕を含む）
            public RectTransform LegL;
            public RectTransform LegR;
        }

        static Image CreatePart(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        static Image CreateEllipse(Transform parent, string name, Color color)
        {
            var image = CreatePart(parent, name, color);
            image.sprite = ProceduralSprites.Circle;
            return image;
        }

        static Person CreateBase(Transform parent, float h, Color color, out RectTransform body)
        {
            var rootGo = new GameObject("Person", typeof(RectTransform));
            var root = (RectTransform)rootGo.transform;
            root.SetParent(parent, false);
            root.pivot = new Vector2(0.5f, 0f);
            root.sizeDelta = new Vector2(h * 0.5f, h);

            // 脚（腰 pivot で振る）
            var legL = CreatePart(root, "LegL", color).rectTransform;
            legL.pivot = new Vector2(0.5f, 1f);
            legL.sizeDelta = new Vector2(h * 0.085f, h * 0.47f);
            legL.anchoredPosition = new Vector2(-h * 0.045f, h * 0.46f);

            var legR = CreatePart(root, "LegR", color).rectTransform;
            legR.pivot = new Vector2(0.5f, 1f);
            legR.sizeDelta = new Vector2(h * 0.085f, h * 0.47f);
            legR.anchoredPosition = new Vector2(h * 0.045f, h * 0.46f);

            var bodyGo = new GameObject("Body", typeof(RectTransform));
            body = (RectTransform)bodyGo.transform;
            body.SetParent(root, false);
            body.anchoredPosition = Vector2.zero;

            // 胴（縦長の楕円）と頭
            var torso = CreateEllipse(body, "Torso", color).rectTransform;
            torso.sizeDelta = new Vector2(h * 0.30f, h * 0.5f);
            torso.anchoredPosition = new Vector2(0f, h * 0.62f);

            var head = CreateEllipse(body, "Head", color).rectTransform;
            head.sizeDelta = new Vector2(h * 0.17f, h * 0.17f);
            head.anchoredPosition = new Vector2(0f, h * 0.95f);

            return new Person { Root = root, Body = body, LegL = legL, LegR = legR };
        }

        /// <summary>歩行者。脚を LegL/LegR の回転で振る。</summary>
        public static Person CreateWalker(Transform parent, float height, Color color)
        {
            return CreateBase(parent, height, color, out _);
        }

        /// <summary>つり革につかまる立ち客。腕が上がっている。</summary>
        public static Person CreateStrapHanger(Transform parent, float height, Color color)
        {
            var person = CreateBase(parent, height, color, out var body);

            var arm = CreatePart(body, "Arm", color).rectTransform;
            arm.pivot = new Vector2(0.5f, 0f);
            arm.sizeDelta = new Vector2(height * 0.07f, height * 0.42f);
            arm.anchoredPosition = new Vector2(height * 0.10f, height * 0.72f);
            arm.localRotation = Quaternion.Euler(0f, 0f, -14f);
            return person;
        }

        /// <summary>座っている乗客（横向きの L 字姿勢）。ルートの原点は座面の高さ。</summary>
        public static RectTransform CreateSitter(Transform parent, float height, Color color)
        {
            var rootGo = new GameObject("Sitter", typeof(RectTransform));
            var root = (RectTransform)rootGo.transform;
            root.SetParent(parent, false);
            root.pivot = new Vector2(0.5f, 0f);
            root.sizeDelta = new Vector2(height * 0.55f, height);

            var torso = CreateEllipse(root, "Torso", color).rectTransform;
            torso.sizeDelta = new Vector2(height * 0.34f, height * 0.52f);
            torso.anchoredPosition = new Vector2(-height * 0.06f, height * 0.34f);

            var head = CreateEllipse(root, "Head", color).rectTransform;
            head.sizeDelta = new Vector2(height * 0.19f, height * 0.19f);
            head.anchoredPosition = new Vector2(-height * 0.06f, height * 0.68f);

            // 膝（前へ突き出す腿）と下腿
            var lap = CreatePart(root, "Lap", color).rectTransform;
            lap.sizeDelta = new Vector2(height * 0.34f, height * 0.13f);
            lap.anchoredPosition = new Vector2(height * 0.12f, height * 0.12f);

            var shin = CreatePart(root, "Shin", color).rectTransform;
            shin.pivot = new Vector2(0.5f, 1f);
            shin.sizeDelta = new Vector2(height * 0.09f, height * 0.22f);
            shin.anchoredPosition = new Vector2(height * 0.25f, height * 0.14f);

            return root;
        }
    }
}
