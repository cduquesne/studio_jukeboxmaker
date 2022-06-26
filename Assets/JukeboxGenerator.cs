using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.IO.Compression;
using System;

public class JukeboxGenerator : MonoBehaviour
{
    public string basePath;
    public string outputPath;
    public TMPro.TextMeshProUGUI textComponent;

    public StoryPack storyPack;

    public Vector2Int positionAtStart;
    public Vector2Int positionOffset;

    private Vector2Int positionIterator;

    private Dictionary<string, string> trackNameToImage = new Dictionary<string, string>();
    private Dictionary<string, string> trackGuidToTrackName = new Dictionary<string, string>();

    private DirectoryInfo assetFolder;
    private DirectoryInfo archiveFolder;

    private void Start()
    {
        Generate();
    }

    [ContextMenu("Generate")]
    public void Generate()
    {
        var settings = File.ReadAllText(Application.dataPath + "\\..\\settings.txt");
        var splitSettings = settings.Split(',');
        foreach(var split in splitSettings)
        {
            var s = split.Trim();
            if (s.StartsWith("inputPath=", StringComparison.InvariantCulture))
                this.basePath = s.Replace("inputPath=", "");
            if (s.StartsWith("outputPath=", StringComparison.InvariantCulture))
                this.outputPath = s.Replace("outputPath=", "");
        }

        var archiveGuid = Guid.NewGuid().ToString();
        DirectoryInfo baseFolder = new DirectoryInfo(basePath);
        archiveFolder = new DirectoryInfo(this.outputPath + baseFolder.Name + "-" + archiveGuid + "-v1");
        assetFolder = new DirectoryInfo(archiveFolder.FullName+"\\assets");
        if (archiveFolder.Exists)
        {
            archiveFolder.Delete(true);
        }
        Directory.CreateDirectory(archiveFolder.FullName);
        Directory.CreateDirectory(assetFolder.FullName);

        storyPack = new StoryPack();
        storyPack.stageNodes = new List<StageNode>();
        storyPack.actionNodes = new List<ActionNode>();


        this.positionIterator = this.positionAtStart;

        this.trackNameToImage.Add("Jukebox", "Jukebox.png");
        var startNode = new StageNode("StartNode", null, "Jukebox.png");
        startNode.controlSettings = new ControlSettings() { ok = true };
        startNode.squareOne = true;
        startNode.position = GetNextPosition();
        storyPack.stageNodes.Add(startNode);

        var folderGuid = Guid.NewGuid().ToString().Replace("-", "");
        var choiceNode = ParseFolderRecursive(this.basePath, null, 0, folderGuid);
        startNode.OkHook(choiceNode,0);

        var jsonResult = JsonUtility.ToJson(storyPack);
        var blankTransition = JsonUtility.ToJson(new Transition());
        jsonResult = jsonResult.Replace("\"null\"", "null");
        jsonResult = jsonResult.Replace(blankTransition, "null");
        File.WriteAllText(this.archiveFolder.FullName+"\\story.json", jsonResult);
        Debug.Log(jsonResult);

        StartCoroutine(GenerateTextImages());
    }

    private IEnumerator GenerateTextImages()
    {
        foreach(var img in trackNameToImage)
        {
            textComponent.text = img.Key;
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(
                    this.assetFolder.FullName +"\\"+ img.Value);
            yield return new WaitForEndOfFrame();
        }
        textComponent.text = "Generating zip archive...";
        yield return new WaitForSeconds(0.5f);
        FileInfo archive = new FileInfo(archiveFolder.FullName + ".zip");
        if (archive.Exists)
            archive.Delete();
        ZipFile.CreateFromDirectory(archiveFolder.FullName, archiveFolder.FullName + ".zip");
        archiveFolder.Delete(true);
        Application.Quit();
    }

