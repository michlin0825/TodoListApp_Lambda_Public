# GitHub Repository Setup Instructions

Follow these steps to push your Todo List Application to GitHub:

## 1. Create a New Repository on GitHub

1. Go to [GitHub](https://github.com/) and sign in
2. Click on the "+" icon in the top right corner and select "New repository"
3. Repository name: `TodoListApp_Lambda`
4. Description: `A serverless Todo List application built with AWS Lambda, API Gateway, DynamoDB, and S3`
5. Keep it as Public (or Private if you prefer)
6. Do NOT initialize with README, .gitignore, or license (we already have these files)
7. Click "Create repository"

## 2. Push Your Local Repository to GitHub

Run the following commands in your terminal:

```bash
# Navigate to your project directory (if not already there)
cd /Users/mba/Desktop/TodoListApp_Lambda

# Add the GitHub repository as a remote
git remote add origin https://github.com/michlin0825/TodoListApp_Lambda.git

# Push your code to GitHub
git branch -M main
git push -u origin main
```

## 3. Using SSH Instead of HTTPS (Optional)

If you prefer using SSH instead of HTTPS, use this command instead:

```bash
git remote add origin git@github.com:michlin0825/TodoListApp_Lambda.git
```

## 4. Verify Your Repository

After pushing, visit your repository at:
https://github.com/michlin0825/TodoListApp_Lambda

You should see all your files and commit history there.

## 5. Additional GitHub Setup (Optional)

Consider setting up:
- GitHub Pages for documentation
- GitHub Actions for CI/CD
- Branch protection rules
- Collaborators and teams
