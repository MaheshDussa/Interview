using WebApplication1.Models.DTOs;

namespace WebApplication1.Services;

public interface ITaskService
{
    Task<IEnumerable<TaskDto>> GetUserTasksAsync(int userId);
    Task<TaskDto?> GetTaskByIdAsync(int taskId, int userId);
    Task<TaskDto> CreateTaskAsync(int userId, CreateTaskRequest request);
    Task<TaskDto?> UpdateTaskAsync(int taskId, int userId, UpdateTaskRequest request);
    Task<bool> DeleteTaskAsync(int taskId, int userId);
}
