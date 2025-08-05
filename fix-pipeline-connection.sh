#!/bin/bash

echo "🔧 TodoList CI/CD Pipeline Connection Fix"
echo "========================================"

# Colors for output
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}📋 Issue Identified:${NC}"
echo "• CodeStar connection is broken/deleted"
echo "• Pipeline Source stage failing with 'No Branch [main] found'"
echo "• Connection ARN: arn:aws:codeconnections:us-east-1:111735445051:connection/7d72fc73-18b2-4aa6-b21f-346b381c97d8"
echo ""

echo -e "${YELLOW}🔧 Solution Steps:${NC}"
echo ""

echo -e "${YELLOW}Step 1: Authorize New CodeStar Connection${NC}"
echo "1. Go to AWS Console → Developer Tools → CodePipeline → Settings → Connections"
echo "2. Find connection: 'TodoList-GitHub-Connection'"
echo "3. Connection ARN: arn:aws:codeconnections:us-east-1:111735445051:connection/43013017-1cef-457e-bfb9-4f2c294ad835"
echo "4. Click 'Update pending connection' and authorize with GitHub"
echo "5. Status should change from 'Pending' to 'Available'"
echo ""

echo -e "${YELLOW}Step 2: Check Connection Status${NC}"
echo "Run this command to verify connection is authorized:"
echo "aws codeconnections get-connection --connection-arn \"arn:aws:codeconnections:us-east-1:111735445051:connection/43013017-1cef-457e-bfb9-4f2c294ad835\" --region us-east-1"
echo ""

echo -e "${YELLOW}Step 3: Update Pipeline Configuration${NC}"
echo "Once connection is authorized, run:"
echo "aws codepipeline update-pipeline --cli-input-json file:///tmp/updated-pipeline-config.json --region us-east-1"
echo ""

echo -e "${YELLOW}Step 4: Test Pipeline${NC}"
echo "Trigger pipeline to test the fix:"
echo "aws codepipeline start-pipeline-execution --name \"TodoList-CI-CD-Pipeline\" --region us-east-1"
echo ""

echo -e "${GREEN}📊 Current Status:${NC}"
echo "• New CodeStar connection created: ✅"
echo "• Updated pipeline configuration prepared: ✅"
echo "• Waiting for GitHub authorization: ⏳"
echo ""

echo -e "${BLUE}🌐 Quick Links:${NC}"
echo "• AWS Console Connections: https://console.aws.amazon.com/codesuite/settings/connections"
echo "• Pipeline Console: https://console.aws.amazon.com/codesuite/codepipeline/pipelines/TodoList-CI-CD-Pipeline/view"
echo ""

echo -e "${GREEN}💡 Alternative: Manual Pipeline Update${NC}"
echo "If you prefer to update via console:"
echo "1. Go to CodePipeline → TodoList-CI-CD-Pipeline → Edit"
echo "2. Edit Source stage → Change connection to new one"
echo "3. Save pipeline"
echo ""

# Check if connection is already authorized
echo -e "${YELLOW}Checking connection status...${NC}"
CONNECTION_STATUS=$(aws codeconnections get-connection --connection-arn "arn:aws:codeconnections:us-east-1:111735445051:connection/43013017-1cef-457e-bfb9-4f2c294ad835" --region us-east-1 --query 'connectionStatus' --output text 2>/dev/null)

if [ "$CONNECTION_STATUS" = "AVAILABLE" ]; then
    echo -e "${GREEN}✅ Connection is already authorized!${NC}"
    echo "You can now update the pipeline configuration:"
    echo "aws codepipeline update-pipeline --cli-input-json file:///tmp/updated-pipeline-config.json --region us-east-1"
elif [ "$CONNECTION_STATUS" = "PENDING" ]; then
    echo -e "${YELLOW}⏳ Connection is pending authorization${NC}"
    echo "Please complete GitHub authorization in AWS Console"
else
    echo -e "${RED}❌ Unable to check connection status${NC}"
    echo "Please verify connection in AWS Console"
fi
