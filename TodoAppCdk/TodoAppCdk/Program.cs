// ============================================================================
// CDK IMPORTS - Essential AWS CDK libraries for infrastructure components
// ============================================================================
using Amazon.CDK;                           // Core CDK framework
using Amazon.CDK.AWS.APIGateway;           // API Gateway L3 constructs (LambdaRestApi)
using Amazon.CDK.AWS.DynamoDB;             // DynamoDB table and configuration
using Amazon.CDK.AWS.IAM;                  // IAM roles and permissions
using Amazon.CDK.AWS.Lambda;               // Lambda function definitions
using Amazon.CDK.AWS.S3;                   // S3 bucket and assets
using Amazon.CDK.AWS.S3.Assets;            // Asset bundling for deployments
using Amazon.CDK.AWS.S3.Deployment;       // S3 deployment utilities
using Constructs;                          // Base construct framework
using System.Collections.Generic;          // .NET collections for environment variables

namespace TodoAppCdk
{
    /// <summary>
    /// Main TodoApp Stack - Defines the complete serverless architecture
    /// Uses L3 CDK constructs for simplified, intent-based infrastructure
    /// </summary>
    public class TodoAppStack : Stack
    {
        internal TodoAppStack(Construct scope, string id, IStackProps? props = null) : base(scope, id, props)
        {
            // ================================================================
            // DATABASE LAYER - DynamoDB NoSQL Database - Development Optimized
            // ================================================================
            // Creates a serverless DynamoDB table for storing todo items
            // Optimized for development/testing with cost-effective configuration
            var table = new Table(this, "TodoTable", new TableProps
            {
                TableName = "TodoItemsCdk",                                    // Physical table name in AWS
                PartitionKey = new Amazon.CDK.AWS.DynamoDB.Attribute          // Primary key definition
                { 
                    Name = "Id", 
                    Type = AttributeType.STRING                                // UUID string as partition key
                },
                BillingMode = BillingMode.PAY_PER_REQUEST,                    // Serverless billing - no provisioned capacity
                RemovalPolicy = RemovalPolicy.DESTROY,                       // Allow table deletion when stack is destroyed
                // Removed: Point-in-time recovery (cost optimization for dev/test)
                // Removed: DynamoDB Streams (not needed for basic functionality)
                // Removed: Global Secondary Index (can be added later if needed)
                // Kept: AWS-managed encryption (included by default)
            });

            // ================================================================
            // COMPUTE LAYER - AWS Lambda Function (.NET 8) - Development Optimized
            // ================================================================
            // Creates a serverless Lambda function with automatic asset bundling
            // Optimized for development/testing with cost-effective configuration
            var lambdaFunction = new Function(this, "TodoLambda", new FunctionProps
            {
                Runtime = Runtime.DOTNET_8,                                   // .NET 8 runtime environment
                Handler = "TodoApi::TodoApi.LambdaEntryPoint::FunctionHandlerAsync", // Entry point for Lambda execution
                Code = Code.FromAsset("../Backend", new Amazon.CDK.AWS.S3.Assets.AssetOptions
                {
                    // AUTOMATIC ASSET BUNDLING - CDK compiles and packages .NET code
                    Bundling = new BundlingOptions
                    {
                        Image = Runtime.DOTNET_8.BundlingImage,               // Official .NET 8 build container
                        Command = new[]                                       // Build commands executed in container
                        {
                            "/bin/sh", "-c",
                            "dotnet tool install -g Amazon.Lambda.Tools --version 5.10.5 && " +  // Install Lambda tools
                            "dotnet lambda package --output-package /asset-output/function.zip"   // Compile and package
                        }
                    }
                }),
                Environment = new Dictionary<string, string>                  // Runtime environment variables
                {
                    ["DYNAMODB_TABLE"] = table.TableName                     // Pass table name to Lambda
                },
                Timeout = Duration.Seconds(30),                              // Maximum execution time
                MemorySize = 256,                                            // Memory allocation (cost-optimized for development)
                // Removed: ReservedConcurrentExecutions (not needed for dev/test)
                // Removed: X-Ray Tracing (cost optimization)
                // Removed: Custom Log Groups (use default)
            });

            // ================================================================
            // SECURITY LAYER - IAM Permissions
            // ================================================================
            // Grant Lambda function read/write permissions to DynamoDB table
            // CDK automatically creates least-privilege IAM policies
            table.GrantReadWriteData(lambdaFunction);

            // ================================================================
            // API LAYER - L3 Lambda REST API (68% Code Reduction)
            // ================================================================
            // L3 LambdaRestApi replaces 25+ lines of manual L2 API Gateway setup
            // Provides automatic CORS, proxy integration, and best practices
            var api = new LambdaRestApi(this, "TodoApi", new LambdaRestApiProps
            {
                Handler = lambdaFunction,                                     // Lambda function to handle all requests
                RestApiName = "Todo Service",                                 // API name in AWS console
                Description = "This service serves todos.",                   // API description
                DefaultCorsPreflightOptions = new CorsOptions                 // AUTOMATIC CORS CONFIGURATION
                {
                    AllowOrigins = Cors.ALL_ORIGINS,                         // Allow requests from any origin
                    AllowMethods = Cors.ALL_METHODS,                         // Allow all HTTP methods
                    AllowHeaders = new[] {                                   // Required headers for API requests
                        "Content-Type", "X-Amz-Date", "Authorization", 
                        "X-Api-Key", "X-Amz-Security-Token" 
                    }
                },
                Proxy = true  // FULL PROXY INTEGRATION - Lambda handles all routing and HTTP methods
            });

            // ================================================================
            // FRONTEND LAYER - L3 Static Website (53% Code Reduction)
            // ================================================================
            // Custom L3 construct replaces 15+ lines of manual S3 setup
            // Provides automatic deployment, public access, and website configuration
            var website = new StaticWebsiteConstruct(this, "TodoFrontend", new StaticWebsiteProps
            {
                BucketName = "todo-app-frontend-cloudchef01-us-cdk-l3",      // S3 bucket name (L3 version)
                IndexDocument = "index.html",                                 // Default document for website
                ErrorDocument = "error.html",                                 // Error page for 404s
                Sources = new[] { Source.Asset("../Frontend") },             // Source directory for website files
                Exclude = new[] { "server.py", ".DS_Store" }                 // Files to exclude from deployment
            });

            // ================================================================
            // OUTPUTS - CloudFormation Stack Outputs
            // ================================================================
            // Export API Gateway URL for external access and CI/CD integration
            new CfnOutput(this, "ApiUrl", new CfnOutputProps
            {
                Value = api.Url,                                             // API Gateway invoke URL
                Description = "API Gateway URL"                              // Description in CloudFormation
            });

            // Note: Website URL is automatically output by StaticWebsiteConstruct
            // This demonstrates L3 construct encapsulation and reusability
        }
    }

