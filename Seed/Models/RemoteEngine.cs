using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;

namespace Seed.Models;

/// <summary>
/// A package is a download available from the flax servers. It can be
/// an Editor or Platform Tools.
/// </summary>
public class Package
{
    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; }
    
    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("required")]
    public bool? Required { get; set; }
    
    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("default")]
    public bool? Default { get; set; }
    
    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("targetPath")]
    public string TargetPath { get; set; }
    
    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("url")]
    public string Url { get; set; }

    /// <summary>
    /// Returns true if this package is an editor.
    /// </summary>
    public bool IsEditorPackage => Name.Equals("Editor");

    public bool IsLinuxTools => !IsEditorPackage && Name.Contains("Linux");
    
    public bool IsWindowsTools => !IsEditorPackage && Name.Contains("Windows");
    
    public bool IsMacTools => !IsEditorPackage && Name.Contains("Mac");

    public bool IsAndroidTools => !IsEditorPackage && Name.Contains("Android");
    
    /// <summary>
    /// If this is an editor package, it will return the
    /// appropriate url for the current platform. Otherwise,
    /// it'll return <see cref="string.Empty"/>
    /// </summary>
    public string EditorUrl
    {
        get
        {
            var mainPath = Url[..Url.LastIndexOf('/')];
            
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return Url;
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return $"{mainPath}/FlaxEditorLinux.zip";
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return $"{mainPath}/FlaxEditor.dmg";
            }

            return string.Empty;
        }
    }
    
    public Package(string name, string targetPath, string url)
    {
        Name = name;
        TargetPath = targetPath;
        Url = url;
    }
}

/// <summary>
/// Describes a remote engine as described by the api the official Flax launcher uses.
/// </summary>
public class RemoteEngine: IComparable<RemoteEngine>
{
    /// <summary>
    /// The name of the engine.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; }
    
    /// <summary>
    /// The version of the engine.
    /// </summary>
    [JsonPropertyName("version")]
    public Version Version { get; set; }
    
    /// <summary>
    /// The packages available for this engine version.
    /// </summary>
    [JsonPropertyName("packages")]
    public List<Package> Packages { get; set; }

    /// <summary>
    /// Get the editor package for this engine.
    /// </summary>
    /// <returns></returns>
    public Package GetEditorPackage(OSPlatform platform)
    {
        return Packages.First(x => x.IsEditorPackage);
    }

    public Package GetPlatformTools(OSPlatform platform)
    {
        if (platform == OSPlatform.Windows)
        {
            
        }
        else if (platform == OSPlatform.Linux)
        {
            
        }
        else if (platform == OSPlatform.OSX)
        {
            
        }
        
        throw new ArgumentException($"Unsupported os: {platform}");
    }

    public int CompareTo(RemoteEngine? other)
    {
        if (ReferenceEquals(this, other)) return 0;
        if (ReferenceEquals(null, other)) return 1;
        return Version.CompareTo(other.Version);
    }
}