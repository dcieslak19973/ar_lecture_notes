using System;
using System.Collections.Generic;

[Serializable]
public class Summary
{
    public string Id;
    public string SessionId;
    public string CondensedText;
    public List<string> KeyTerms = new List<string>();
    public List<string> ActionItems = new List<string>();
    public DateTime GeneratedAt;
}
