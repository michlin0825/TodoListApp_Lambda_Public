#!/bin/bash

# Deploy TodoList CI/CD Pipeline
# This script deploys the CodePipeline infrastructure for automated deployments

set -e

echo "🚀 Deploying TodoList CI/CD Pipeline"
echo "===================================="

# Check if AWS CLI is configured
if ! aws sts get-caller-identity > /dev/null 2>&1; then
    echo "❌ AWS CLI not configured. Please run 'aws configure' first."
    exit 1
fi

# Get current AWS account and region
ACCOUNT_ID=$(aws sts get-caller-identity --query Account --output text)
REGION="us-east-1"

echo "📍 Deploying to Account: $ACCOUNT_ID"
echo "📍 Region: $REGION"

# Navigate to CDK directory
cd "$(dirname "$0")/../TodoAppCdk/TodoAppCdk"

echo ""
echo "📦 Installing CDK dependencies..."
dotnet restore

echo ""
echo "📋 Synthesizing CDK templates..."
cd ..
cdk synth --context deploy-pipeline=true

echo ""
echo "🚀 Deploying Pipeline Stack..."
cdk deploy TodoListPipelineStack --context deploy-pipeline=true --require-approval never

echo ""
echo "✅ Pipeline deployment completed!"
echo ""
echo "📧 **IMPORTANT**: Check your email (michael.tw.lin@gmail.com) and confirm the SNS subscription"
echo "   You'll receive an email titled 'AWS Notification - Subscription Confirmation'"
echo "   Click the 'Confirm subscription' link to enable approval notifications"
echo ""
echo "🔗 **Pipeline Features**:"
echo "   ✅ GitHub CodeConnections integration (secure, no tokens needed)"
echo "   ✅ Automated .NET 8 build process"
echo "   ✅ Manual approval with email notifications"
echo "   ✅ Automated CDK deployment"
echo "   ✅ Webhook-triggered on code changes"
echo ""

# Get pipeline URL
PIPELINE_NAME="TodoList-CI-CD-Pipeline"
PIPELINE_URL="https://console.aws.amazon.com/codesuite/codepipeline/pipelines/$PIPELINE_NAME/view?region=$REGION"

echo "🔗 **Pipeline URL**: $PIPELINE_URL"
echo ""
echo "🎉 Setup complete! Your CI/CD pipeline is ready and running."
echo ""
echo "📝 **Next Steps**:"
echo "   1. Confirm SNS email subscription"
echo "   2. Push code changes to trigger the pipeline"
echo "   3. Monitor pipeline execution in AWS Console"
echo "   4. Approve deployments via email notifications"
