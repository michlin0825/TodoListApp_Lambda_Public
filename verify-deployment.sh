#!/bin/bash

# TodoList Application - Deployment Verification Script
# This script verifies that all components are working correctly

echo "🔍 TodoList Application - Deployment Verification"
echo "=================================================="

# Configuration
API_URL="https://g3zokr6lo4.execute-api.us-east-1.amazonaws.com/prod/api/todos"
FRONTEND_URL="http://<your-bucket-name>.s3-website-us-east-1.amazonaws.com"

echo ""
echo "📍 Testing API Endpoint..."
echo "URL: $API_URL"

# Test API endpoint
HTTP_STATUS=$(curl -s -o /dev/null -w "%{http_code}" "$API_URL")

if [ "$HTTP_STATUS" -eq 200 ]; then
    echo "✅ API Status: $HTTP_STATUS (OK)"
    
    # Get actual response
    echo ""
    echo "📋 Sample API Response:"
    curl -s "$API_URL" | head -c 200
    echo ""
    echo "..."
else
    echo "❌ API Status: $HTTP_STATUS (Error)"
fi

echo ""
echo "🌐 Testing Frontend Website..."
echo "URL: $FRONTEND_URL"

# Test frontend
FRONTEND_STATUS=$(curl -s -o /dev/null -w "%{http_code}" "$FRONTEND_URL")

if [ "$FRONTEND_STATUS" -eq 200 ]; then
    echo "✅ Frontend Status: $FRONTEND_STATUS (OK)"
else
    echo "❌ Frontend Status: $FRONTEND_STATUS (Error)"
fi

echo ""
echo "🔧 AWS Resources Check..."

# Check if AWS CLI is available
if command -v aws &> /dev/null; then
    echo "✅ AWS CLI is available"
    
    # Check current AWS account
    ACCOUNT_ID=$(aws sts get-caller-identity --query Account --output text 2>/dev/null)
    if [ "$ACCOUNT_ID" = "111735445051" ]; then
        echo "✅ Connected to correct AWS account: $ACCOUNT_ID (cloudchef01)"
    else
        echo "⚠️  Connected to AWS account: $ACCOUNT_ID (expected: 111735445051)"
    fi
    
    # Check key resources
    echo ""
    echo "📊 Resource Status:"
    
    # Lambda function
    if aws lambda get-function --function-name todo-api-lambda --region us-east-1 &>/dev/null; then
        echo "✅ Lambda function: todo-api-lambda"
    else
        echo "❌ Lambda function: todo-api-lambda (not found)"
    fi
    
    # DynamoDB table
    if aws dynamodb describe-table --table-name TodoItems --region us-east-1 &>/dev/null; then
        echo "✅ DynamoDB table: TodoItems"
    else
        echo "❌ DynamoDB table: TodoItems (not found)"
    fi
    
    # S3 bucket
    if aws s3api head-bucket --bucket todo-app-frontend-cloudchef01 --region us-east-1 &>/dev/null; then
        echo "✅ S3 bucket: todo-app-frontend-cloudchef01"
    else
        echo "❌ S3 bucket: todo-app-frontend-cloudchef01 (not found)"
    fi
    
else
    echo "⚠️  AWS CLI not available - skipping resource checks"
fi

echo ""
echo "📝 Summary:"
echo "==========="

if [ "$HTTP_STATUS" -eq 200 ] && [ "$FRONTEND_STATUS" -eq 200 ]; then
    echo "🎉 All systems operational!"
    echo ""
    echo "🔗 Quick Links:"
    echo "   Frontend: $FRONTEND_URL"
    echo "   API:      $API_URL"
    echo ""
    echo "💡 Try creating a todo item:"
    echo "   curl -X POST \"$API_URL\" \\"
    echo "     -H \"Content-Type: application/json\" \\"
    echo "     -d '{\"description\": \"Test todo from verification script\"}'"
else
    echo "⚠️  Some components may not be working correctly"
    echo "   Check the status messages above for details"
fi

echo ""
