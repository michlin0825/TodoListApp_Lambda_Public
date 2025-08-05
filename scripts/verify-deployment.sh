#!/bin/bash

# TodoList Application Deployment Verification Script
# This script verifies that all components of the TodoList application are working correctly

echo "🔍 TodoList Application Deployment Verification"
echo "=============================================="
echo ""

# Colors for output
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# URLs
FRONTEND_URL="http://todo-app-frontend-cloudchef01-us-cdk.s3-website-us-east-1.amazonaws.com"
API_URL="https://g3zokr6lo4.execute-api.us-east-1.amazonaws.com/prod/api/todos"

echo "📱 Testing Frontend Website..."
FRONTEND_STATUS=$(curl -s -o /dev/null -w "%{http_code}" "$FRONTEND_URL")
if [ "$FRONTEND_STATUS" = "200" ]; then
    echo -e "   ✅ Frontend: ${GREEN}ACCESSIBLE${NC} (HTTP $FRONTEND_STATUS)"
else
    echo -e "   ❌ Frontend: ${RED}FAILED${NC} (HTTP $FRONTEND_STATUS)"
fi

echo ""
echo "🔌 Testing Backend API..."

# Test GET endpoint
echo "   Testing GET /api/todos..."
GET_RESPONSE=$(curl -s -w "HTTPSTATUS:%{http_code}" "$API_URL")
GET_STATUS=$(echo $GET_RESPONSE | tr -d '\n' | sed -e 's/.*HTTPSTATUS://')
GET_BODY=$(echo $GET_RESPONSE | sed -e 's/HTTPSTATUS:.*//g')

if [ "$GET_STATUS" = "200" ]; then
    TODO_COUNT=$(echo $GET_BODY | jq '. | length' 2>/dev/null || echo "N/A")
    echo -e "   ✅ GET /api/todos: ${GREEN}SUCCESS${NC} (HTTP $GET_STATUS) - $TODO_COUNT items"
else
    echo -e "   ❌ GET /api/todos: ${RED}FAILED${NC} (HTTP $GET_STATUS)"
fi

# Test POST endpoint
echo "   Testing POST /api/todos..."
POST_DATA='{"description": "Verification test todo - '"$(date)"'"}'
POST_RESPONSE=$(curl -s -w "HTTPSTATUS:%{http_code}" -X POST "$API_URL" \
    -H "Content-Type: application/json" \
    -d "$POST_DATA")
POST_STATUS=$(echo $POST_RESPONSE | tr -d '\n' | sed -e 's/.*HTTPSTATUS://')

if [ "$POST_STATUS" = "200" ] || [ "$POST_STATUS" = "201" ]; then
    echo -e "   ✅ POST /api/todos: ${GREEN}SUCCESS${NC} (HTTP $POST_STATUS)"
else
    echo -e "   ❌ POST /api/todos: ${RED}FAILED${NC} (HTTP $POST_STATUS)"
fi

echo ""
echo "☁️  Checking AWS Resources..."

# Check Lambda function
echo "   Checking Lambda function..."
LAMBDA_STATUS=$(aws lambda get-function --function-name todo-api-lambda --query 'Configuration.State' --output text 2>/dev/null)
if [ "$LAMBDA_STATUS" = "Active" ]; then
    echo -e "   ✅ Lambda Function: ${GREEN}ACTIVE${NC}"
else
    echo -e "   ❌ Lambda Function: ${RED}$LAMBDA_STATUS${NC}"
fi

# Check DynamoDB table
echo "   Checking DynamoDB table..."
DYNAMO_STATUS=$(aws dynamodb describe-table --table-name TodoItems --query 'Table.TableStatus' --output text 2>/dev/null)
if [ "$DYNAMO_STATUS" = "ACTIVE" ]; then
    echo -e "   ✅ DynamoDB Table: ${GREEN}ACTIVE${NC}"
else
    echo -e "   ❌ DynamoDB Table: ${RED}$DYNAMO_STATUS${NC}"
fi

# Check S3 bucket
echo "   Checking S3 bucket..."
S3_EXISTS=$(aws s3 ls s3://todo-app-frontend-cloudchef01-us-cdk/ 2>/dev/null | wc -l)
if [ "$S3_EXISTS" -gt 0 ]; then
    echo -e "   ✅ S3 Bucket: ${GREEN}EXISTS WITH FILES${NC}"
else
    echo -e "   ❌ S3 Bucket: ${RED}EMPTY OR INACCESSIBLE${NC}"
fi

echo ""
echo "📊 Summary"
echo "=========="
echo -e "Frontend URL: ${YELLOW}$FRONTEND_URL${NC}"
echo -e "Backend URL:  ${YELLOW}$API_URL${NC}"
echo -e "AWS Account:  ${YELLOW}111735445051 (cloudchef01)${NC}"
echo -e "Region:       ${YELLOW}us-east-1${NC}"
echo ""
echo "🎉 Verification completed!"
echo ""
echo "💡 To use the application:"
echo "   1. Visit the frontend URL in your browser"
echo "   2. Use the API endpoints for integration"
echo "   3. Check CloudWatch logs for debugging"
