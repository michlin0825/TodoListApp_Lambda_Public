/**
 * Create Todo page script
 */
document.addEventListener('DOMContentLoaded', () => {
    // Initialize the API service
    const api = new TodoApi();
    
    // DOM elements
    const createTodoForm = document.getElementById('create-todo-form');
    const todoDescription = document.getElementById('todo-description');
    const todoCompleted = document.getElementById('todo-completed');
    const errorContainer = document.getElementById('error-container');
    const errorMessage = document.getElementById('error-message');
    const submitButton = document.querySelector('button[type="submit"]');
    const backButton = document.querySelector('a.btn-outline-secondary');
    
    // Focus on the description field when the page loads
    todoDescription.focus();
    
    /**
     * Create a new todo
     * @param {Event} event - Form submit event
     */
    async function createTodo(event) {
        event.preventDefault();
        
        const description = todoDescription.value.trim();
        if (!description) {
            todoDescription.classList.add('is-invalid');
            return;
        }
        
        // Show saving state on button
        submitButton.disabled = true;
        submitButton.innerHTML = '<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Creating...';
        backButton.classList.add('disabled');
        
        try {
            const newTodo = {
                description: description,
                isCompleted: todoCompleted.checked
            };
            
            // Store the new todo in localStorage for immediate display on the list page
            const createdTodo = await api.createTodo(newTodo);
            localStorage.setItem('newTodo', JSON.stringify(createdTodo));
            
            // Redirect back to the list page
            window.location.href = 'index.html';
        } catch (error) {
            console.error('Error creating todo:', error);
            showError('Failed to create todo. Please try again.');
            
            // Reset button state
            submitButton.disabled = false;
            submitButton.innerHTML = '<i class="bi bi-save"></i> Create';
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
    createTodoForm.addEventListener('submit', createTodo);
    
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
