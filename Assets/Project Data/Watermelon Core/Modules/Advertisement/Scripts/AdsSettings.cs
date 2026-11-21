#pragma warning disable 0414

using UnityEngine;

namespace Watermelon
{
    [SetupTab("Advertising", texture = "icon_ads")]
    [CreateAssetMenu(fileName = "Ads Settings", menuName = "Settings/Ads Settings")]
    [HelpURL("https://www.notion.so/wmelongames/Advertisement-221053e32d4047bb880275027daba9f0?pvs=4")]
    public class AdsSettings : ScriptableObject
    {
        [SerializeField] AdProvider bannerType = AdProvider.Disable;
        public AdProvider BannerType => bannerType;

        [SerializeField] AdProvider interstitialType = AdProvider.TikTok;
        public AdProvider InterstitialType => interstitialType;

        [SerializeField] AdProvider rewardedVideoType = AdProvider.TikTok;
        public AdProvider RewardedVideoType => rewardedVideoType;

        [Tooltip("Enables development mode to setup advertisement providers.")]
        [SerializeField] bool testMode = false;
        public bool TestMode => testMode;

        [Group("Settings")]
        [Tooltip("Enables logging. Use it to debug advertisement logic.")]
        [SerializeField] bool systemLogs = false;
        public bool SystemLogs => systemLogs;

        [Space]
        [Group("Settings")]
        [Tooltip("Delay in seconds before interstitial appearings on first game launch.")]
        [SerializeField] float interstitialFirstStartDelay = 40f;
        public float InterstitialFirstStartDelay => interstitialFirstStartDelay;

        [Group("Settings")]
        [Tooltip("Delay in seconds before interstitial appearings.")]
        [SerializeField] float interstitialStartDelay = 40f;
        public float InterstitialStartDelay => interstitialStartDelay;

        [Group("Settings")]
        [Tooltip("Delay in seconds between interstitial appearings.")]
        [SerializeField] float interstitialShowingDelay = 30f;
        public float InterstitialShowingDelay => interstitialShowingDelay;

        [Group("Settings")]
        [SerializeField] bool autoShowInterstitial;
        public bool AutoShowInterstitial => autoShowInterstitial;

        [Group("Privacy")]
        [SerializeField] bool isGDPREnabled = false;
        public bool IsGDPREnabled => isGDPREnabled;

        [Group("Privacy")]
        [SerializeField] bool isIDFAEnabled = false;
        public bool IsIDFAEnabled => isIDFAEnabled;

        [Group("Privacy")]
        [SerializeField] string trackingDescription = "Your data will be used to deliver personalized ads to you.";
        public string TrackingDescription => trackingDescription;

        [Group("Privacy")]
        [SerializeField] string privacyLink = "https://mywebsite.com/privacy";
        public string PrivacyLink => privacyLink;

        [Group("Privacy")]
        [SerializeField] string termsOfUseLink = "https://mywebsite.com/terms";
        public string TermsOfUseLink => termsOfUseLink;
    }

    public enum BannerPosition
    {
        Bottom = 0,
        Top = 1,
    }
}