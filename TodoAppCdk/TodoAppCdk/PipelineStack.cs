// ============================================================================
// CDK IMPORTS - CI/CD Pipeline Infrastructure Components
// ============================================================================
using Amazon.CDK;                           // Core CDK framework
using Amazon.CDK.AWS.CodeBuild;             // CodeBuild projects for build/deploy
using Amazon.CDK.AWS.CodePipeline;          // CodePipeline orchestration
using Amazon.CDK.AWS.CodePipeline.Actions;  // Pipeline actions (source, build, deploy, approval)
using Amazon.CDK.AWS.IAM;                   // IAM roles and permissions
using Amazon.CDK.AWS.S3;                    // S3 bucket for pipeline artifacts
using Amazon.CDK.AWS.SNS;                   // SNS notifications for approvals
using Amazon.CDK.AWS.SNS.Subscriptions;    // Email subscriptions for notifications
using Constructs;                          // Base construct framework
using System;                              // System namespace for Environment
using System.Collections.Generic;          // .NET collections for build specs

namespace TodoAppCdk
{
    /// <summary>
    /// CI/CD Pipeline Stack - Complete automated deployment pipeline
    /// 
    /// PIPELINE ARCHITECTURE:
    /// 1. SOURCE: GitHub repository via CodeStar connection
    /// 2. BUILD: .NET 8 compilation and packaging
    /// 3. MANUAL REVIEW: Human approval gate with email notifications
    /// 4. DEPLOY: CDK deployment with L3 constructs
    /// 
    /// FEATURES:
    /// - Automated GitHub webhook triggers
    /// - .NET 8 build environment
    /// - Manual approval with detailed review information
    /// - CDK asset bundling for Lambda deployment
    /// - SNS email notifications
    /// - Comprehensive error handling and logging
    /// </summary>
    public class PipelineStack : Stack
    {
        internal PipelineStack(Construct scope, string id, IStackProps props) : base(scope, id, props)
        {
            // ================================================================
            // PIPELINE ARTIFACTS STORAGE - S3 Bucket for build artifacts
            // ================================================================
            // Creates versioned S3 bucket to store pipeline artifacts between stages
            var artifactsBucket = new Bucket(this, "PipelineArtifacts", new BucketProps
            {
                BucketName = $"todolist-pipeline-artifacts-{Account}",        // Unique bucket name per AWS account
                Versioned = true,                                             // Enable versioning for artifact history
                RemovalPolicy = RemovalPolicy.DESTROY,                       // Allow bucket deletion with stack
                AutoDeleteObjects = true                                      // Automatically clean up objects on deletion
            });

            // ================================================================
            // NOTIFICATION SYSTEM - SNS Topic for manual approval alerts
            // ================================================================
            // Creates SNS topic to send email notifications for manual approvals
            var approvalTopic = new Topic(this, "ApprovalNotifications", new TopicProps
            {
                TopicName = "TodoList-Manual-Approval",                      // Topic name in AWS console
                DisplayName = "TodoList Manual Approval Notifications"       // Human-readable display name
            });

            // ================================================================
            // EMAIL SUBSCRIPTION - Notify reviewer via email
            // ================================================================
            // Add email subscription for manual approval notifications
            // When pipeline reaches manual approval stage, email is sent to reviewer
            var approvalEmail = this.Node.TryGetContext("approval-email")?.ToString()
                ?? System.Environment.GetEnvironmentVariable("APPROVAL_EMAIL")
                ?? throw new ArgumentException("Approval email must be provided via context or environment variable");
            approvalTopic.AddSubscription(new EmailSubscription(approvalEmail));

            // ================================================================
            // BUILD PROJECT - .NET 8 Application Compilation
            // ================================================================
            // Creates CodeBuild project to compile and package .NET 8 backend
            var buildProject = new Project(this, "TodoListBuild", new ProjectProps
            {
                ProjectName = "TodoList-Build",                               // Project name in CodeBuild console
                Description = "Build and package TodoList backend",          // Project description
                Environment = new BuildEnvironment                           // Build environment configuration
                {
                    BuildImage = LinuxBuildImage.AMAZON_LINUX_2_5,          // Amazon Linux 2 with .NET support
                    ComputeType = ComputeType.SMALL                          // Small compute instance (cost-effective)
                },
                // ============================================================
                // BUILD SPECIFICATION - Defines build phases and commands
                // ============================================================
                BuildSpec = BuildSpec.FromObject(new Dictionary<string, object>
                {
                    ["version"] = "0.2",                                     // BuildSpec version
                    ["phases"] = new Dictionary<string, object>              // Build phases definition
                    {
                        // ====================================================
                        // INSTALL PHASE - Setup build environment
                        // ====================================================
                        ["install"] = new Dictionary<string, object>
                        {
                            ["runtime-versions"] = new Dictionary<string, string>
                            {
                                ["dotnet"] = "8.0"                          // Install .NET 8 runtime
                            }
                        },
                        // ====================================================
                        // PRE-BUILD PHASE - Prepare for compilation
                        // ====================================================
                        ["pre_build"] = new Dictionary<string, object>
                        {
                            ["commands"] = new[]
                            {
                                "echo Build started on `date`",             // Log build start time
                                "cd Backend",                               // Navigate to backend source
                                "dotnet restore"                            // Restore NuGet packages
                            }
                        },
                        // ====================================================
                        // BUILD PHASE - Compile .NET application
                        // ====================================================
                        ["build"] = new Dictionary<string, object>
                        {
                            ["commands"] = new[]
                            {
                                "echo Building .NET application",           // Log build phase start
                                "dotnet build --configuration Release",    // Build in Release mode
                                "dotnet publish -c Release -f net8.0",     // Publish for .NET 8 target
                                "echo .NET build completed - Backend source preserved for CDK bundling", // Log completion
                                "cd ..",                                    // Return to root directory
                                "echo Verifying Backend directory structure for artifacts:", // Verification
                                "ls -la Backend/"                          // List backend contents
                            }
                        },
                        // ====================================================
                        // POST-BUILD PHASE - Verify build artifacts
                        // ====================================================
                        ["post_build"] = new Dictionary<string, object>
                        {
                            ["commands"] = new[]
                            {
                                "echo Build completed on `date`",           // Log completion time
                                "echo Backend source ready for CDK bundling", // Confirm CDK readiness
                                "echo Artifact structure:",                 // Log artifact structure
                                "find . -name 'Backend' -type d",          // Find Backend directory
                                "ls -la Backend/ || echo 'Backend directory not found at root level'" // Verify structure
                            }
                        }
                    },
                    // ========================================================
                    // ARTIFACTS - Define build output artifacts
                    // ========================================================
                    ["artifacts"] = new Dictionary<string, object>
                    {
                        ["files"] = new[]                                   // Include all files in artifact
                        {
                            "**/*"                                          // Recursive include all files
                        },
                        ["name"] = "BuildArtifacts",                        // Artifact name
                        ["base-directory"] = "."                           // Base directory for artifacts
                    }
                })
            });

            // ================================================================
            // DEPLOYMENT PROJECT - CDK Infrastructure Deployment
            // ================================================================
            // Creates CodeBuild project to deploy infrastructure using CDK
            // This project handles the final deployment of L3 constructs to AWS
            var deployProject = new Project(this, "TodoListDeploy", new ProjectProps
            {
                ProjectName = "TodoList-Deploy",                             // Project name in CodeBuild console
                Description = "Deploy TodoList infrastructure using CDK",    // Project description
                Environment = new BuildEnvironment                           // Deployment environment configuration
                {
                    BuildImage = LinuxBuildImage.AMAZON_LINUX_2_5,          // Amazon Linux 2 with CDK support
                    ComputeType = ComputeType.SMALL,                        // Small compute instance (cost-effective)
                    Privileged = true                                       // Required for Docker operations in CDK bundling
                },
                // ============================================================
                // DEPLOYMENT BUILD SPECIFICATION - CDK deployment phases
                // ============================================================
                BuildSpec = BuildSpec.FromObject(new Dictionary<string, object>
                {
                    ["version"] = "0.2",                                   // BuildSpec version
                    ["phases"] = new Dictionary<string, object>            // Deployment phases definition
                    {
                        // ====================================================
                        // INSTALL PHASE - Setup deployment environment
                        // ====================================================
                        ["install"] = new Dictionary<string, object>
                        {
                            ["runtime-versions"] = new Dictionary<string, string>
                            {
                                ["dotnet"] = "8.0",                        // .NET 8 for CDK project compilation
                                ["nodejs"] = "20"                         // Node.js 20 for CDK CLI (latest LTS)
                            },
                            ["commands"] = new[]
                            {
                                "npm install -g aws-cdk@latest"           // Install latest CDK CLI globally
                            }
                        },
                        // ====================================================
                        // PRE-BUILD PHASE - Prepare for CDK deployment
                        // ====================================================
                        ["pre_build"] = new Dictionary<string, object>
                        {
                            ["commands"] = new[]
                            {
                                "echo CDK deployment started on `date`",    // Log deployment start time
                                "echo Using CDK asset bundling for Lambda deployment", // CDK bundling explanation
                                "echo Verifying complete artifact structure:", // Artifact structure verification
                                "pwd",                                       // Show current working directory
                                "echo 'CODEBUILD_SRC_DIR: $CODEBUILD_SRC_DIR'", // CodeBuild environment variable
                                "echo 'Root directory contents:'",          // Root directory listing
                                "ls -la",                                    // List all files in root
                                "echo 'Looking for Backend directory:'",    // Backend directory search
                                "find . -name 'Backend' -type d",          // Find Backend directory recursively
                                "echo 'Backend directory contents (if exists):'", // Backend contents verification
                                "ls -la Backend/ || echo 'Backend directory not found at root level'", // List Backend or show error
                                "echo 'TodoAppCdk directory contents:'",    // CDK directory verification
                                "ls -la TodoAppCdk/ || echo 'TodoAppCdk directory not found'", // List CDK directory
                                "echo 'Navigating to CDK project directory'", // Navigation explanation
                                "cd TodoAppCdk/TodoAppCdk",                 // Navigate to CDK project root
                                "echo Current directory: $(pwd)",           // Show current path after navigation
                                "echo 'Checking relative path to Backend from CDK directory:'", // Path verification
                                "ls -la ../../Backend/ || echo 'Backend not found at ../../Backend'", // Check relative path
                                "ls -la ../Backend/ || echo 'Backend not found at ../Backend'", // Check alternative path
                                "echo 'Restoring CDK project dependencies'", // Dependency restoration explanation
                                "dotnet restore"                            // Restore .NET packages for CDK project
                            }
                        },
                        // ====================================================
                        // BUILD PHASE - CDK synthesis and deployment
                        // ====================================================
                        ["build"] = new Dictionary<string, object>
                        {
                            ["commands"] = new[]
                            {
                                "echo Synthesizing CDK templates",          // CDK synthesis start
                                "echo 'Current directory before CDK operations: $(pwd)'", // Directory verification
                                "echo 'Verifying Backend directory is accessible:'", // Backend accessibility check
                                "ls -la ../../Backend/ || echo 'Backend not accessible at ../../Backend'", // Verify Backend access
                                "echo 'Moving to CDK root directory (where cdk.json is located)'", // Navigation explanation
                                "cd ..",                                    // Move to CDK root (where cdk.json exists)
                                "echo 'Current directory: $(pwd)'",        // Show current directory after move
                                "ls -la cdk.json || echo 'cdk.json not found'", // Verify CDK configuration file
                                "cdk synth",                                // Synthesize CloudFormation templates from CDK
                                "echo Deploying TodoList application",     // Deployment start message
                                "cdk deploy TodoAppStack --require-approval never --outputs-file outputs.json" // Deploy L3 constructs without manual approval
                            }
                        },
                        // ====================================================
                        // POST-BUILD PHASE - Verify deployment results
                        // ====================================================
                        ["post_build"] = new Dictionary<string, object>
                        {
                            ["commands"] = new[]
                            {
                                "echo Deployment completed on `date`",      // Log deployment completion time
                                "echo 'Current directory: $(pwd)'",        // Show current directory
                                "echo 'Looking for outputs file:'",        // Output file search explanation
                                "find . -name 'outputs.json' -type f",     // Find CDK deployment outputs file
                                "echo 'Deployment outputs:'",              // Display deployment outputs
                                "cat outputs.json || echo 'No outputs file found in current directory'", // Show outputs or error
                                "cat ../outputs.json || echo 'No outputs file found in parent directory'" // Check alternative location
                            }
                        }
                    },
                    // ========================================================
                    // DEPLOYMENT ARTIFACTS - CDK outputs and results
                    // ========================================================
                    ["artifacts"] = new Dictionary<string, object>
                    {
                        ["files"] = new[]                                   // Include deployment output files
                        {
                            "outputs.json",                                 // CDK deployment outputs (primary location)
                            "../outputs.json"                              // CDK deployment outputs (alternative location)
                        }
                    }
                })
            });

            // ================================================================
            // IAM PERMISSIONS - Grant comprehensive deployment permissions
            // ================================================================
            // Grant necessary permissions to CodeBuild deploy project for CDK operations
            // These permissions allow CodeBuild to create/modify AWS resources via CDK
            deployProject.AddToRolePolicy(new PolicyStatement(new PolicyStatementProps
            {
                Effect = Effect.ALLOW,                                      // Allow these actions
                Actions = new[]                                             // Required AWS API actions for CDK deployment
                {
                    "cloudformation:*",                                     // CloudFormation operations (CDK backend)
                    "iam:*",                                               // IAM roles, policies, and permissions
                    "lambda:*",                                            // Lambda function management and configuration
                    "apigateway:*",                                        // API Gateway creation and configuration
                    "dynamodb:*",                                          // DynamoDB table operations
                    "s3:*",                                                // S3 bucket operations and website hosting
                    "sts:AssumeRole",                                      // Role assumption for cross-service access
                    "logs:*"                                               // CloudWatch Logs access for monitoring
                },
                Resources = new[] { "*" }                                  // Apply to all resources (broad permissions required for CDK)
            }));

            // ================================================================
            // PIPELINE ARTIFACTS - Define artifact flow between stages
            // ================================================================
            // Artifacts represent the data passed between pipeline stages
            var sourceOutput = new Artifact_("SourceOutput");              // GitHub source code artifact
            var buildOutput = new Artifact_("BuildOutput");                // Compiled application artifact

            // ================================================================
            // CODEPIPELINE DEFINITION - Complete CI/CD workflow orchestration
            // ================================================================
            // Creates the main CI/CD pipeline with 4 stages: Source → Build → Manual Review → Deploy
            var pipeline = new Pipeline(this, "TodoListPipeline", new PipelineProps
            {
                PipelineName = "TodoList-CI-CD-Pipeline",                   // Pipeline name in AWS console
                ArtifactBucket = artifactsBucket,                           // S3 bucket for storing artifacts between stages
                Stages = new[]                                              // Pipeline stages definition (executed sequentially)
                {
                    // ============================================================
                    // STAGE 1: SOURCE - GitHub Repository Integration
                    // ============================================================
                    // Automatically triggered by GitHub webhooks on commits to main branch
                    new Amazon.CDK.AWS.CodePipeline.StageProps
                    {
                        StageName = "Source",                               // Stage name displayed in AWS console
                        Actions = new[]
                        {
                            // GitHub source action using CodeStar connection for secure access
                            new CodeStarConnectionsSourceAction(new CodeStarConnectionsSourceActionProps
                            {
                                ActionName = "GitHub_Source",               // Action name in pipeline
                                Owner = "michlin0825",                      // GitHub username/organization
                                Repo = "TodoListApp_Lambda",                // Repository name
                                Branch = "main",                            // Source branch to monitor
                                ConnectionArn = this.Node.TryGetContext("codestar-connection-arn")?.ToString()
                                    ?? System.Environment.GetEnvironmentVariable("CODESTAR_CONNECTION_ARN")
                                    ?? throw new ArgumentException("CodeStar connection ARN must be provided via context or environment variable"), // CodeStar connection ARN
                                Output = sourceOutput                       // Output artifact for next stage
                            })
                        }
                    },

                    // ============================================================
                    // STAGE 2: BUILD - .NET Application Compilation
                    // ============================================================
                    // Compiles .NET 8 backend and prepares artifacts for deployment
                    new Amazon.CDK.AWS.CodePipeline.StageProps
                    {
                        StageName = "Build",                                // Stage name displayed in AWS console
                        Actions = new[]
                        {
                            // CodeBuild action for .NET 8 compilation and packaging
                            new CodeBuildAction(new CodeBuildActionProps
                            {
                                ActionName = "Build_Application",           // Action name in pipeline
                                Project = buildProject,                    // Reference to build project defined above
                                Input = sourceOutput,                      // Input artifact from source stage
                                Outputs = new[] { buildOutput }           // Output artifact for next stage
                            })
                        }
                    },

                    // ============================================================
                    // STAGE 3: MANUAL REVIEW - Human approval gate with notifications
                    // ============================================================
                    // Provides human oversight before production deployment
                    new Amazon.CDK.AWS.CodePipeline.StageProps
                    {
                        StageName = "Manual_Review",                        // Stage name displayed in AWS console
                        Actions = new[]
                        {
                            // Manual approval action with comprehensive review information
                            new ManualApprovalAction(new ManualApprovalActionProps
                            {
                                ActionName = "Review_and_Approve",          // Action name in pipeline
                                AdditionalInformation = @"                  // Detailed review information for approver
🔍 **Manual Review Required**

Please review the following before approving:
• Code changes in the GitHub repository
• Build artifacts and logs
• Infrastructure changes in CDK templates

**Current Application URLs:**
• Frontend: http://todo-app-frontend-cloudchef01-us-cdk.s3-website-us-east-1.amazonaws.com
• API: https://g3zokr6lo4.execute-api.us-east-1.amazonaws.com/prod/api/todos

**Review Checklist:**
✅ Code changes reviewed
✅ No security vulnerabilities
✅ Infrastructure changes validated
✅ Ready for deployment

Click 'Approve' to proceed with deployment or 'Reject' to stop the pipeline.
                                ",
                                NotificationTopic = approvalTopic           // SNS topic for email notifications
                            })
                        }
                    },

                    // ============================================================
                    // STAGE 4: DEPLOY - CDK Infrastructure Deployment
                    // ============================================================
                    // Deploys L3 CDK constructs to AWS using automated CDK deployment
                    new Amazon.CDK.AWS.CodePipeline.StageProps
                    {
                        StageName = "Deploy",                               // Stage name displayed in AWS console
                        Actions = new[]
                        {
                            // CodeBuild action for CDK deployment with L3 constructs
                            new CodeBuildAction(new CodeBuildActionProps
                            {
                                ActionName = "Deploy_Infrastructure",       // Action name in pipeline
                                Project = deployProject,                   // Reference to deploy project defined above
                                Input = buildOutput                        // Input artifact from build stage
                            })
                        }
                    }
                }
            });

            // ================================================================
            // CLOUDFORMATION OUTPUTS - Export pipeline information for external access
            // ================================================================
            
            // Export pipeline name for cross-stack references and external tools
            new CfnOutput(this, "PipelineName", new CfnOutputProps
            {
                Description = "Name of the CodePipeline",                   // Output description in CloudFormation
                Value = pipeline.PipelineName,                             // Actual pipeline name value
                ExportName = "TodoList-PipelineName"                       // Cross-stack export name for referencing
            });

            // Export pipeline console URL for easy access and monitoring
            new CfnOutput(this, "PipelineUrl", new CfnOutputProps
            {
                Description = "URL of the CodePipeline in AWS Console",     // Output description
                Value = $"https://console.aws.amazon.com/codesuite/codepipeline/pipelines/{pipeline.PipelineName}/view", // Direct console URL
                ExportName = "TodoList-PipelineUrl"                        // Cross-stack export name
            });

            // Export SNS topic ARN for notification management and integration
            new CfnOutput(this, "ApprovalTopicArn", new CfnOutputProps
            {
                Description = "SNS Topic ARN for manual approvals",        // Output description
                Value = approvalTopic.TopicArn,                            // SNS topic ARN value
                ExportName = "TodoList-ApprovalTopicArn"                   // Cross-stack export name
            });
        }
    }
}
