using System.Collections;
using UnityEngine;

namespace Watermelon
{
    [RegisterModule("Monetization/Ads Manager")]
    public class AdsManagerInitModule : InitModule
    {
        public AdsSettings Settings;
        public GameObject GDPRPrefab;

        [Space]
        public bool LoadAdOnStart = true;

        public AdsManagerInitModule()
        {
            moduleName = "Ads Manager";
        }

        public override void CreateComponent(Initialiser initialiser)
        {
            AdsManager.Initialise(this, LoadAdOnStart);
        }
    }
}