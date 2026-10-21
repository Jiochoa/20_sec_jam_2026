using UnityEditor;
using UnityEngine;

//--------------------------------------------------------------------
//One-shot maintenance tool. The Character Editor Package ships .anim/.mat/.controller
//files saved in a pre-2019.1 serialization format, which Unity 6.5 logs a deprecation
//warning for on every import ("Serialized files [version 15] before 2019.1 are deprecated").
//Nothing is broken - Unity still reads them - but the warnings bury real ones in the console.
//Rewriting each asset in the current format silences them for good.
//This only covers files inside Assets/; ProjectSettings/ is not part of the AssetDatabase
//and is re-saved instead by switching Edit > Project Settings > Editor > Asset Serialization.
//--------------------------------------------------------------------
public static class ReserializeAssetsTool
{
    [MenuItem("Tools/Reserialize All Assets (fix deprecated format warnings)")]
    private static void ReserializeAll()
    {
        if (!EditorUtility.DisplayDialog("Reserialize All Assets",
                "This rewrites every asset under Assets/ in the current serialization format.\n\n" +
                "It touches a lot of files at once and cannot be undone, so back up the project folder first.\n\n" +
                "Continue?",
                "Reserialize", "Cancel"))
        {
            return;
        }

        AssetDatabase.ForceReserializeAssets();
        AssetDatabase.SaveAssets();
        Debug.Log("Reserialized all assets under Assets/ to the current serialization version.");
    }
}
