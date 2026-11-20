using UnityEngine;
using UnityEngine.UI;

namespace WordRPG.UI
{
    // Korean Word RPG (#8): 한국어 단어의 로마자 (Figma 'Korean Word RPG' 페이지 — 부품 'Korean Word').
    // 크게 보이는 단어는 바로 아래 한 줄(Under + Show), 보기 버튼은 단어 아래 작게(OnButton), 목록은 단어 오른쪽에 작게(After).
    // 로마자가 비면(로마자 칸이 없는 단어 · 뜻이 문제인 칸) 숨김
    public static class RomanLabels
    {
        public static readonly Color OnButtonColor = new Color(1f, 1f, 1f, 0.75f); // 파란 버튼 위라 회색보다 흰색 75%가 읽기 쉬움
        public const float ButtonWordBottom = 0.34f;                                 // 로마자가 있을 때 버튼 글자 칸의 아래 끝

        // 단어 아래 한 줄 칸 (본문 글꼴, 흐린 글자색). 처음엔 숨김 — Show로 글자를 넣음
        public static Text Under(string name, Transform parent, int size, float minX, float minY, float maxX, float maxY, Color? color = null)
        {
            var label = UiKit.OneLine(UiKit.Label(name, parent, "", size, color ?? Palette.TextDim, minX, minY, maxX, maxY));
            label.gameObject.SetActive(false);
            return label;
        }

        public static void Show(Text label, string roman)
        {
            if (label == null) return;
            label.text = roman ?? "";
            label.gameObject.SetActive(!string.IsNullOrEmpty(roman));
        }

        // 보기 버튼: 로마자가 있으면 단어를 위로 올리고 그 아래에 작게, 없으면 원래대로 (단어가 버튼 가운데)
        public static void OnButton(Button button, string roman, int size = 26)
        {
            var label = UiKit.LabelOf(button);
            var found = button.transform.Find("Roman");
            var romanLabel = found != null ? found.GetComponent<Text>() : null;
            bool show = !string.IsNullOrEmpty(roman);
            if (romanLabel == null)
            {
                if (!show) return;
                romanLabel = UiKit.OneLine(UiKit.Label("Roman", button.transform, "", size, OnButtonColor, 0, 0.04f, 1, 0.42f));
            }
            romanLabel.text = roman ?? "";
            romanLabel.gameObject.SetActive(show);
            label.rectTransform.anchorMin = new Vector2(label.rectTransform.anchorMin.x, show ? ButtonWordBottom : 0f);
        }

        // 버튼 안 로마자 (테스트용) — 없거나 숨었으면 ""
        public static string OnButtonText(Button button)
        {
            var found = button.transform.Find("Roman");
            return found != null && found.gameObject.activeSelf ? found.GetComponent<Text>().text : "";
        }

        // 목록: 왼쪽 맞춤 단어 글자 바로 오른쪽에 작게. 로마자가 없으면 만들지 않음
        public static Text After(Text word, string roman, int size, Color color, float gap = 16f)
        {
            if (string.IsNullOrEmpty(roman)) return null;
            var label = UiKit.OneLine(UiKit.Label("Roman", word.transform, roman, size, color, 0, 0, 0, 1, TextAnchor.MiddleLeft));
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            var rt = label.rectTransform;
            rt.pivot = new Vector2(0, 0.5f);
            rt.sizeDelta = new Vector2(400, 0);
            rt.anchoredPosition = new Vector2(word.preferredWidth + gap, 0);
            return label;
        }

        // 글자 칸 안에서 쓰는 서식 글자: 단어 뒤에 작고 흐린 로마자 (도감 자세히 보기처럼 여러 줄 글 한 칸)
        public static string Inline(string word, string roman, int size)
        {
            if (string.IsNullOrEmpty(roman)) return word;
            return $"{word}  <size={size}><color=#{ColorUtility.ToHtmlStringRGB(Palette.TextDim)}>{roman}</color></size>";
        }
    }
}
