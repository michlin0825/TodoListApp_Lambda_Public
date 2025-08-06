/**
 * Configuration for the Todo application
 * 
 * This file contains the configuration for the Todo application.
 * Update the API_URL to point to your API Gateway endpoint.
 * 
 * After deployment, replace <your-api-id> with the actual API Gateway ID
 * from your CDK deployment output.
 */
const CONFIG = {
    API_URL: 'https://<your-api-id>.execute-api.us-east-1.amazonaws.com/prod/api/todos'
};
