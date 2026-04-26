using System.Collections.Generic;
using System.Threading.Tasks;

public interface ICourseService
{
    Task<List<Course>> GetAllCoursesAsync();
    Task<Course> GetCourseAsync(string id);
    Task<Course> CreateCourseAsync(string name, string instructor, string room, string schedule);
    Task UpdateCourseAsync(Course course);
    Task DeleteCourseAsync(string id);
}
