/**
 * Edit Todo page script
 */
document.addEventListener('DOMContentLoaded', () => {
    // Initialize the API service
    const api = new TodoApi();
    
    // DOM elements
    const editTodoForm = document.getElementById('edit-todo-form');
    const todoId = document.getElementById('todo-id');
    const todoDescription = document.getElementById('todo-description');
    const todoCompleted = document.getElementById('todo-completed');
    const errorContainer = document.getElementById('error-container');
    const errorMessage = document.getElementById('error-message');
    const submitButton = document.querySelector('button[type="submit"]');
    const backButton = document.querySelector('a.btn-outline-secondary');
    
    // Get todo ID from URL
    const urlParams = new URLSearchParams(window.location.search);
    const id = urlParams.get('id');
    
    if (!id) {
        window.location.href = 'index.html';
    }
    
    // Load todo data
    loadTodo(id);
    
    /**
     * Load todo data
     * @param {string} id - Todo ID
     */
    async function loadTodo(id) {
        try {
            // Show loading state
            todoDescription.disabled = true;
            todoCompleted.disabled = true;
            submitButton.disabled = true;
            submitButton.innerHTML = '<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Loading...';
            
            const todo = await api.getTodoById(id);
            
            todoId.value = todo.id;
            todoDescription.value = todo.description;
            todoCompleted.checked = todo.isCompleted;
            
            // Hide loading state
            todoDescription.disabled = false;
            todoCompleted.disabled = false;
            submitButton.disabled = false;
            submitButton.innerHTML = '<i class="bi bi-save"></i> Save';
            
            // Focus on the description field
            todoDescription.focus();
        } catch (error) {
            console.error('Error loading todo:', error);
            showError('Failed to load todo. Please try again.');
            
            // Enable form elements but keep submit button disabled
            todoDescription.disabled = false;
            todoCompleted.disabled = false;
            submitButton.innerHTML = '<i class="bi bi-save"></i> Save';
        }
    }
    
    /**
     * Update todo
     * @param {Event} event - Form submit event
     */
    async function updateTodo(event) {
        event.preventDefault();
        
        const description = todoDescription.value.trim();
        if (!description) {
            todoDescription.classList.add('is-invalid');
            return;
        }
        
        // Show saving state on button
        submitButton.disabled = true;
        submitButton.innerHTML = '<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Saving...';
        backButton.classList.add('disabled');
        
        try {
            const updatedTodo = {
                id: todoId.value,
                description: description,
                isCompleted: todoCompleted.checked
            };
            
            // Store the updated todo in localStorage for immediate display on the list page
            localStorage.setItem('updatedTodo', JSON.stringify(updatedTodo));
            
            // Send the update to the API
            await api.updateTodo(updatedTodo.id, updatedTodo);
            
            // Redirect back to the list page
            window.location.href = 'index.html';
        } catch (error) {
            console.error('Error updating todo:', error);
            showError('Failed to update todo. Please try again.');
            
            // Reset button state
            submitButton.disabled = false;
            submitButton.innerHTML = '<i class="bi bi-save"></i> Save';
            backButton.classList.remove('disabled');
        }
    }
    
    /**
     * Show error message
     * @param {string} message - Error message
     */
    function showError(message) {
        errorMessage.textContent = message;
        errorContainer.classList.remove('d-none');
    }
    
    // Event listeners
    editTodoForm.addEventListener('submit', updateTodo);
    
    // Input validation
    todoDescription.addEventListener('input', () => {
        if (todoDescription.value.trim()) {
            todoDescription.classList.remove('is-invalid');
        } else {
            todoDescription.classList.add('is-invalid');
        }
    });
    
    // Hide error on click
    errorContainer.addEventListener('click', () => {
        errorContainer.classList.add('d-none');
    });
});
