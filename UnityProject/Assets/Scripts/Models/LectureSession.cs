using System;
using System.Collections.Generic;

[Serializable]
public class LectureSession
{
    public string Id;
    public string CourseId;
    public string Title;
    public DateTime StartTime;
    public DateTime EndTime;
    public bool IsActive;
    public string SummaryId;
    public List<string> NoteItemIds = new List<string>();
    public List<string> TagIds = new List<string>();
}
