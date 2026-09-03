using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEditor.iOS.Xcode.Extensions;
#endif

public class PostProcessHyprMX : MonoBehaviour {
    private const string TargetUnityIphonePodfileLine = "target 'Unity-iPhone' do";
    private const string UseFrameworksPodfileLine = "use_frameworks!";
    private const string UseFrameworksDynamicPodfileLine = "use_frameworks! :linkage => :dynamic";
    private const string UseFrameworksStaticPodfileLine = "use_frameworks! :linkage => :static";

    const string hyprMXPodsDirectory = "Pods/HyprMX/";
    const string coreHyprMXFrameworkName = "HyprMX.xcframework";
    [PostProcessBuild(101)]
    private static void PostProcessBuild_HyprMX(BuildTarget target, string buildPath)
    {
#if UNITY_EDITOR_OSX
        if (target == BuildTarget.iOS) {
            
            string projPath = PBXProject.GetPBXProjectPath(buildPath);
            PBXProject proj = new PBXProject();
            proj.ReadFromString(File.ReadAllText(projPath));

            // Determine if the target uses static or dynamically linked frameworks
            bool isStaticallyLinked = ShouldEmbedDynamicLibraries(buildPath);

            if (isStaticallyLinked) 
            {
#if UNITY_2019_3_OR_NEWER
                // HyprMX is installed/linked by CocoaPods to the UnityFramework.framework, but the dynamic lib still
                // needs to be embedded into the application target.
                Debug.Log ("Embedding HyprMX.xcframework into Unity-iPhone target...");
                string targetGuid = proj.GetUnityMainTargetGuid();
                EmbedHyprMXFramework(proj, projPath, targetGuid, coreHyprMXFrameworkName);
#else
                // Unity projects before 2019.3 do not contain runtime search paths needed for dynamic libraries.
                Debug.Log ("Adding the runtime search path to Unity-iPhone target...");
                string targetGuid = proj.TargetGuidByName("Unity-iPhone");
                proj.SetBuildProperty(targetGuid, "LD_RUNPATH_SEARCH_PATHS", "$(inherited) @executable_path/Frameworks");
#endif
                proj.WriteToFile (projPath);
            }
        }
#endif
    }
    
    private static void EmbedHyprMXFramework(PBXProject proj, string projPath, string targetGuid, string framework)
    {
#if UNITY_EDITOR_OSX
        string hyprMXframeworkPath = hyprMXPodsDirectory + framework;
        string fileGuid = proj.AddFile(hyprMXframeworkPath, hyprMXframeworkPath, PBXSourceTree.Source);
        PBXProjectExtensions.AddFileToEmbedFrameworks(proj, targetGuid, fileGuid);
#endif
    }

    /// <summary>
    /// |-----------------------------------------------------------------------------------------------------------------------------------------------------|
    /// |         embed             |  use_frameworks! (:linkage => :dynamic)  |  use_frameworks! :linkage => :static  |  `use_frameworks!` line not present  |
    /// |---------------------------|------------------------------------------|---------------------------------------|--------------------------------------|
    /// | Unity-iPhone present      | Do not embed dynamic libraries           | Embed dynamic libraries               | Do not embed dynamic libraries       |
    /// | Unity-iPhone not present  | Embed dynamic libraries                  | Embed dynamic libraries               | Embed dynamic libraries              |
    /// |-----------------------------------------------------------------------------------------------------------------------------------------------------|
    /// </summary>
    /// <param name="buildPath">An iOS build path</param>
    /// <returns>Whether or not the dynamic libraries should be embedded.</returns>
    private static bool ShouldEmbedDynamicLibraries(string buildPath)
    {
        var podfilePath = Path.Combine(buildPath, "Podfile");
        if (!File.Exists(podfilePath)) return false;

        // If the Podfile doesn't have a `Unity-iPhone` target, we should embed the dynamic libraries.
        var lines = File.ReadAllLines(podfilePath);
        var containsUnityIphoneTarget = lines.Any(line => line.Contains(TargetUnityIphonePodfileLine));
        if (!containsUnityIphoneTarget) return true;

        // If the Podfile does not have a `use_frameworks! :linkage => static` line, we should not embed the dynamic libraries.
        var useFrameworksStaticLineIndex = Array.FindIndex(lines, line => line.Contains(UseFrameworksStaticPodfileLine));
        if (useFrameworksStaticLineIndex == -1) return false;

        // If more than one of the `use_frameworks!` lines are present, CocoaPods will use the last one.
        var useFrameworksLineIndex = Array.FindIndex(lines, line => line.Trim() == UseFrameworksPodfileLine); // Check for exact line to avoid matching `use_frameworks! :linkage => static/dynamic`
        var useFrameworksDynamicLineIndex = Array.FindIndex(lines, line => line.Contains(UseFrameworksDynamicPodfileLine));

        // Check if `use_frameworks! :linkage => :static` is the last line of the three. If it is, we should embed the dynamic libraries.
        return useFrameworksLineIndex < useFrameworksStaticLineIndex && useFrameworksDynamicLineIndex < useFrameworksStaticLineIndex;
    }
}