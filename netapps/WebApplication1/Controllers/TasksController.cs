using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Models.DTOs;
using WebApplication1.Services;

namespace WebApplication1.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TasksController : ControllerBase
{
    private readonly ITaskService _taskService;

    public TasksController(ITaskService taskService)
    {
        _taskService = taskService;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.Parse(userIdClaim!);
    }

    [HttpGet]
    public async Task<IActionResult> GetTasks()
    {
        var userId = GetCurrentUserId();
        var tasks = await _taskService.GetUserTasksAsync(userId);
        return Ok(tasks);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetTask(int id)
    {
        var userId = GetCurrentUserId();
        var task = await _taskService.GetTaskByIdAsync(id, userId);

        if (task == null)
        {
            return NotFound(new { message = "Task not found" });
        }

        return Ok(task);
    }

    [HttpPost]
    public async Task<IActionResult> CreateTask([FromBody] CreateTaskRequest request)
    {
        var userId = GetCurrentUserId();
        var task = await _taskService.CreateTaskAsync(userId, request);
        return CreatedAtAction(nameof(GetTask), new { id = task.TaskId }, task);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTask(int id, [FromBody] UpdateTaskRequest request)
    {
        var userId = GetCurrentUserId();
        var task = await _taskService.UpdateTaskAsync(id, userId, request);

        if (task == null)
        {
            return NotFound(new { message = "Task not found" });
        }

        return Ok(task);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTask(int id)
    {
        var userId = GetCurrentUserId();
        var result = await _taskService.DeleteTaskAsync(id, userId);

        if (!result)
        {
            return NotFound(new { message = "Task not found" });
        }

        return NoContent();
    }
}
