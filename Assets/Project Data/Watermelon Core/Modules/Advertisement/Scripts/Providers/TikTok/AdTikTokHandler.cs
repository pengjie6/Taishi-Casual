using UnityEngine;

namespace Watermelon
{
    /// <summary>
    /// 抖音小游戏广告适配层：把 Watermelon 模板的激励视频 / 插屏转发到 AdSDK 的 AdManager（TikTokPlatform）。
    /// 抖音的 IAdPlatform 没有 banner 接口，横幅相关方法按空实现处理。
    /// 编辑器 / 非 WebGL 环境下没有真实广告，直接按「看完」处理，保证调试时奖励流程能走通。
    /// </summary>
    public class AdTikTokHandler : AdProviderHandler
    {
        private bool isInterstitialLoaded;
        private bool isRewardedVideoLoaded;

        public AdTikTokHandler(AdProvider providerType) : base(providerType) { }

        public override void Initialise(AdsSettings adsSettings)
        {
            this.adsSettings = adsSettings;

            OnProviderInitialised();
        }

        #region Banner
        public override void ShowBanner()
        {
            // 抖音 IAdPlatform 未提供 banner 接口，留空
        }

        public override void HideBanner()
        {
        }

        public override void DestroyBanner()
        {
        }
        #endregion

        #region Interstitial
        public override void RequestInterstitial()
        {
            isInterstitialLoaded = true;

            AdsManager.OnProviderAdLoaded(providerType, AdType.Interstitial);
        }

        public override bool IsInterstitialLoaded()
        {
            return isInterstitialLoaded;
        }

        public override void ShowInterstitial(InterstitialCallback callback)
        {
            if (AdManager.Instance == null)
            {
                Debug.LogWarning("[AdsManager]: 场景里没有 AdManager（抖音广告），插屏已跳过");

                AdsManager.ExecuteInterstitialCallback(false);
                return;
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                AdManager.Instance.ShowInterstitialAd();

                AdsManager.OnProviderAdDisplayed(providerType, AdType.Interstitial);
                AdsManager.OnProviderAdClosed(providerType, AdType.Interstitial);
                AdsManager.ExecuteInterstitialCallback(true);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[AdsManager]: 抖音插屏展示异常：" + e.Message);

                AdsManager.ExecuteInterstitialCallback(false);
            }
#else
            // 编辑器 / 非抖音环境：没有真实广告，模拟展示完成
            Debug.Log("[AdsManager]: 当前不是抖音运行环境，模拟插屏广告展示");

            AdsManager.OnProviderAdDisplayed(providerType, AdType.Interstitial);
            AdsManager.OnProviderAdClosed(providerType, AdType.Interstitial);
            AdsManager.ExecuteInterstitialCallback(true);
#endif
        }
        #endregion

        #region Rewarded Video
        public override void RequestRewardedVideo()
        {
            isRewardedVideoLoaded = true;

            AdsManager.OnProviderAdLoaded(providerType, AdType.RewardedVideo);
        }

        public override bool IsRewardedVideoLoaded()
        {
            return isRewardedVideoLoaded;
        }

        public override void ShowRewardedVideo(RewardedVideoCallback callback)
        {
            AdsManager.OnProviderAdDisplayed(providerType, AdType.RewardedVideo);

#if UNITY_WEBGL && !UNITY_EDITOR
            if (AdManager.Instance == null)
            {
                Debug.LogError("[AdsManager]: 场景里没有 AdManager（抖音广告），激励视频无法展示");

                AdsManager.OnProviderAdClosed(providerType, AdType.RewardedVideo);
                AdsManager.ExecuteRewardVideoCallback(false);
                return;
            }

            AdManager.Instance.ShowRewardedVideoAd(
                () =>
                {
                    // 抖音激励视频播放完成
                    AdsManager.OnProviderAdClosed(providerType, AdType.RewardedVideo);
                    AdsManager.ExecuteRewardVideoCallback(true);
                },
                (error) =>
                {
                    Debug.LogWarning("[AdsManager]: 抖音激励视频未完成：" + error);

                    AdsManager.OnProviderAdClosed(providerType, AdType.RewardedVideo);
                    AdsManager.ExecuteRewardVideoCallback(false);
                });
#else
            // 编辑器 / 非抖音环境：没有真实广告，模拟播放完成
            Debug.Log("[AdsManager]: 当前不是抖音运行环境，模拟激励视频播放成功");

            AdsManager.OnProviderAdClosed(providerType, AdType.RewardedVideo);
            AdsManager.ExecuteRewardVideoCallback(true);
#endif
        }
        #endregion
    }
}
