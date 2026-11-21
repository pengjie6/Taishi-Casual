#pragma warning disable 0618

using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    [System.Serializable]
    public class UIMainMenuButton
    {
        [SerializeField] RectTransform rect;
        [SerializeField] Button button;
        public Button Button => button;

        /// <summary>
        /// 场景里可能把按钮对象删掉了，序列化引用会变成"已销毁"状态，需要先判断有效性
        /// </summary>
        public bool IsValid => rect != null && button != null;

        [Space]
        [SerializeField] AnimationCurve showStoreAdButtonsCurve;
        [SerializeField] AnimationCurve hideStoreAdButtonsCurve;
        [SerializeField] float showHideDuration;

        private float savedRectPosX;
        private float rectXPosBehindOfTheScreen;

        private TweenCase showHideCase;

        public void Init(float rectXPosBehindOfTheScreen)
        {
            this.rectXPosBehindOfTheScreen = rectXPosBehindOfTheScreen;

            if (rect == null) return;

            savedRectPosX = rect.anchoredPosition.x;
        }

        public void Show(bool immediately = false)
        {
            if (rect == null) return;

            if (showHideCase != null && showHideCase.IsActive) return;

            if (immediately)
            {
                rect.anchoredPosition = rect.anchoredPosition.SetX(savedRectPosX);
                return;
            }

            //RESET
            rect.anchoredPosition = rect.anchoredPosition.SetX(rectXPosBehindOfTheScreen);

            showHideCase = rect.DOAnchoredPosition(rect.anchoredPosition.SetX(savedRectPosX), showHideDuration).SetCurveEasing(showStoreAdButtonsCurve);
        }

        public void Hide(bool immediately = false)
        {
            if (rect == null) return;

            if (showHideCase != null && showHideCase.IsActive) return;

            if (immediately)
            {
                rect.anchoredPosition = rect.anchoredPosition.SetX(rectXPosBehindOfTheScreen);
                return;
            }

            //RESET
            rect.anchoredPosition = rect.anchoredPosition.SetX(savedRectPosX);

            showHideCase = rect.DOAnchoredPosition(rect.anchoredPosition.SetX(rectXPosBehindOfTheScreen), showHideDuration).SetCurveEasing(hideStoreAdButtonsCurve);
        }
    }
}
