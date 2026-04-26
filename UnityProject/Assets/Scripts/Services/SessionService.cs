using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class SessionService : ISessionService
{
    private const string SessionCollection = "sessions";
    private const string NoteCollection = "notes";
    private readonly IStorageProvider _storage;

    public SessionService(IStorageProvider storage) => _storage = storage;

    public async Task<List<LectureSession>> GetSessionsForCourseAsync(string courseId)
    {
        var all = await _storage.LoadAllAsync<LectureSession>(SessionCollection);
        return all.FindAll(s => s.CourseId == courseId)
                  .OrderByDescending(s => s.StartTime)
                  .ToList();
    }

    public Task<LectureSession> GetSessionAsync(string id) =>
        _storage.LoadAsync<LectureSession>(SessionCollection, id);

    public async Task<LectureSession> StartSessionAsync(string courseId, string title)
    {
        var session = new LectureSession
        {
            Id = Guid.NewGuid().ToString(),
            CourseId = courseId,
            Title = string.IsNullOrEmpty(title) ? "Lecture " + DateTime.Now.ToString("MMM d, yyyy") : title,
            StartTime = DateTime.UtcNow,
            IsActive = true
        };
        await _storage.SaveAsync(SessionCollection, session.Id, session);
        return session;
    }

    public async Task EndSessionAsync(string sessionId)
    {
        var session = await _storage.LoadAsync<LectureSession>(SessionCollection, sessionId);
        if (session == null) return;
        session.EndTime = DateTime.UtcNow;
        session.IsActive = false;
        await _storage.SaveAsync(SessionCollection, session.Id, session);
    }

    public async Task<NoteItem> AddNoteItemAsync(string sessionId, string content, NoteItemType type, MarkerType marker = MarkerType.None)
    {
        var note = new NoteItem
        {
            Id = Guid.NewGuid().ToString(),
            SessionId = sessionId,
            Content = content,
            Type = type,
            Marker = marker,
            Timestamp = DateTime.UtcNow
        };
        await _storage.SaveAsync(NoteCollection, note.Id, note);

        var session = await _storage.LoadAsync<LectureSession>(SessionCollection, sessionId);
        if (session != null)
        {
            session.NoteItemIds.Add(note.Id);
            await _storage.SaveAsync(SessionCollection, session.Id, session);
        }
        return note;
    }

    public async Task<List<NoteItem>> GetNoteItemsAsync(string sessionId)
    {
        var all = await _storage.LoadAllAsync<NoteItem>(NoteCollection);
        return all.Where(n => n.SessionId == sessionId)
                  .OrderBy(n => n.Timestamp)
                  .ToList();
    }

    public async Task<List<LectureSession>> SearchSessionsAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return new List<LectureSession>();
        var lower = query.ToLowerInvariant();
        var sessions = await _storage.LoadAllAsync<LectureSession>(SessionCollection);
        var notes = await _storage.LoadAllAsync<NoteItem>(NoteCollection);
        var matchingSessions = new HashSet<string>(
            notes.Where(n => n.Content.ToLowerInvariant().Contains(lower))
                 .Select(n => n.SessionId));
        return sessions.Where(s =>
                s.Title.ToLowerInvariant().Contains(lower) || matchingSessions.Contains(s.Id))
            .OrderByDescending(s => s.StartTime)
            .ToList();
    }
}
