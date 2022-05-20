using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class RuntimeScriptableSingletonBuildValidator : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        try
        {
            string errorMessage = RuntimeScriptableSingletonEditor.PreBuildProcess();
            RuntimeScriptableSingletonInitializer.Clear();
            if (errorMessage.Length > 0)
            {
                Debug.LogError("Error");
                throw new UnityEditor.Build.BuildFailedException(new System.Exception(errorMessage));
            }
        }
        catch (System.Exception e) //Relanzamos el error
        {
            throw new UnityEditor.Build.BuildFailedException(e);
        }
    }
}
