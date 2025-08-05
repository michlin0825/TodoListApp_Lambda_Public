using Microsoft.AspNetCore.Mvc;
using Amazon.DynamoDBv2.DataModel;
using TodoApi.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace TodoApi.Controllers
{
    /// <summary>
    /// Controller for managing Todo items in DynamoDB
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class TodosController : ControllerBase
    {
        private readonly IDynamoDBContext _dynamoDbContext;
        private readonly ILogger<TodosController> _logger;

        /// <summary>
        /// Constructor for TodosController
        /// </summary>
        /// <param name="dynamoDbContext">DynamoDB context for database operations</param>
        /// <param name="logger">Logger for error handling</param>
        public TodosController(IDynamoDBContext dynamoDbContext, ILogger<TodosController> logger)
        {
            _dynamoDbContext = dynamoDbContext ?? throw new ArgumentNullException(nameof(dynamoDbContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Get all todo items
        /// </summary>
        /// <returns>List of todo items</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Todo>>> GetTodos()
        {
            try
            {
                _logger.LogInformation("Retrieving all todo items");
                var scan = _dynamoDbContext.ScanAsync<Todo>(null);
                var todos = await scan.GetRemainingAsync();
                return Ok(todos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving todos");
                return StatusCode(500, "An error occurred while retrieving the todo list");
            }
        }

        /// <summary>
        /// Get a specific todo item by ID
        /// </summary>
        /// <param name="id">Todo item ID</param>
        /// <returns>Todo item</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<Todo>> GetTodo(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                _logger.LogWarning("GetTodo called with null or empty ID");
                return BadRequest("ID cannot be empty");
            }

            try
            {
                _logger.LogInformation("Retrieving todo item with ID: {Id}", id);
                var todo = await _dynamoDbContext.LoadAsync<Todo>(id);

                if (todo == null)
                {
                    _logger.LogWarning("Todo item with ID {Id} not found", id);
                    return NotFound();
                }

                return todo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving todo with id {Id}", id);
                return StatusCode(500, "An error occurred while retrieving the todo item");
            }
        }

        /// <summary>
        /// Update an existing todo item
        /// </summary>
        /// <param name="id">Todo item ID</param>
        /// <param name="todo">Updated todo item</param>
        /// <returns>No content if successful</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> PutTodo(string id, Todo todo)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest("ID cannot be empty");
            }

            if (todo == null)
            {
                return BadRequest("Todo item cannot be null");
            }

            if (id != todo.Id)
            {
                return BadRequest("ID in URL does not match ID in todo item");
            }

            try
            {
                _logger.LogInformation("Updating todo item with ID: {Id}", id);
                await _dynamoDbContext.SaveAsync(todo);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating todo with id {Id}", id);
                return StatusCode(500, "An error occurred while updating the todo item");
            }
        }

        /// <summary>
        /// Create a new todo item
        /// </summary>
        /// <param name="todo">Todo item to create</param>
        /// <returns>Created todo item</returns>
        [HttpPost]
        public async Task<ActionResult<Todo>> PostTodo(Todo todo)
        {
            if (todo == null)
            {
                return BadRequest("Todo item cannot be null");
            }

            try
            {
                if (string.IsNullOrEmpty(todo.Id))
                {
                    todo.Id = Guid.NewGuid().ToString();
                }
                
                _logger.LogInformation("Creating new todo item with ID: {Id}", todo.Id);
                await _dynamoDbContext.SaveAsync(todo);
                return CreatedAtAction(nameof(GetTodo), new { id = todo.Id }, todo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating todo");
                return StatusCode(500, "An error occurred while creating the todo item");
            }
        }

        /// <summary>
        /// Delete a todo item
        /// </summary>
        /// <param name="id">Todo item ID</param>
        /// <returns>No content if successful</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTodo(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest("ID cannot be empty");
            }

            try
            {
                _logger.LogInformation("Deleting todo item with ID: {Id}", id);
                await _dynamoDbContext.DeleteAsync<Todo>(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting todo with id {Id}", id);
                return StatusCode(500, "An error occurred while deleting the todo item");
            }
        }

        /// <summary>
        /// Toggle the completion status of a todo item
        /// </summary>
        /// <param name="id">Todo item ID</param>
        /// <returns>No content if successful</returns>
        [HttpPatch("{id}/toggle")]
        public async Task<IActionResult> ToggleTodoStatus(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest("ID cannot be empty");
            }

            try
            {
                _logger.LogInformation("Toggling status for todo item with ID: {Id}", id);
                var todo = await _dynamoDbContext.LoadAsync<Todo>(id);
                
                if (todo == null)
                {
                    _logger.LogWarning("Todo item with ID {Id} not found", id);
                    return NotFound();
                }

                todo.IsCompleted = !todo.IsCompleted;
                await _dynamoDbContext.SaveAsync(todo);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while toggling status for todo with id {Id}", id);
                return StatusCode(500, "An error occurred while updating the todo status");
            }
        }
    }
}
