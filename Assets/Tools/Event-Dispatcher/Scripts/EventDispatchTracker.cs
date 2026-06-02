using System;
using System.Collections.Generic;

public class EventDispatchTracker
{
    // Log File
    public const string logFileName = "EventDispatchLog.txt";
    public const string logFilePath = "Assets/Plugins/EventDispatcher/Log/";

    public class EventDispatchInfo
    {
        public string eventName = "";
        public ParamList eventParam = null;
        public float dispatchTime = 0.0f;
        public uint dispatchFrame = 0U;
        public int bindingCount = 0;

        public EventDispatchInfo(string eventName, float dispatchTime, uint dispatchFrame, int bindingCount, ParamList eventParam)
        {
            this.eventName = eventName;
            this.eventParam = eventParam;
            this.dispatchTime = dispatchTime;
            this.bindingCount = bindingCount;
            this.dispatchFrame = dispatchFrame;
        }
    }

    private List<EventDispatchInfo> eventDispatchInfos = new List<EventDispatchInfo>();

    public void AddEventDispatchInfo(string eventName, float dispatchTime, uint dispatchFrame, int bindingsCount, ParamList eventParam)
    {
        AddEventDispatchInfo(new EventDispatchInfo(eventName, dispatchTime, dispatchFrame, bindingsCount, eventParam));
    }

    public void AddEventDispatchInfo(EventDispatchInfo dispatchInfo)
    {
        eventDispatchInfos.Add(dispatchInfo);
    }

    public void ClearEventDispatchInfos()
    {
        eventDispatchInfos.Clear();
    }

    public void LogEventDispatchInfos()
    {
        string logContent = FormatEventDispatchInfos();

        string fullPath = System.IO.Path.Combine(logFilePath, logFileName);

        CreateOutputDirectory(fullPath);

        System.IO.File.WriteAllText(fullPath, logContent);
    }

    #region Format Helper
    private string FormatHeader()
    {
        return $"Event Dispatch Log - {DateTime.Now}" +
            "\n----------------------------------------\n";
    }
    private string FormatFrameSeparator(uint currentFrame)
    {
        return $"--- Frame {currentFrame} ---------------\n";
    }
    private string FormatEventDispatchInfo(EventDispatchInfo info)
    {
        string output = "";

        output += $"Time: {info.dispatchTime} | Event: {info.eventName} | Bindings : {info.bindingCount}";

        if (info.eventParam == null) return output + "\n";

        output += $" | Params: {info.eventParam}\n";

        return output;
    }
    private string FormatEventDispatchInfos()
    {
        string logContent = FormatHeader();

        uint currentFrame = 0;
        foreach (var info in eventDispatchInfos)
        {
            // Add frame separation
            if (currentFrame != info.dispatchFrame)
            {
                currentFrame = info.dispatchFrame;
                logContent += FormatFrameSeparator(currentFrame);
            }

            // log event infos
            logContent += FormatEventDispatchInfo(info);
        }

        return logContent;
    }
    private void CreateOutputDirectory(string path)
    {
        if (!System.IO.Directory.Exists(logFilePath))
        {
            System.IO.Directory.CreateDirectory(logFilePath);
        }
    }
    #endregion
}