    private ActionNode ParseFolderRecursive(string folderPath, ActionNode homeNode, int choiceId, string folderGuid)
    {
        DirectoryInfo folder = new DirectoryInfo(folderPath);
        var files = folder.GetFiles();
        var trackList = new string[files.Length];

        for (int i = 0; i < files.Length; i++)
        {
            trackList[i] = Guid.NewGuid().ToString().Replace("-", "") + files[i].Extension;
            trackGuidToTrackName.Add(trackList[i], files[i].Name.Replace(files[i].Extension,""));
            files[i].CopyTo(assetFolder.FullName + '\\' + trackList[i],true);
        }

        var subfolders = folder.GetDirectories();
        var folderList = new string[subfolders.Length];
        for(int i = 0; i < subfolders.Length;i++)
        {
            folderList[i] = subfolders[i].FullName;
        }

        var choiceCount = trackList.Length + folderList.Length;

        var choiceNode = new ActionNode("ChoiceNode"+folderGuid, choiceCount);
        
        choiceNode.position = GetNextPosition();
        storyPack.actionNodes.Add(choiceNode);

        StageNode[] folderNodes = new StageNode[folderList.Length];
        for (int i = 0; i < folderNodes.Length; i++)
        {
            folderNodes[i] = MakeFolderNode(subfolders[i], homeNode,choiceNode, choiceId, i);
            choiceNode.Hook(folderNodes[i], i);
            storyPack.stageNodes.Add(folderNodes[i]);
            folderNodes[i].position = GetNextPosition();
        }

        StageNode[] trackNodes = new StageNode[trackList.Length];
        for (int i = 0; i < trackNodes.Length; i++)
        {
            trackNodes[i] = MakeTrackNode(trackList[i], homeNode, choiceNode, choiceId) ;
            if (i > 0)
                trackNodes[i - 1].OkHook(choiceNode, i);
            
            choiceNode.Hook(trackNodes[i], i + folderList.Length);
            storyPack.stageNodes.Add(trackNodes[i]);
            trackNodes[i].position = GetNextPosition();
            
            if(trackNodes.Length <= 1)
            {
                trackNodes[i].okTransition = null;
                trackNodes[i].controlSettings.autoplay = false;
            }

        }

        if(trackNodes.Length > 1)
        trackNodes[trackNodes.Length - 1].OkHook(choiceNode, 0);

        return choiceNode;
    }

    private StageNode MakeTrackNode(string audiofileId, ActionNode homeNode, ActionNode newHomeNode, int choiceId)
    {
        var trackGuid = Guid.NewGuid().ToString().Replace("-", "");
        this.trackNameToImage.Add(this.trackGuidToTrackName[audiofileId], trackGuid + ".png");
        var stageNode = new StageNode(audiofileId + "TrackStageNode", audiofileId, trackGuid + ".png");
        stageNode.controlSettings = new ControlSettings() { autoplay = true, pause = true, wheel = true, home = true };
        stageNode.HomeHook(homeNode, choiceId);
        return stageNode;
    }

    private StageNode MakeFolderNode(DirectoryInfo subfolder, ActionNode homeNode, ActionNode newHomeNode, int choiceId, int newChoiceId)
    {
        var folderGuid = Guid.NewGuid().ToString().Replace("-", "");
        this.trackNameToImage.Add(subfolder.Name, folderGuid +".png");
        var stageNode = new StageNode(folderGuid + "FolderStageNode", null, this.trackNameToImage[subfolder.Name]);
        stageNode.controlSettings = new ControlSettings() { wheel = true, ok = true, home = true };
        
        var actionNode = ParseFolderRecursive(subfolder.FullName, newHomeNode, newChoiceId, folderGuid);
        stageNode.OkHook(actionNode,0);
        if (homeNode != null)
            stageNode.HomeHook(homeNode, choiceId);
        
        return stageNode;
    }

    private Vector2Int GetNextPosition()
    {
        var result = positionIterator;
        positionIterator += this.positionOffset;
        return result;
    }
}
