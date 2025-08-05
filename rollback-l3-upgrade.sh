#!/bin/bash

echo "🔄 Rolling Back L3 Upgrade"
echo "========================="

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

cd ~/Desktop/TFC_NGDE/ngde_sandbox_projects/TodoListApp_Lambda

# Step 1: Find backup tag
echo -e "${YELLOW}Step 1: Finding backup tag...${NC}"
BACKUP_TAG=$(git tag | grep "pre-l3-upgrade" | tail -1)

if [ -z "$BACKUP_TAG" ]; then
    echo -e "${RED}❌ No backup tag found${NC}"
    echo "Available tags:"
    git tag
    exit 1
fi

echo -e "${GREEN}Found backup tag: $BACKUP_TAG${NC}"

# Step 2: Confirm rollback
echo -e "${YELLOW}Step 2: Confirm rollback to $BACKUP_TAG? (y/n)${NC}"
read -r response
if [[ ! "$response" =~ ^[Yy]$ ]]; then
    echo "Rollback cancelled"
    exit 0
fi

# Step 3: Reset to backup
echo -e "${YELLOW}Step 3: Resetting to backup...${NC}"
git reset --hard "$BACKUP_TAG"

# Step 4: Redeploy original L2 version
echo -e "${YELLOW}Step 4: Redeploying original L2 version...${NC}"
cd TodoAppCdk
dotnet restore TodoAppCdk/TodoAppCdk.csproj
cdk deploy TodoAppStack --require-approval never

if [ $? -eq 0 ]; then
    echo -e "${GREEN}✅ Rollback successful${NC}"
else
    echo -e "${RED}❌ Rollback failed${NC}"
    exit 1
fi

# Step 5: Test rolled back application
echo -e "${YELLOW}Step 5: Testing rolled back application...${NC}"
API_URL=$(aws cloudformation describe-stacks --stack-name TodoAppStack --query "Stacks[0].Outputs[?OutputKey=='ApiUrl'].OutputValue" --output text --region us-east-1)

if curl -f "$API_URL/api/todos" > /dev/null 2>&1; then
    echo -e "${GREEN}✅ Rolled back API endpoint responding${NC}"
else
    echo -e "${RED}❌ Rolled back API endpoint not responding${NC}"
fi

echo ""
echo -e "${GREEN}🔄 Rollback Complete${NC}"
echo "==================="
echo "• Code reset to: $BACKUP_TAG"
echo "• L2 constructs restored"
echo "• Application redeployed"
echo "• API URL: $API_URL"
