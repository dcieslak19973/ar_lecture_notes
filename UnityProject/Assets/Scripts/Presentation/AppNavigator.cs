using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Central navigation hub. All scene transitions go through here
/// so we can add transition effects or guards in one place later.
/// </summary>
public static class AppNavigator
{
    public const string SceneCourseList    = "CourseList";
    public const string SceneLectureCapture = "LectureCapture";
    public const string SceneReview        = "Review";
    public const string SceneSettings      = "AppSettings";

    // State passed between scenes
    public static string CurrentCourseId   { get; private set; }
    public static string CurrentSessionId  { get; private set; }

    public static void GoToCourseList()
    {
        CurrentCourseId = null;
        CurrentSessionId = null;
        SceneManager.LoadScene(SceneCourseList);
    }

    public static void GoToLectureCapture(string courseId)
    {
        CurrentCourseId = courseId;
        SceneManager.LoadScene(SceneLectureCapture);
    }

    public static void GoToReview(string sessionId)
    {
        CurrentSessionId = sessionId;
        SceneManager.LoadScene(SceneReview);
    }

    public static void GoToSettings()
    {
        SceneManager.LoadScene(SceneSettings);
    }
}
