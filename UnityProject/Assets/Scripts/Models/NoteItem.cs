using System;
using System.Collections.Generic;

public enum NoteItemType { Bullet, Transcript, Marker }

public enum MarkerType { None, Important, ExamItem, Assignment }

[Serializable]
public class NoteItem
{
    public string Id;
    public string SessionId;
    public string Content;
    public NoteItemType Type;
    public MarkerType Marker;
    public DateTime Timestamp;
    public List<string> TagIds = new List<string>();
}
