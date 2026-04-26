using System.Collections.Generic;
using System.Threading.Tasks;

public interface ISessionService
{
    Task<List<LectureSession>> GetSessionsForCourseAsync(string courseId);
    Task<LectureSession> GetSessionAsync(string id);
    Task<LectureSession> StartSessionAsync(string courseId, string title);
    Task EndSessionAsync(string sessionId);
    Task<NoteItem> AddNoteItemAsync(string sessionId, string content, NoteItemType type, MarkerType marker = MarkerType.None);
    Task<List<NoteItem>> GetNoteItemsAsync(string sessionId);
    Task<List<LectureSession>> SearchSessionsAsync(string query);
}
