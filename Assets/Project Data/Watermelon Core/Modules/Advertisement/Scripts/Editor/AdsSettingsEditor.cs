using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Watermelon
{
    [CustomEditor(typeof(AdsSettings))]
    public class AdsSettingsEditor : Editor
    {
        private SerializedProperty bannerTypeProperty;
        private SerializedProperty interstitialTypeProperty;
        private SerializedProperty rewardedVideoTypeProperty;
        private SerializedProperty testModeProperty;

        private IEnumerable<SerializedProperty> gdprProperties;
        private IEnumerable<SerializedProperty> settingsProperties;

        protected void OnEnable()
        {
            bannerTypeProperty = serializedObject.FindProperty("bannerType");
            interstitialTypeProperty = serializedObject.FindProperty("interstitialType");
            rewardedVideoTypeProperty = serializedObject.FindProperty("rewardedVideoType");

            gdprProperties = serializedObject.GetPropertiesByGroup("Privacy");
            settingsProperties = serializedObject.GetPropertiesByGroup("Settings");

            testModeProperty = serializedObject.FindProperty("testMode");

            serializedObject.ApplyModifiedProperties();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.BeginVertical(WatermelonEditor.Styles.Skin.box);

            EditorGUILayoutCustom.Header("ADVERTISING");

            // 只保留抖音：Disable / TikTok
            EditorGUILayout.PropertyField(bannerTypeProperty);
            EditorGUILayout.PropertyField(interstitialTypeProperty);
            EditorGUILayout.PropertyField(rewardedVideoTypeProperty);

            EditorGUILayout.EndVertical();

            GUILayout.Space(8);

            EditorGUILayout.BeginVertical(WatermelonEditor.Styles.Skin.box);

            EditorGUILayoutCustom.Header("SETTINGS");

            EditorGUILayout.PropertyField(testModeProperty);

            foreach (SerializedProperty prop in settingsProperties)
            {
                EditorGUILayout.PropertyField(prop);
            }

            GUILayout.Space(5);

            EditorGUILayout.EndVertical();

            GUILayout.Space(8);

            EditorGUILayout.BeginVertical(WatermelonEditor.Styles.Skin.box);

            EditorGUILayoutCustom.Header("PRIVACY");

            foreach (SerializedProperty prop in gdprProperties)
            {
                EditorGUILayout.PropertyField(prop);
            }

            EditorGUILayout.EndVertical();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