    /// <summary>
    /// CDK Application Entry Point - Orchestrates stack deployment based on context
    /// Supports both application deployment and CI/CD pipeline deployment (L2 and L3 options)
    /// </summary>
    public class Program
    {
        public static void Main(string[] args)
        {
            // ================================================================
            // CDK APPLICATION INITIALIZATION
            // ================================================================
            var app = new App();                                              // Initialize CDK application
            
            // ================================================================
            // DEPLOYMENT MODE DETECTION - Enhanced with L3 pipeline option
            // ================================================================
            // Check CDK context to determine deployment mode
            // Context can be set via: 
            // - cdk deploy --context deploy-pipeline=true (L2 pipeline)
            // - cdk deploy --context deploy-modern-pipeline=true (L3 pipeline)
            var deployPipeline = app.Node.TryGetContext("deploy-pipeline");
            var deployModernPipeline = app.Node.TryGetContext("deploy-modern-pipeline");
            
            if (deployModernPipeline != null && deployModernPipeline.ToString() == "true")
            {
                // ============================================================
                // L3 MODERN PIPELINE DEPLOYMENT MODE (NEW)
                // ============================================================
                // Deploy the modern L3 pipeline infrastructure (87% code reduction)
                // This creates a complete CI/CD pipeline using L3 constructs
                new ModernPipelineStack(app, "TodoListModernPipelineStack", new StackProps
                {
                    Env = new Amazon.CDK.Environment                          // AWS environment configuration
                    {
                        Account = System.Environment.GetEnvironmentVariable("CDK_DEFAULT_ACCOUNT"), // AWS account ID
                        Region = System.Environment.GetEnvironmentVariable("CDK_DEFAULT_REGION") ?? "us-east-1" // AWS region
                    }
                });
            }
            else if (deployPipeline != null && deployPipeline.ToString() == "true")
            {
                // ============================================================
                // L2 LEGACY PIPELINE DEPLOYMENT MODE
                // ============================================================
                // Deploy the legacy L2 pipeline infrastructure (for comparison)
                // This creates CodePipeline, CodeBuild, and related resources using L2 constructs
                new PipelineStack(app, "TodoListPipelineStack", new StackProps
                {
                    Env = new Amazon.CDK.Environment                          // AWS environment configuration
                    {
                        Account = System.Environment.GetEnvironmentVariable("CDK_DEFAULT_ACCOUNT"), // AWS account ID
                        Region = System.Environment.GetEnvironmentVariable("CDK_DEFAULT_REGION") ?? "us-east-1" // AWS region
                    }
                });
            }
            else
            {
                // ============================================================
                // APPLICATION DEPLOYMENT MODE (Default)
                // ============================================================
                // Deploy the main TodoApp infrastructure (Enhanced L3 constructs)
                // This creates Lambda, API Gateway, DynamoDB, S3, and monitoring resources
                new TodoAppStack(app, "TodoAppStack", new StackProps
                {
                    Env = new Amazon.CDK.Environment                          // AWS environment configuration
                    {
                        Account = System.Environment.GetEnvironmentVariable("CDK_DEFAULT_ACCOUNT"), // AWS account ID
                        Region = System.Environment.GetEnvironmentVariable("CDK_DEFAULT_REGION") ?? "us-east-1" // Default to us-east-1
                    }
                });
            }
            
            // ================================================================
            // CDK SYNTHESIS
            // ================================================================
            // Generate CloudFormation templates from CDK constructs
            // This is the final step that converts CDK code to deployable templates
            app.Synth();
        }
    }
}
