#!/bin/bash

echo "Building and deploying Todo App with CDK..."

# Build the backend
cd /Users/mba/Desktop/TodoListApp_Lambda/Backend
echo "Building backend..."
dotnet publish -c Release

# Build and deploy the CDK project
cd /Users/mba/Desktop/TodoListApp_Lambda/TodoAppCdk
echo "Building CDK project..."
dotnet build TodoAppCdk

echo "Bootstrapping CDK environment..."
cdk bootstrap aws://111735445051/us-east-1 --profile CloudChef01

echo "Deploying CDK stack..."
cdk deploy --profile CloudChef01 --require-approval never

# Get the API URL from the CDK output (L3 upgraded)
API_URL=$(aws cloudformation describe-stacks --stack-name TodoAppStack --query "Stacks[0].Outputs[?OutputKey=='ApiUrl'].OutputValue" --output text --profile CloudChef01)
FRONTEND_URL=$(aws cloudformation describe-stacks --stack-name TodoAppStack --query "Stacks[0].Outputs[?OutputKey=='TodoFrontendWebsiteUrlCC1189AA'].OutputValue" --output text --profile CloudChef01)

# Update the config.js with the actual API URL
cd /Users/mba/Desktop/TodoListApp_Lambda/Frontend
echo "/**
 * Configuration for the Todo application (L3 Upgraded)
 */
const CONFIG = {
    API_URL: '$API_URL'
};" > js/config.js

# Upload the updated config.js to S3 (L3 bucket)
BUCKET_NAME="todo-app-frontend-cloudchef01-us-cdk-l3"
aws s3 cp js/config.js s3://$BUCKET_NAME/js/config.js --profile CloudChef01

echo "Deployment complete!"
echo "Frontend URL: $FRONTEND_URL"
echo "API URL: $API_URL"
