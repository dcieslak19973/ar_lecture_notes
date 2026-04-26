using System;

[Serializable]
public class TranscriptSegment
{
    public string Id;
    public string SessionId;
    public string Text;
    public DateTime Timestamp;
    public float Confidence;
    public int SequenceIndex;
}
