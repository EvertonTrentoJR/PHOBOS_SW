using System.Collections.Generic;

public class TableRow
{
    public int ID { get; set; }
    public string Item { get; set; }
}

public class AppConfig
{ 
    public string Setup { get; set; }
    public string Loop { get; set; }
    public int LoopCount { get; set; }
    public string Timer { get; set; }
    public int TimerCount { get; set; }
    public string Port { get; set; }
    public string path { get; set; }
    public string filename { get; set; }
    public string LCRid { get; set; }
    public int freqStart { get; set; }
    public int freqEnd { get; set; }
    public int freqNpoints { get; set; }
    public string freqMethod { get; set; }
    public string measMode { get; set; }
    public int samples { get; set; }

    public List<TableRow> TableData { get; set; }
}

public class RootConfig
{
    public AppConfig AppConfig { get; set; }
}