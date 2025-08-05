using Amazon.DynamoDBv2.DataModel;
using TodoApi.Models;

namespace TodoApi.Data
{
    // This class is kept for compatibility but not used with DynamoDB
    public class TodoContext
    {
        public TodoContext()
        {
        }

        // This property is not used with DynamoDB
        public System.Collections.Generic.IEnumerable<Todo> Todos { get; set; } = new List<Todo>();
    }
}
