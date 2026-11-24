using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager.UI;
using UnityEngine;

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
}
