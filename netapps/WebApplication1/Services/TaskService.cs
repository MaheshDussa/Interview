using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models.DTOs;
using WebApplication1.Models.Entities;

namespace WebApplication1.Services;

public class TaskService : ITaskService
{
    private readonly LearningContext _context;

    public TaskService(LearningContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<TaskDto>> GetUserTasksAsync(int userId)
    {
        return await _context.Tasks
            .Where(t => t.UserId == userId)
            .Select(t => MapToDto(t))
            .ToListAsync();
    }

    public async Task<TaskDto?> GetTaskByIdAsync(int taskId, int userId)
    {
        var task = await _context.Tasks
            .FirstOrDefaultAsync(t => t.TaskId == taskId && t.UserId == userId);

        return task == null ? null : MapToDto(task);
    }

    public async Task<TaskDto> CreateTaskAsync(int userId, CreateTaskRequest request)
    {
        var task = new UserTask
        {
            UserId = userId,
            Title = request.Title,
            DueDate = request.DueDate,
            IsCompleted = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        return MapToDto(task);
    }

    public async Task<TaskDto?> UpdateTaskAsync(int taskId, int userId, UpdateTaskRequest request)
    {
        var task = await _context.Tasks
            .FirstOrDefaultAsync(t => t.TaskId == taskId && t.UserId == userId);

        if (task == null)
        {
            return null;
        }

        task.Title = request.Title;
        task.IsCompleted = request.IsCompleted;
        task.DueDate = request.DueDate;

        await _context.SaveChangesAsync();

        return MapToDto(task);
    }

    public async Task<bool> DeleteTaskAsync(int taskId, int userId)
    {
        var task = await _context.Tasks
            .FirstOrDefaultAsync(t => t.TaskId == taskId && t.UserId == userId);

        if (task == null)
        {
            return false;
        }

        _context.Tasks.Remove(task);
        await _context.SaveChangesAsync();

        return true;
    }

    private static TaskDto MapToDto(UserTask task)
    {
        return new TaskDto
        {
            TaskId = task.TaskId,
            UserId = task.UserId,
            Title = task.Title,
            IsCompleted = task.IsCompleted ?? false,
            DueDate = task.DueDate,
            CreatedAt = task.CreatedAt
        };
    }
}
