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
    const addTodoForm = document.getElementById('add-todo-form');
    const newTodoInput = document.getElementById('new-todo-input');
    const newTodoCompleted = document.getElementById('new-todo-completed');
    const errorContainer = document.getElementById('error-container');
    const errorMessage = document.getElementById('error-message');
    const addNewBtn = document.getElementById('add-new-btn');
    const addFormContainer = document.getElementById('add-form-container');
    const cancelAddBtn = document.getElementById('cancel-add-btn');
    
    // Modal elements
    const editTodoModal = new bootstrap.Modal(document.getElementById('edit-todo-modal'));
    const editTodoForm = document.getElementById('edit-todo-form');
    const editTodoId = document.getElementById('edit-todo-id');
    const editTodoDescription = document.getElementById('edit-todo-description');
    const editTodoCompleted = document.getElementById('edit-todo-completed');
    const saveEditBtn = document.getElementById('save-edit-btn');
    
    // Load todos on page load
    loadTodos();
    
    /**
     * Load all todos from the API
     */
    async function loadTodos() {
        try {
            showLoading();
            const todos = await api.getAllTodos();
            renderTodos(todos);
            updateStats(todos);
            hideLoading();
        } catch (error) {
            console.error('Error loading todos:', error);
            showError('Failed to load todos. Please try again later.');
            hideLoading();
        }
    }
    
    /**
     * Render todos in the list
     * @param {Array} todos - Array of todo items
     */
    function renderTodos(todos) {
        todoListContainer.innerHTML = '';
        
        if (todos.length === 0) {
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
        todoElement.querySelector('.toggle-status-btn').addEventListener('click', () => toggleTodoStatus(todo.id));
        todoElement.querySelector('.edit-todo-btn').addEventListener('click', () => openEditModal(todo));
        todoElement.querySelector('.delete-todo-btn').addEventListener('click', () => deleteTodo(todo.id));
        
        // Add animation class
        todoElement.classList.add('fade-in');
        
        return todoElement;
    }
    
    /**
     * Update todo statistics
     * @param {Array} todos - Array of todo items
     */
    function updateStats(todos) {
        const total = todos.length;
        const completed = todos.filter(todo => todo.isCompleted).length;
        const pending = total - completed;
        
        todoStats.textContent = `Total: ${total} | Completed: ${completed} | Pending: ${pending}`;
    }
    
    /**
     * Show add todo form
     */
    function showAddForm() {
        addNewBtn.classList.add('d-none');
        addFormContainer.classList.remove('d-none');
        newTodoInput.focus();
    }
    
    /**
     * Hide add todo form
     */
    function hideAddForm() {
        addNewBtn.classList.remove('d-none');
        addFormContainer.classList.add('d-none');
        newTodoInput.value = '';
        newTodoCompleted.checked = false;
    }
    
    /**
     * Add a new todo
     * @param {Event} event - Form submit event
     */
    async function addTodo(event) {
        event.preventDefault();
        
        const description = newTodoInput.value.trim();
        if (!description) return;
        
        try {
            const newTodo = {
                description: description,
                isCompleted: newTodoCompleted.checked
            };
            
            console.log('Sending new todo:', newTodo);
            await api.createTodo(newTodo);
            hideAddForm();
            await loadTodos();
        } catch (error) {
            console.error('Error adding todo:', error);
            showError('Failed to add todo. Please try again.');
        }
    }
    
    /**
     * Toggle todo completion status
     * @param {number} id - Todo ID
     */
    async function toggleTodoStatus(id) {
        try {
            await api.toggleTodoStatus(id);
            await loadTodos();
        } catch (error) {
            console.error('Error toggling todo status:', error);
            showError('Failed to update todo status. Please try again.');
        }
    }
    
    /**
     * Open edit modal with todo data
     * @param {Object} todo - Todo object
     */
    function openEditModal(todo) {
        editTodoId.value = todo.id;
        editTodoDescription.value = todo.description;
        editTodoCompleted.checked = todo.isCompleted;
        editTodoModal.show();
    }
    
    /**
     * Save edited todo
     */
    async function saveEditedTodo() {
        const id = parseInt(editTodoId.value);
        const description = editTodoDescription.value.trim();
        
        if (!description) return;
        
        try {
            const updatedTodo = {
                id: id,
                description: description,
                isCompleted: editTodoCompleted.checked
            };
            
            await api.updateTodo(id, updatedTodo);
            editTodoModal.hide();
            await loadTodos();
        } catch (error) {
            console.error('Error updating todo:', error);
            showError('Failed to update todo. Please try again.');
        }
    }
    
    /**
     * Delete a todo
     * @param {number} id - Todo ID
     */
    async function deleteTodo(id) {
        if (!confirm('Are you sure you want to delete this item?')) return;
        
        try {
            const todoElement = document.querySelector(`.list-group-item[data-id="${id}"]`);
            todoElement.classList.add('fade-out');
            
            // Wait for animation to complete
            setTimeout(async () => {
                await api.deleteTodo(id);
                await loadTodos();
            }, 300);
        } catch (error) {
            console.error('Error deleting todo:', error);
            showError('Failed to delete todo. Please try again.');
        }
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
        
        // Auto-hide after 5 seconds
        setTimeout(() => {
            errorContainer.classList.add('d-none');
        }, 5000);
    }
    
    // Event listeners
    addNewBtn.addEventListener('click', showAddForm);
    cancelAddBtn.addEventListener('click', hideAddForm);
    addTodoForm.addEventListener('submit', addTodo);
    saveEditBtn.addEventListener('click', saveEditedTodo);
    
    // Hide error on click
    errorContainer.addEventListener('click', () => {
        errorContainer.classList.add('d-none');
    });
});
