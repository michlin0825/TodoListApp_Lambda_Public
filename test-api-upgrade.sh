#!/bin/bash

echo "🧪 Testing LambdaRestApi L3 Upgrade..."

# Get API URL from CDK output
API_URL=$(aws cloudformation describe-stacks --stack-name TodoAppStack --query "Stacks[0].Outputs[?OutputKey=='ApiUrl'].OutputValue" --output text --region us-east-1)

echo "API URL: $API_URL"

# Test all endpoints
echo "Testing GET /api/todos..."
curl -s "$API_URL/api/todos" | jq '.' || echo "❌ GET failed"

echo "Testing POST /api/todos..."
curl -s -X POST "$API_URL/api/todos" \
  -H "Content-Type: application/json" \
  -d '{"title":"L3 Test Todo","isCompleted":false}' | jq '.' || echo "❌ POST failed"

echo "✅ LambdaRestApi upgrade test complete"
