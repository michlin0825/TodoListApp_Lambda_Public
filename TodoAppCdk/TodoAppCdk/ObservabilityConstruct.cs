// ============================================================================
// CDK IMPORTS - Observability and Monitoring Components
// ============================================================================
using Amazon.CDK;                           // Core CDK framework
using Amazon.CDK.AWS.CloudWatch;            // CloudWatch metrics and alarms
using Amazon.CDK.AWS.CloudWatch.Actions;    // CloudWatch alarm actions
using Amazon.CDK.AWS.Lambda;                // Lambda function monitoring
using Amazon.CDK.AWS.APIGateway;            // API Gateway monitoring
using Amazon.CDK.AWS.DynamoDB;              // DynamoDB monitoring
using Amazon.CDK.AWS.SNS;                   // SNS notifications for alerts
using Amazon.CDK.AWS.SNS.Subscriptions;    // Email subscriptions for alerts
using Constructs;                          // Base construct framework
using System.Collections.Generic;          // .NET collections
using System.Linq;                         // LINQ for collections

namespace TodoAppCdk
{
    /// <summary>
    /// L3 Observability Construct - Comprehensive monitoring and alerting for serverless applications
    /// 
    /// BENEFITS OF L3 OBSERVABILITY CONSTRUCT:
    /// - Encapsulates monitoring best practices
    /// - Provides consistent alerting across applications
    /// - Automatic dashboard creation with key metrics
    /// - Built-in alarm thresholds based on AWS recommendations
    /// - Reduces monitoring setup from 100+ lines to ~10 lines
    /// - Reusable across multiple serverless applications
    /// </summary>
    public class ObservabilityConstruct : Construct
    {
        // ================================================================
        // PUBLIC PROPERTIES - Expose monitoring resources
        // ================================================================
        public Dashboard Dashboard { get; }                                  // CloudWatch dashboard
        public Topic AlertTopic { get; }                                     // SNS topic for alerts
        public List<Alarm> Alarms { get; }                                  // Collection of CloudWatch alarms

        /// <summary>
        /// Constructor - Creates comprehensive observability infrastructure
        /// </summary>
        /// <param name="scope">Parent construct (usually a Stack)</param>
        /// <param name="id">Unique identifier for this construct</param>
        /// <param name="props">Configuration properties for observability</param>
        public ObservabilityConstruct(Construct scope, string id, ObservabilityProps props) 
            : base(scope, id)
        {
            Alarms = new List<Alarm>();

            // ================================================================
            // ALERT NOTIFICATION SYSTEM - SNS Topic for monitoring alerts
            // ================================================================
            AlertTopic = new Topic(this, "AlertTopic", new TopicProps
            {
                TopicName = $"{props.ApplicationName}-Alerts",               // Topic name in AWS console
                DisplayName = $"{props.ApplicationName} Monitoring Alerts"   // Human-readable display name
            });

            // Add email subscriptions for alert notifications
            foreach (var email in props.AlertEmails ?? new[] { "admin@example.com" })
            {
                AlertTopic.AddSubscription(new EmailSubscription(email));
            }

            // ================================================================
            // LAMBDA FUNCTION MONITORING - Performance and error tracking
            // ================================================================
            if (props.LambdaFunction != null)
            {
                CreateLambdaAlarms(props.LambdaFunction, props.ApplicationName);
            }

            // ================================================================
            // API GATEWAY MONITORING - Request and latency tracking
            // ================================================================
            if (props.ApiGateway != null)
            {
                CreateApiGatewayAlarms(props.ApiGateway, props.ApplicationName);
            }

            // ================================================================
            // DYNAMODB MONITORING - Performance and throttling tracking
            // ================================================================
            if (props.DynamoDbTable != null)
            {
                CreateDynamoDbAlarms(props.DynamoDbTable, props.ApplicationName);
            }

            // ================================================================
            // CLOUDWATCH DASHBOARD - Centralized monitoring view
            // ================================================================
            Dashboard = new Dashboard(this, "MonitoringDashboard", new DashboardProps
            {
                DashboardName = $"{props.ApplicationName}-Monitoring",       // Dashboard name in CloudWatch
                Widgets = CreateDashboardWidgets(props)                     // Dynamic widget creation
            });

            // ================================================================
            // CLOUDFORMATION OUTPUTS - Export monitoring information
            // ================================================================
            new CfnOutput(this, "DashboardUrl", new CfnOutputProps
            {
                Value = $"https://console.aws.amazon.com/cloudwatch/home?region={Stack.Of(this).Region}#dashboards:name={Dashboard.DashboardName}",
                Description = $"CloudWatch Dashboard URL for {props.ApplicationName}",
                ExportName = $"{props.ApplicationName}-DashboardUrl"
            });

            new CfnOutput(this, "AlertTopicArn", new CfnOutputProps
            {
                Value = AlertTopic.TopicArn,
                Description = $"SNS Topic ARN for {props.ApplicationName} alerts",
                ExportName = $"{props.ApplicationName}-AlertTopicArn"
            });
        }

