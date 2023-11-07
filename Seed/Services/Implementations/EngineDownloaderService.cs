#define USE_JSON_FILE
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using Seed.Models;

namespace Seed.Services;

public class EngineDownloaderService: IEngineDownloaderService
{
    public const string ApiUrl = "https://api.flaxengine.com/launcher/engine";

    public event Action<string> ActionChanged;
    
    private HttpClient _client = new();
    private Progress<float> _progress = new();
    public Progress<float> Progress => _progress;
    private string _currentAction;
    public string CurrentAction
    {
        get => _currentAction;
        private set
        {
            _currentAction = value;
            ActionChanged?.Invoke(value);
        } 
    }

    public EngineDownloaderService()
    {
    }
    
    public async Task<List<RemoteEngine>?> GetAvailableVersions()
    {
#if USE_JSON_FILE
        var json = await File.ReadAllTextAsync("/home/minebill/git/Seed/Seed/Assets/api.json");
#else
        _client.DefaultRequestHeaders.Accept.Clear();
        _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _client.DefaultRequestHeaders.Add("User-Agent", "Seed Launcher for Flax");

        var json = await _client.GetStringAsync(ApiUrl);
#endif
        try
        {
            var tree = JsonNode.Parse(json);
            if (tree is null) 
                return null;
            
            var engines = tree["versions"].Deserialize<List<RemoteEngine>>();
            return engines;
        }
        catch (JsonException je)
        {
            var box = MessageBoxManager.GetMessageBoxStandard(
                "Exception",
                "An exception occured while deserializing information from the Flax API. It's possible the API changed. Please make an issue at <url-repo>.",
                icon: Icon.Error);
            await box.ShowWindowDialogAsync(App.Current.MainWindow);
            return null;
        }
    }

    public async Task<Engine> DownloadVersion(RemoteEngine engine, List<RemotePackage> platformTools, string installFolderPath)
    {
        var tempEditorFile = Path.GetTempFileName();
#if NODEBUG
        var editorUrl = "/home/minebill/Downloads/FlaxEditorLinux.zip";
        File.Move(editorUrl, tempEditorFile, true);
#else
        CurrentAction = $"Downloading {engine.Name}";
        var editorUrl = engine.GetEditorPackage().EditorUrl;
        await using (var file = new FileStream(tempEditorFile, FileMode.Create, FileAccess.Write, FileShare.None))
            await _client.DownloadDataAsync(editorUrl, file, _progress);
        // await DownloadFile(editorUrl, tempEditorFile);
#endif
        // create sub folder for this engine installation
        var editorInstallFolder = Path.Combine(installFolderPath, engine.Name);
        
        // TODO: Check for errors
        // ZipFile.ExtractToDirectory(tempEditorFile, editorInstallFolder);
        CurrentAction = "Extracting editor";
        await ZipHelpers.ExtractToDirectoryAsync(tempEditorFile, editorInstallFolder, _progress);
        
#if DELETE_TMP_FILES
        File.Delete(tempEditorFile);
#endif

        var installedPackages = new List<Package>(platformTools.Count);
        foreach (var tools in platformTools)
        {
            CurrentAction = $"Downloading platform tools for {tools.Name}";
            var tmpFile = Path.GetTempFileName();
            await using (var file = new FileStream(tmpFile, FileMode.Create, FileAccess.Write, FileShare.None))
                await _client.DownloadDataAsync(tools.Url, file, _progress);

            var installFolder = Path.Combine(editorInstallFolder, tools.TargetPath);
            CurrentAction = $"Extracting {tools.Name}";
            await ZipHelpers.ExtractToDirectoryAsync(tmpFile, installFolder, _progress);

            installedPackages.Add(new Package(tools.Name, installFolder));
#if DELETE_TMP_FILES
            File.Delete(tmpFile);
#endif
        }
        
        return new Engine
        {
            Name = engine.Name,
            Path = editorInstallFolder,
            Version = engine.Version,
            InstalledPackages = installedPackages
        };
    }

    private async Task DownloadFile(string uri, string outputFile)
    {
        if (!Uri.TryCreate(uri, UriKind.Absolute, out var uriResult))
            throw new InvalidOperationException("URI is invalid.");

        using var response = await _client.GetAsync(uriResult, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        await using var fileStream = File.OpenWrite(outputFile);
        await using var httpStream = await response.Content.ReadAsStreamAsync();
        await httpStream.CopyToAsync(fileStream);
    }
}