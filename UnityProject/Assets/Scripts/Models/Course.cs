using System;
using System.Collections.Generic;

[Serializable]
public class Course
{
    public string Id;
    public string Name;
    public string Instructor;
    public string Room;
    public string Schedule;
    public List<string> TagIds = new List<string>();
    public DateTime CreatedAt;
}
