using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class StoryPack
{
    public string format = "v1";
    public string title = "JukeboxText";
    public int version = 1;
    public string description = "";
    public bool nightModeAvailable = false;
    public List<StageNode> stageNodes;
    public List<ActionNode> actionNodes;
}

[System.Serializable]
public class StageNode
{
    public string uuid;
    public string type = "stage";
    public string name = "Stage node";
    public Vector2Int position;
    public string image = null;
    public string audio = null;
    public Transition okTransition = null;
    public Transition homeTransition = null;
    public ControlSettings controlSettings;
    public bool squareOne = false;

    public string Image
    {
        get; set;
    }

    public StageNode(string name, string audio, string image)
    {
        this.name = name;
        this.audio = audio;
        this.image = image;
        if (string.IsNullOrEmpty(audio))
            this.audio = "null";
        if (string.IsNullOrEmpty(image))
            this.image = "null";
        this.uuid = Guid.NewGuid().ToString();
    }

    public void OkHook(ActionNode target, int id)
    {
        this.okTransition = new Transition()
        {
            actionNode = target.id,
            optionIndex = id,
        };
    }

    public void HomeHook(ActionNode target, int id)
    {
        this.homeTransition = new Transition()
        {
            actionNode = target.id,
            optionIndex = id,
        };
    }
}

[System.Serializable]
public class ActionNode
{
    public string id;
    public string name = "Action node";
    public Vector2Int position;
    public string[] options;

    public ActionNode(string name, int optionCount)
    {
        this.name = name;
        this.id = Guid.NewGuid().ToString();
        this.options = new string[optionCount];
    }

    public void Hook(StageNode[] options)
    {
        this.options = new string[options.Length];
        for (int i = 0; i < options.Length; i++)
        {
            this.options[i] = options[i].uuid;
        }
    }

    public void Hook(StageNode option, int optionId)
    {
        this.options[optionId] = option.uuid;
    }
}

[System.Serializable]
public class Transition
{
    public string actionNode;
    public int optionIndex;
}

[System.Serializable]
public class ControlSettings
{
    public bool wheel = false;
    public bool ok = false;
    public bool home = false;
    public bool pause = false;
    public bool autoplay = false;
}
