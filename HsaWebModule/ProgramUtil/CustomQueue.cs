using System;
using System.Text;
using HsaWebModule;

public class CustomQueue : CustomQueueBase
{

    public CustomQueue(bool console = false)
    {
        // defaultSize (20) stringQueue
        useConsole = console;
        if (useConsole) Program.WriteLog($"Set_CustomQueue_DataSize : {stringLength} bytes * [{defaultSize}]");
        customQueue = new StringBuilder(stringLength * defaultSize);
        curruntQueueSize = defaultSize;
    }

    public CustomQueue(int size, bool console = false)
    {
        // CustomSize (size) stringQueue
        useConsole = console;
        if (useConsole) Program.WriteLog($"Set CustomQueue__DataSize : {stringLength} bytes * [{size}]");
        customQueue = new StringBuilder(stringLength * size);
        curruntQueueSize = size;
    }
    public CustomQueue(int readSize, int size, bool console = false)
    {
        // CustomSize (size) stringQueue
        useConsole = console;
        stringLength = readSize;
        if (useConsole) Program.WriteLog($"Set_CustomQueue_DataSize : {stringLength} bytes * [{size}]");
        customQueue = new StringBuilder(stringLength * size);
        curruntQueueSize = size;
    }

    public override void Enqueue(string str)
    {
        if (customQueue != null)
        {
            InputString(str);
        }
    }

    public override string Dequeue()
    {
        if (GetString() == string.Empty) return null;
        else
        {
            return dequeueString;
        }
    }
}