        /// <summary>
        /// Creates Lambda function monitoring alarms
        /// </summary>
        private void CreateLambdaAlarms(Function lambdaFunction, string applicationName)
        {
            // Lambda Error Rate Alarm
            var errorRateAlarm = new Alarm(this, "LambdaErrorRateAlarm", new AlarmProps
            {
                AlarmName = $"{applicationName}-Lambda-ErrorRate",
                AlarmDescription = "Lambda function error rate is too high",
                Metric = lambdaFunction.MetricErrors(new MetricOptions
                {
                    Statistic = "Sum",
                    Period = Duration.Minutes(5)
                }),
                Threshold = 5,                                               // Alert if more than 5 errors in 5 minutes
                EvaluationPeriods = 2,                                       // Require 2 consecutive periods
                ComparisonOperator = ComparisonOperator.GREATER_THAN_OR_EQUAL_TO_THRESHOLD,
                TreatMissingData = TreatMissingData.NOT_BREACHING
            });
            errorRateAlarm.AddAlarmAction(new SnsAction(AlertTopic));
            Alarms.Add(errorRateAlarm);

            // Lambda Duration Alarm
            var durationAlarm = new Alarm(this, "LambdaDurationAlarm", new AlarmProps
            {
                AlarmName = $"{applicationName}-Lambda-Duration",
                AlarmDescription = "Lambda function duration is too high",
                Metric = lambdaFunction.MetricDuration(new MetricOptions
                {
                    Statistic = "Average",
                    Period = Duration.Minutes(5)
                }),
                Threshold = 10000,                                           // Alert if average duration > 10 seconds
                EvaluationPeriods = 3,
                ComparisonOperator = ComparisonOperator.GREATER_THAN_THRESHOLD,
                TreatMissingData = TreatMissingData.NOT_BREACHING
            });
            durationAlarm.AddAlarmAction(new SnsAction(AlertTopic));
            Alarms.Add(durationAlarm);

            // Lambda Throttle Alarm
            var throttleAlarm = new Alarm(this, "LambdaThrottleAlarm", new AlarmProps
            {
                AlarmName = $"{applicationName}-Lambda-Throttles",
                AlarmDescription = "Lambda function is being throttled",
                Metric = lambdaFunction.MetricThrottles(new MetricOptions
                {
                    Statistic = "Sum",
                    Period = Duration.Minutes(5)
                }),
                Threshold = 1,                                               // Alert on any throttling
                EvaluationPeriods = 1,
                ComparisonOperator = ComparisonOperator.GREATER_THAN_OR_EQUAL_TO_THRESHOLD,
                TreatMissingData = TreatMissingData.NOT_BREACHING
            });
            throttleAlarm.AddAlarmAction(new SnsAction(AlertTopic));
            Alarms.Add(throttleAlarm);
        }

