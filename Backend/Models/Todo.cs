using System;
using System.ComponentModel.DataAnnotations;
using Amazon.DynamoDBv2.DataModel;

namespace TodoApi.Models
{
    /// <summary>
    /// Represents a Todo item in the DynamoDB table
    /// </summary>
    [DynamoDBTable("TodoItemsCdk")]
    public class Todo
    {
        /// <summary>
        /// Unique identifier for the Todo item
        /// </summary>
        [DynamoDBHashKey]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// Description of the Todo item
        /// </summary>
        [Required(ErrorMessage = "Description is required")]
        [StringLength(100, ErrorMessage = "Description cannot be longer than 100 characters")]
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// Indicates whether the Todo item is completed
        /// </summary>
        public bool IsCompleted { get; set; }
        
        /// <summary>
        /// Date and time when the Todo item was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Date and time when the Todo item was last updated
        /// </summary>
        public DateTime? UpdatedAt { get; set; }
    }
}
