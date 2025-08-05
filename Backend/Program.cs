using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Add CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .DisallowCredentials();
    });
});

// Configure DynamoDB
var dynamoDbConfig = new AmazonDynamoDBConfig
{
    RegionEndpoint = Amazon.RegionEndpoint.USEast1
};
var dynamoDbClient = new AmazonDynamoDBClient(dynamoDbConfig);
builder.Services.AddSingleton<IAmazonDynamoDB>(dynamoDbClient);
builder.Services.AddSingleton<IDynamoDBContext>(sp =>
{
    return new DynamoDBContext(sp.GetRequiredService<IAmazonDynamoDB>());
});

// Add Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// IMPORTANT: Use CORS before any other middleware
app.UseCors("AllowAll");

app.UseAuthorization();

app.MapControllers();

app.Run();