        /// <summary>
        /// Creates API Gateway monitoring alarms
        /// </summary>
        private void CreateApiGatewayAlarms(RestApi apiGateway, string applicationName)
        {
            // API Gateway 4XX Error Alarm
            var clientErrorAlarm = new Alarm(this, "ApiGateway4XXAlarm", new AlarmProps
            {
                AlarmName = $"{applicationName}-ApiGateway-4XXErrors",
                AlarmDescription = "API Gateway 4XX error rate is too high",
                Metric = apiGateway.MetricClientError(new MetricOptions
                {
                    Statistic = "Sum",
                    Period = Duration.Minutes(5)
                }),
                Threshold = 10,                                              // Alert if more than 10 client errors in 5 minutes
                EvaluationPeriods = 2,
                ComparisonOperator = ComparisonOperator.GREATER_THAN_THRESHOLD,
                TreatMissingData = TreatMissingData.NOT_BREACHING
            });
            clientErrorAlarm.AddAlarmAction(new SnsAction(AlertTopic));
            Alarms.Add(clientErrorAlarm);

            // API Gateway 5XX Error Alarm
            var serverErrorAlarm = new Alarm(this, "ApiGateway5XXAlarm", new AlarmProps
            {
                AlarmName = $"{applicationName}-ApiGateway-5XXErrors",
                AlarmDescription = "API Gateway 5XX error rate is too high",
                Metric = apiGateway.MetricServerError(new MetricOptions
                {
                    Statistic = "Sum",
                    Period = Duration.Minutes(5)
                }),
                Threshold = 5,                                               // Alert if more than 5 server errors in 5 minutes
                EvaluationPeriods = 1,
                ComparisonOperator = ComparisonOperator.GREATER_THAN_THRESHOLD,
                TreatMissingData = TreatMissingData.NOT_BREACHING
            });
            serverErrorAlarm.AddAlarmAction(new SnsAction(AlertTopic));
            Alarms.Add(serverErrorAlarm);

            // API Gateway Latency Alarm
            var latencyAlarm = new Alarm(this, "ApiGatewayLatencyAlarm", new AlarmProps
            {
                AlarmName = $"{applicationName}-ApiGateway-Latency",
                AlarmDescription = "API Gateway latency is too high",
                Metric = apiGateway.MetricLatency(new MetricOptions
                {
                    Statistic = "Average",
                    Period = Duration.Minutes(5)
                }),
                Threshold = 5000,                                            // Alert if average latency > 5 seconds
                EvaluationPeriods = 3,
                ComparisonOperator = ComparisonOperator.GREATER_THAN_THRESHOLD,
                TreatMissingData = TreatMissingData.NOT_BREACHING
            });
            latencyAlarm.AddAlarmAction(new SnsAction(AlertTopic));
            Alarms.Add(latencyAlarm);
        }

        /// <summary>
        /// Creates DynamoDB monitoring alarms
        /// </summary>
        private void CreateDynamoDbAlarms(Table dynamoDbTable, string applicationName)
        {
            // DynamoDB Read Throttle Alarm
            var readThrottleAlarm = new Alarm(this, "DynamoDbReadThrottleAlarm", new AlarmProps
            {
                AlarmName = $"{applicationName}-DynamoDB-ReadThrottles",
                AlarmDescription = "DynamoDB read requests are being throttled",
                Metric = dynamoDbTable.MetricThrottledRequestsForOperations(new OperationsMetricOptions
                {
                    Operations = new[] { Operation.GET_ITEM, Operation.QUERY, Operation.SCAN },
                    Statistic = "Sum",
                    Period = Duration.Minutes(5)
                }),
                Threshold = 1,                                               // Alert on any read throttling
                EvaluationPeriods = 1,
                ComparisonOperator = ComparisonOperator.GREATER_THAN_OR_EQUAL_TO_THRESHOLD,
                TreatMissingData = TreatMissingData.NOT_BREACHING
            });
            readThrottleAlarm.AddAlarmAction(new SnsAction(AlertTopic));
            Alarms.Add(readThrottleAlarm);

            // DynamoDB Write Throttle Alarm
            var writeThrottleAlarm = new Alarm(this, "DynamoDbWriteThrottleAlarm", new AlarmProps
            {
                AlarmName = $"{applicationName}-DynamoDB-WriteThrottles",
                AlarmDescription = "DynamoDB write requests are being throttled",
                Metric = dynamoDbTable.MetricThrottledRequestsForOperations(new OperationsMetricOptions
                {
                    Operations = new[] { Operation.PUT_ITEM, Operation.UPDATE_ITEM, Operation.DELETE_ITEM },
                    Statistic = "Sum",
                    Period = Duration.Minutes(5)
                }),
                Threshold = 1,                                               // Alert on any write throttling
                EvaluationPeriods = 1,
                ComparisonOperator = ComparisonOperator.GREATER_THAN_OR_EQUAL_TO_THRESHOLD,
                TreatMissingData = TreatMissingData.NOT_BREACHING
            });
            writeThrottleAlarm.AddAlarmAction(new SnsAction(AlertTopic));
            Alarms.Add(writeThrottleAlarm);
        }

