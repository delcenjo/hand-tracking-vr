using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager.UI;
using UnityEditor.Rendering;
using UnityEditor.Rendering.Universal;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEditor.XR.OpenXR.Features;
using UnityEngine;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;

public static class HandTrackingSetup
{
    static readonly (string paquete, string ejemplo)[] ejemplos =
    {
        ("com.unity.xr.interaction.toolkit", "Starter Assets"),
        ("com.unity.xr.interaction.toolkit", "XR Device Simulator"),
        ("com.unity.xr.hands", "HandVisualizer"),
        ("com.unity.xr.hands", "Gestures"),
        ("com.unity.xr.interaction.toolkit", "Hands Interaction Demo"),
    };

    static readonly string[] features =
    {
        "HandTracking",
        "MetaHandTrackingAim",
        "HandInteractionProfile",
        "OculusTouchControllerProfile",
        "MetaQuestTouchPlusControllerProfile",
        "MetaQuestFeature",
    };

    [MenuItem("Hand Tracking/Importar samples", priority = 1)]
    public static void ImportarSamples()
    {
        foreach (var (paquete, nombre) in ejemplos)
        {
            var info = UnityEditor.PackageManager.PackageInfo.FindForPackageName(paquete);
            if (info == null)
            {
                Debug.LogError("Falta el paquete " + paquete);
                continue;
            }
            var sample = Sample.FindByPackage(paquete, info.version).FirstOrDefault(s => s.displayName == nombre);
            if (string.IsNullOrEmpty(sample.displayName) || sample.isImported) continue;
            sample.Import(Sample.ImportOptions.HideImportWindow);
        }
        AssetDatabase.Refresh();
    }

    [MenuItem("Hand Tracking/Configurar OpenXR", priority = 2)]
    public static void ConfigurarOpenXR()
    {
        if (!EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out XRGeneralSettingsPerBuildTarget xrSettings) || xrSettings == null)
        {
            xrSettings = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
            if (!AssetDatabase.IsValidFolder("Assets/XR")) AssetDatabase.CreateFolder("Assets", "XR");
            AssetDatabase.CreateAsset(xrSettings, "Assets/XR/XRGeneralSettingsPerBuildTarget.asset");
            EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, xrSettings, true);
        }

        foreach (var grupo in new[] { BuildTargetGroup.Standalone, BuildTargetGroup.Android })
        {
            if (!xrSettings.HasSettingsForBuildTarget(grupo)) xrSettings.CreateDefaultSettingsForBuildTarget(grupo);
            if (!xrSettings.HasManagerSettingsForBuildTarget(grupo)) xrSettings.CreateDefaultManagerSettingsForBuildTarget(grupo);
            xrSettings.SettingsForBuildTarget(grupo).InitManagerOnStart = true;
            XRPackageMetadataStore.AssignLoader(xrSettings.ManagerSettingsForBuildTarget(grupo), typeof(OpenXRLoader).FullName, grupo);

            FeatureHelpers.RefreshFeatures(grupo);
            var openxr = OpenXRSettings.GetSettingsForBuildTargetGroup(grupo);
            if (openxr == null) continue;
            foreach (var f in openxr.GetFeatures())
            {
                if (!features.Contains(f.GetType().Name)) continue;
                f.enabled = true;
                EditorUtility.SetDirty(f);
            }
            EditorUtility.SetDirty(openxr);
            Debug.Log(grupo + ": " + string.Join(", ", openxr.GetFeatures().Where(f => f.enabled).Select(f => f.GetType().Name)));
        }

        PasarMaterialesAURP();

        // Ajustes que pide Meta para Quest
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel32;
        PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        AssetDatabase.SaveAssets();
    }

    // Los samples de XRI traen materiales del render integrado y en URP salen en rosa
    [MenuItem("Hand Tracking/Pasar materiales de los samples a URP", priority = 3)]
    public static void PasarMaterialesAURP()
    {
        var interactable = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Samples/XR Interaction Toolkit/3.7.0/Starter Assets/Shaders/Interactable.shadergraph");
        int n = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { "Assets/Samples" }))
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
            if (m == null) continue;
            var nombre = m.shader.name;
            if (nombre == "Standard" || nombre == "Standard (Specular setup)")
            {
                MaterialUpgrader.Upgrade(m, new StandardUpgrader(nombre), MaterialUpgrader.UpgradeFlags.None);
            }
            else if (nombre == "XRIT/BiRP_Fresnel" && interactable != null)
            {
                var textura = m.GetTexture("_MainTex");
                m.shader = interactable;
                if (textura != null) m.SetTexture("_MainTexture", textura);
            }
            else continue;
            EditorUtility.SetDirty(m);
            n++;
        }
        AssetDatabase.SaveAssets();
        Debug.Log("Materiales pasados a URP: " + n);
    }
}
