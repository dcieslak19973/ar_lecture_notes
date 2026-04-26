using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class CourseService : ICourseService
{
    private const string Collection = "courses";
    private readonly IStorageProvider _storage;

    public CourseService(IStorageProvider storage) => _storage = storage;

    public Task<List<Course>> GetAllCoursesAsync() =>
        _storage.LoadAllAsync<Course>(Collection);

    public Task<Course> GetCourseAsync(string id) =>
        _storage.LoadAsync<Course>(Collection, id);

    public async Task<Course> CreateCourseAsync(string name, string instructor, string room, string schedule)
    {
        var course = new Course
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            Instructor = instructor,
            Room = room,
            Schedule = schedule,
            CreatedAt = DateTime.UtcNow
        };
        await _storage.SaveAsync(Collection, course.Id, course);
        return course;
    }

    public Task UpdateCourseAsync(Course course) =>
        _storage.SaveAsync(Collection, course.Id, course);

    public Task DeleteCourseAsync(string id) =>
        _storage.DeleteAsync(Collection, id);
}
