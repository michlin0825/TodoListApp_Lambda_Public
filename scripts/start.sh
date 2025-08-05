#!/bin/bash

# Set error handling
set -e

# Function to display error messages
error() {
  echo "ERROR: $1" >&2
  exit 1
}

# Kill any existing processes on the required ports
echo "Checking for existing processes on ports 5050, 5051, and 5500..."
lsof -i :5050 | grep LISTEN | awk '{print $2}' | xargs kill -9 2>/dev/null || true
lsof -i :5051 | grep LISTEN | awk '{print $2}' | xargs kill -9 2>/dev/null || true
lsof -i :5500 | grep LISTEN | awk '{print $2}' | xargs kill -9 2>/dev/null || true

# Get the absolute path to the project directory
PROJECT_DIR="$(cd "$(dirname "$0")" && pwd)"
BACKEND_DIR="$PROJECT_DIR/Backend"
FRONTEND_DIR="$PROJECT_DIR/Frontend"

# Verify directories exist
[ -d "$BACKEND_DIR" ] || error "Backend directory not found: $BACKEND_DIR"
[ -d "$FRONTEND_DIR" ] || error "Frontend directory not found: $FRONTEND_DIR"
[ -f "$FRONTEND_DIR/index.html" ] || error "index.html not found in Frontend directory"

# Start the backend API
echo "Starting the backend API..."
cd "$BACKEND_DIR"
dotnet run &
BACKEND_PID=$!

# Wait for the backend to start
echo "Waiting for backend to start..."
sleep 5

# Verify the backend is running
if ! curl -s http://localhost:5050/api/todos > /dev/null; then
  echo "Backend API is not responding. Waiting a bit longer..."
  sleep 5
  
  # Try again
  if ! curl -s http://localhost:5050/api/todos > /dev/null; then
    echo "WARNING: Backend API may not be running correctly. Continuing anyway..."
  else
    echo "Backend API is now responding!"
  fi
fi

# Start the frontend server
echo "Starting the frontend server..."
cd "$FRONTEND_DIR"
echo "Current directory: $(pwd)"
echo "Files in this directory:"
ls -la

# Start the HTTP server
python3 -m http.server 5500 &
FRONTEND_PID=$!

# Wait for the frontend server to start
sleep 2

# Verify the frontend server is running
if ! curl -s http://localhost:5500 > /dev/null; then
  echo "WARNING: Frontend server may not be running correctly."
else
  echo "Frontend server is running!"
fi

echo ""
echo "Application started!"
echo "Backend API: http://localhost:5050"
echo "Swagger UI: http://localhost:5050/swagger"
echo "Frontend: http://localhost:5500"
echo ""
echo "Press Ctrl+C to stop both servers"

# Handle cleanup on exit
trap "kill $BACKEND_PID $FRONTEND_PID 2>/dev/null; echo 'Servers stopped'; exit" INT TERM EXIT

# Keep the script running
wait
