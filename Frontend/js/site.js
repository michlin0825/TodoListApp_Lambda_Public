/**
 * Main application script for Todo List App
 */
document.addEventListener('DOMContentLoaded', () => {
    // Initialize the API service
    const api = new TodoApi();
    
    // DOM elements
    const todoListContainer = document.getElementById('todo-list-container');
    const emptyState = document.getElementById('empty-state');
    const todoStats = document.getElementById('todo-stats');
    const errorContainer = document.getElementById('error-container');
    const errorMessage = document.getElementById('error-message');
    
    // Check for newly created or updated todos from localStorage
    checkForLocalStorageTodos();
    
    // Load todos on page load
    loadTodos();
    
    /**
     * Check for newly created or updated todos from localStorage
     * This allows for immediate feedback when returning from create/edit pages
     */
    function checkForLocalStorageTodos() {
        // Check for newly created todo
        const newTodoJson = localStorage.getItem('newTodo');
        if (newTodoJson) {
            try {
                const newTodo = JSON.parse(newTodoJson);
                console.log('Found newly created todo in localStorage:', newTodo);
                // Clear it from localStorage
                localStorage.removeItem('newTodo');
            } catch (error) {
                console.error('Error parsing newTodo from localStorage:', error);
            }
        }
        
        // Check for updated todo
        const updatedTodoJson = localStorage.getItem('updatedTodo');
        if (updatedTodoJson) {
            try {
                const updatedTodo = JSON.parse(updatedTodoJson);
                console.log('Found updated todo in localStorage:', updatedTodo);
                // Clear it from localStorage
                localStorage.removeItem('updatedTodo');
            } catch (error) {
                console.error('Error parsing updatedTodo from localStorage:', error);
            }
        }
    }
    
    /**
     * Load all todos from the API
     */
    async function loadTodos() {
        try {
            showLoading();
            
            const todos = await api.getAllTodos();
            console.log('Loaded todos:', todos);
            renderTodos(todos);
            updateStats(todos);
            hideLoading();
            
            // Hide error message if it was previously shown
            errorContainer.classList.add('d-none');
        } catch (error) {
            console.error('Error loading todos:', error);
            showError('Failed to load todos. Please try again later. (Error: ' + error.message + ')');
            hideLoading();
            
            // Show empty state when there's an error
            emptyState.classList.remove('d-none');
            // Update stats to show 0 todos
            updateStats([]);
        }
    }
    
    /**
     * Render todos in the list
     * @param {Array} todos - Array of todo items
     */
    function renderTodos(todos) {
        todoListContainer.innerHTML = '';
        
        if (!todos || todos.length === 0) {
            emptyState.classList.remove('d-none');
        } else {
            emptyState.classList.add('d-none');
            
            todos.forEach(todo => {
                const todoElement = createTodoElement(todo);
                todoListContainer.appendChild(todoElement);
            });
        }
    }
    
    /**
     * Create a todo list item element
     * @param {Object} todo - Todo object
     * @returns {HTMLElement} Todo list item element
     */
    function createTodoElement(todo) {
        const template = document.getElementById('todo-item-template');
        const todoElement = template.content.cloneNode(true).querySelector('.list-group-item');
        
        // Set data attribute for ID
        todoElement.dataset.id = todo.id;
        
        // Set completed class if needed
        if (todo.isCompleted) {
            todoElement.classList.add('bg-light');
        }
        
        // Set description
        const descriptionElement = todoElement.querySelector('.todo-description');
        descriptionElement.textContent = todo.description;
        if (todo.isCompleted) {
            descriptionElement.classList.add('text-decoration-line-through');
            descriptionElement.classList.add('text-muted');
        }
        
        // Set toggle button
        const toggleButton = todoElement.querySelector('.toggle-status-btn');
        const toggleIcon = toggleButton.querySelector('i');
        if (todo.isCompleted) {
            toggleButton.classList.remove('btn-outline-secondary');
            toggleButton.classList.add('btn-success');
            toggleIcon.classList.remove('bi-circle');
            toggleIcon.classList.add('bi-check-circle-fill');
        }
        
        // Add event listeners
        toggleButton.addEventListener('click', (e) => {
            e.preventDefault();
            toggleTodoStatusWithImmediate(todo.id, todoElement);
        });
        
        const editButton = todoElement.querySelector('.edit-todo-btn');
        editButton.href = `edit.html?id=${todo.id}`;
        
        todoElement.querySelector('.delete-todo-btn').addEventListener('click', (e) => {
            e.preventDefault();
            deleteTodoWithImmediate(todo.id, todoElement);
        });
        
        return todoElement;
    }
    
    /**
     * Update todo statistics
     * @param {Array} todos - Array of todo items
     */
    function updateStats(todos) {
        if (!todos) {
            todoStats.textContent = 'Total: 0 | Completed: 0 | Pending: 0';
            return;
        }
        
        const total = todos.length;
        const completed = todos.filter(todo => todo.isCompleted).length;
        const pending = total - completed;
        
        todoStats.textContent = `Total: ${total} | Completed: ${completed} | Pending: ${pending}`;
    }
    
    /**
     * Toggle todo completion status with immediate visual feedback
     * @param {number} id - Todo ID
     * @param {HTMLElement} todoElement - The todo list item element
     */
    async function toggleTodoStatusWithImmediate(id, todoElement) {
        // Get current state
        const isCurrentlyCompleted = todoElement.classList.contains('bg-light');
        
        // Get elements to update
        const descriptionElement = todoElement.querySelector('.todo-description');
        const toggleButton = todoElement.querySelector('.toggle-status-btn');
        const toggleIcon = toggleButton.querySelector('i');
        
        // Update UI immediately
        if (!isCurrentlyCompleted) {
            // Mark as completed
            todoElement.classList.add('bg-light');
            descriptionElement.classList.add('text-decoration-line-through', 'text-muted');
            toggleButton.classList.remove('btn-outline-secondary');
            toggleButton.classList.add('btn-success');
            toggleIcon.classList.remove('bi-circle');
            toggleIcon.classList.add('bi-check-circle-fill');
        } else {
            // Mark as not completed
            todoElement.classList.remove('bg-light');
            descriptionElement.classList.remove('text-decoration-line-through', 'text-muted');
            toggleButton.classList.remove('btn-success');
            toggleButton.classList.add('btn-outline-secondary');
            toggleIcon.classList.remove('bi-check-circle-fill');
            toggleIcon.classList.add('bi-circle');
        }
        
        // Update stats immediately (approximate)
        const allTodos = document.querySelectorAll('.list-group-item');
        const completedTodos = document.querySelectorAll('.list-group-item.bg-light');
        const total = allTodos.length;
        const completed = completedTodos.length;
        const pending = total - completed;
        todoStats.textContent = `Total: ${total} | Completed: ${completed} | Pending: ${pending}`;
        
        // Send API request in the background
        try {
            await api.toggleTodoStatus(id);
            // No need to reload the entire list
        } catch (error) {
            console.error('Error toggling todo status:', error);
            showError('Failed to update todo status. The page will refresh to show the correct state.');
            
            // If there was an error, reload the list to get the correct state
            setTimeout(() => {
                loadTodos();
            }, 2000);
        }
    }
    
    /**
     * Delete a todo with immediate visual feedback
     * @param {number} id - Todo ID
     * @param {HTMLElement} todoElement - The todo list item element
     */
    async function deleteTodoWithImmediate(id, todoElement) {
        if (!confirm('Are you sure you want to delete this item?')) {
            // User canceled the deletion, do nothing
            return;
        }
        
        // Apply fade-out animation
        todoElement.classList.add('fade-out');
        
        // Update stats immediately (approximate)
        const isCompleted = todoElement.classList.contains('bg-light');
        const allTodos = document.querySelectorAll('.list-group-item').length - 1; // Subtract the one being deleted
        const completedTodos = document.querySelectorAll('.list-group-item.bg-light').length - (isCompleted ? 1 : 0);
        const total = allTodos;
        const completed = completedTodos;
        const pending = total - completed;
        todoStats.textContent = `Total: ${total} | Completed: ${completed} | Pending: ${pending}`;
        
        // Wait for animation to complete
        setTimeout(async () => {
            // Remove the element from the DOM
            todoElement.remove();
            
            // Show empty state if no todos left
            if (document.querySelectorAll('.list-group-item').length === 0) {
                emptyState.classList.remove('d-none');
            }
            
            // Send API request in the background
            try {
                await api.deleteTodo(id);
                // No need to reload the entire list
            } catch (error) {
                console.error('Error deleting todo:', error);
                showError('Failed to delete todo. The page will refresh to show the correct state.');
                
                // If there was an error, reload the list to get the correct state
                setTimeout(() => {
                    loadTodos();
                }, 2000);
            }
        }, 300);
    }
    
    /**
     * Show loading spinner
     */
    function showLoading() {
        // Remove any existing spinner
        const existingSpinner = document.querySelector('.spinner-container');
        if (existingSpinner) existingSpinner.remove();
        
        // Create and add spinner
        const spinnerContainer = document.createElement('div');
        spinnerContainer.className = 'spinner-container';
        spinnerContainer.innerHTML = `
            <div class="spinner-border text-primary" role="status">
                <span class="visually-hidden">Loading...</span>
            </div>
        `;
        
        todoListContainer.innerHTML = '';
        todoListContainer.appendChild(spinnerContainer);
    }
    
    /**
     * Hide loading spinner
     */
    function hideLoading() {
        const spinner = document.querySelector('.spinner-container');
        if (spinner) spinner.remove();
    }
    
    /**
     * Show error message
     * @param {string} message - Error message
     */
    function showError(message) {
        errorMessage.textContent = message;
        errorContainer.classList.remove('d-none');
    }
    
    // Hide error on click
    if (errorContainer) {
        errorContainer.addEventListener('click', () => {
            errorContainer.classList.add('d-none');
        });
    }
    
    // Add a reload button to the error container
    const reloadButton = document.createElement('button');
    reloadButton.className = 'btn btn-primary mt-2';
    reloadButton.textContent = 'Retry';
    reloadButton.addEventListener('click', (e) => {
        e.preventDefault();
        loadTodos();
    });
    errorContainer.appendChild(reloadButton);
});
