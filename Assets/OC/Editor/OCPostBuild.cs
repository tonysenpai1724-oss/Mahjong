using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
public class OCPostBuild : IPostprocessBuildWithReport
{
    public int callbackOrder => 0;


    public void OnPostprocessBuild(BuildReport report)
    {
        BuildVersion buildVersion = Resources.Load<BuildVersion>("BuildVersion");
        if (buildVersion == null)
        {
            Debug.LogError($"BuildVersion asset not found");
            return;
        }

        buildVersion.version++;
        Debug.Log($"Build version incremented to: {buildVersion.version}");

        EditorUtility.SetDirty(buildVersion);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}