        /// <summary>
        /// Creates dashboard widgets for monitoring
        /// </summary>
        private IWidget[][] CreateDashboardWidgets(ObservabilityProps props)
        {
            var widgets = new List<List<IWidget>>();

            // First row - Lambda metrics
            if (props.LambdaFunction != null)
            {
                widgets.Add(new List<IWidget>
                {
                    new GraphWidget(new GraphWidgetProps
                    {
                        Title = "Lambda Invocations",
                        Left = new[] { props.LambdaFunction.MetricInvocations() },
                        Width = 12,
                        Height = 6
                    }),
                    new GraphWidget(new GraphWidgetProps
                    {
                        Title = "Lambda Errors",
                        Left = new[] { props.LambdaFunction.MetricErrors() },
                        Width = 12,
                        Height = 6
                    })
                });

                widgets.Add(new List<IWidget>
                {
                    new GraphWidget(new GraphWidgetProps
                    {
                        Title = "Lambda Duration",
                        Left = new[] { props.LambdaFunction.MetricDuration() },
                        Width = 24,
                        Height = 6
                    })
                });
            }

            // Second row - API Gateway metrics
            if (props.ApiGateway != null)
            {
                widgets.Add(new List<IWidget>
                {
                    new GraphWidget(new GraphWidgetProps
                    {
                        Title = "API Gateway Requests",
                        Left = new[] { props.ApiGateway.MetricCount() },
                        Width = 8,
                        Height = 6
                    }),
                    new GraphWidget(new GraphWidgetProps
                    {
                        Title = "API Gateway 4XX Errors",
                        Left = new[] { props.ApiGateway.MetricClientError() },
                        Width = 8,
                        Height = 6
                    }),
                    new GraphWidget(new GraphWidgetProps
                    {
                        Title = "API Gateway 5XX Errors",
                        Left = new[] { props.ApiGateway.MetricServerError() },
                        Width = 8,
                        Height = 6
                    })
                });
            }

            // Third row - DynamoDB metrics
            if (props.DynamoDbTable != null)
            {
                widgets.Add(new List<IWidget>
                {
                    new GraphWidget(new GraphWidgetProps
                    {
                        Title = "DynamoDB Read Capacity",
                        Left = new[] { props.DynamoDbTable.MetricConsumedReadCapacityUnits() },
                        Width = 12,
                        Height = 6
                    }),
                    new GraphWidget(new GraphWidgetProps
                    {
                        Title = "DynamoDB Write Capacity",
                        Left = new[] { props.DynamoDbTable.MetricConsumedWriteCapacityUnits() },
                        Width = 12,
                        Height = 6
                    })
                });
            }

            return widgets.Select(row => row.ToArray()).ToArray();
        }
    }

    /// <summary>
    /// Configuration Properties for Observability L3 Construct
    /// </summary>
    public class ObservabilityProps
    {
        /// <summary>
        /// Application name for naming monitoring resources (REQUIRED)
        /// </summary>
        public string ApplicationName { get; set; } = null!;

        /// <summary>
        /// Email addresses for alert notifications (OPTIONAL)
        /// Default: ["admin@example.com"]
        /// </summary>
        public string[]? AlertEmails { get; set; }

        /// <summary>
        /// Lambda function to monitor (OPTIONAL)
        /// </summary>
        public Function? LambdaFunction { get; set; }

        /// <summary>
        /// API Gateway to monitor (OPTIONAL)
        /// </summary>
        public RestApi? ApiGateway { get; set; }

        /// <summary>
        /// DynamoDB table to monitor (OPTIONAL)
        /// </summary>
        public Table? DynamoDbTable { get; set; }
    }
}
