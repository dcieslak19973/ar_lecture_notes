using System;

[Serializable]
public class ReviewCard
{
    public string Id;
    public string SessionId;
    public string Front;
    public string Back;
    public DateTime NextReviewDate;
    public int DifficultyRating; // 1-5
    public int ReviewCount;
}
