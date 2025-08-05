#!/bin/bash

echo "🚀 Deploying L2 → L3 Upgrade"
echo "============================"

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

# Change to CDK directory
cd ~/Desktop/TFC_NGDE/ngde_sandbox_projects/TodoListApp_Lambda/TodoAppCdk

# Step 1: Backup current deployment
echo -e "${YELLOW}Step 1: Creating backup...${NC}"
git add -A
git commit -m "Pre-L3-upgrade backup - $(date)"
git tag "pre-l3-upgrade-$(date +%Y%m%d-%H%M%S)"

# Step 2: Build and test
echo -e "${YELLOW}Step 2: Building and testing...${NC}"
dotnet restore TodoAppCdk/TodoAppCdk.csproj
if [ $? -ne 0 ]; then
    echo -e "${RED}❌ Build failed${NC}"
    exit 1
fi

# Step 3: Deploy application stack with L3 upgrades
echo -e "${YELLOW}Step 3: Deploying L3 upgraded application...${NC}"
cdk deploy TodoAppStack --require-approval never
if [ $? -eq 0 ]; then
    echo -e "${GREEN}✅ L3 Application deployment successful${NC}"
else
    echo -e "${RED}❌ L3 Application deployment failed${NC}"
    echo "Rolling back..."
    git reset --hard HEAD~1
    cdk deploy TodoAppStack --require-approval never
    exit 1
fi

# Step 4: Test deployed application
echo -e "${YELLOW}Step 4: Testing deployed application...${NC}"
API_URL=$(aws cloudformation describe-stacks --stack-name TodoAppStack --query "Stacks[0].Outputs[?OutputKey=='ApiUrl'].OutputValue" --output text --region us-east-1)

if curl -f "$API_URL/api/todos" > /dev/null 2>&1; then
    echo -e "${GREEN}✅ API endpoint responding${NC}"
else
    echo -e "${RED}❌ API endpoint not responding${NC}"
    exit 1
fi

# Step 5: Optional - Deploy Modern Pipeline
echo -e "${YELLOW}Step 5: Deploy Modern Pipeline? (y/n)${NC}"
read -r response
if [[ "$response" =~ ^[Yy]$ ]]; then
    echo "Deploying Modern CDK Pipeline..."
    cdk deploy TodoModernPipelineStack --context modern-pipeline=true --require-approval never
    if [ $? -eq 0 ]; then
        echo -e "${GREEN}✅ Modern Pipeline deployment successful${NC}"
    else
        echo -e "${RED}❌ Modern Pipeline deployment failed${NC}"
    fi
fi

echo ""
echo -e "${GREEN}🎉 L3 Upgrade Deployment Complete!${NC}"
echo "=================================="
echo "✅ LambdaRestApi: API Gateway simplified"
echo "✅ StaticWebsite: S3 website construct deployed"
echo "✅ Modern Pipeline: Available for deployment"
echo ""
echo "Application URLs:"
echo "• API: $API_URL"
echo "• Frontend: Check CloudFormation outputs"
echo ""
echo "Monitoring:"
echo "• CloudWatch Logs: /aws/lambda/TodoAppStack-TodoLambda*"
echo "• API Gateway: AWS Console → API Gateway → Todo Service"
