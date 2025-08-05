/**
 * API service for interacting with the Todo API
 * 
 * This service handles all communication with the backend API
 * for creating, reading, updating, and deleting todo items.
 */
class TodoApi {
    /**
     * Initialize the API service
     */
    constructor() {
        // Use the API URL from config.js
        this.baseUrl = CONFIG.API_URL;
    }

    /**
     * Get all todos
     * @returns {Promise<Array>} Array of todo items
     * @throws {Error} If the API request fails
     */
    async getAllTodos() {
        try {
            console.log('Fetching todos from:', this.baseUrl);
            const response = await fetch(this.baseUrl, {
                method: 'GET',
                headers: {
                    'Accept': 'application/json'
                },
                mode: 'cors',
                cache: 'no-cache',
                credentials: 'omit'
            });
            
            if (!response.ok) {
                console.error('Response not OK:', response.status, response.statusText);
                throw new Error(`Error: ${response.status} - ${response.statusText}`);
            }
            
            const data = await response.json();
            console.log('Fetched todos successfully:', data);
            return data;
        } catch (error) {
            console.error('Failed to fetch todos:', error);
            throw error;
        }
    }

    /**
     * Get a specific todo by ID
     * @param {string} id - Todo ID
     * @returns {Promise<Object>} Todo item
     * @throws {Error} If the API request fails
     */
    async getTodoById(id) {
        if (!id) {
            throw new Error('Todo ID is required');
        }
        
        try {
            const response = await fetch(`${this.baseUrl}/${id}`, {
                method: 'GET',
                headers: {
                    'Accept': 'application/json'
                },
                mode: 'cors',
                cache: 'no-cache',
                credentials: 'omit'
            });
            
            if (!response.ok) {
                throw new Error(`Error: ${response.status} - ${response.statusText}`);
            }
            
            return await response.json();
        } catch (error) {
            console.error(`Failed to fetch todo with ID ${id}:`, error);
            throw error;
        }
    }

    /**
     * Create a new todo
     * @param {Object} todo - Todo object with description
     * @returns {Promise<Object>} Created todo
     * @throws {Error} If the API request fails
     */
    async createTodo(todo) {
        if (!todo || !todo.description) {
            throw new Error('Todo description is required');
        }
        
        try {
            console.log('API sending:', JSON.stringify(todo));
            const response = await fetch(this.baseUrl, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Accept': 'application/json'
                },
                mode: 'cors',
                cache: 'no-cache',
                credentials: 'omit',
                body: JSON.stringify(todo)
            });
            
            if (!response.ok) {
                const errorText = await response.text();
                console.error('Server error response:', errorText);
                throw new Error(`Error: ${response.status} - ${response.statusText}`);
            }
            
            return await response.json();
        } catch (error) {
            console.error('Failed to create todo:', error);
            throw error;
        }
    }

    /**
     * Update an existing todo
     * @param {string} id - Todo ID
     * @param {Object} todo - Updated todo object
     * @returns {Promise<void>}
     * @throws {Error} If the API request fails
     */
    async updateTodo(id, todo) {
        if (!id) {
            throw new Error('Todo ID is required');
        }
        
        if (!todo || !todo.description) {
            throw new Error('Todo description is required');
        }
        
        try {
            const response = await fetch(`${this.baseUrl}/${id}`, {
                method: 'PUT',
                headers: {
                    'Content-Type': 'application/json',
                    'Accept': 'application/json'
                },
                mode: 'cors',
                cache: 'no-cache',
                credentials: 'omit',
                body: JSON.stringify(todo)
            });
            
            if (!response.ok) {
                const errorText = await response.text();
                console.error('Server error response:', errorText);
                throw new Error(`Error: ${response.status} - ${response.statusText}`);
            }
        } catch (error) {
            console.error(`Failed to update todo with ID ${id}:`, error);
            throw error;
        }
    }

    /**
     * Delete a todo
     * @param {string} id - Todo ID
     * @returns {Promise<void>}
     * @throws {Error} If the API request fails
     */
    async deleteTodo(id) {
        if (!id) {
            throw new Error('Todo ID is required');
        }
        
        try {
            const response = await fetch(`${this.baseUrl}/${id}`, {
                method: 'DELETE',
                headers: {
                    'Accept': 'application/json'
                },
                mode: 'cors',
                cache: 'no-cache',
                credentials: 'omit'
            });
            
            if (!response.ok) {
                throw new Error(`Error: ${response.status} - ${response.statusText}`);
            }
        } catch (error) {
            console.error(`Failed to delete todo with ID ${id}:`, error);
            throw error;
        }
    }

    /**
     * Toggle the completion status of a todo
     * @param {string} id - Todo ID
     * @returns {Promise<void>}
     * @throws {Error} If the API request fails
     */
    async toggleTodoStatus(id) {
        if (!id) {
            throw new Error('Todo ID is required');
        }
        
        try {
            const response = await fetch(`${this.baseUrl}/${id}/toggle`, {
                method: 'PATCH',
                headers: {
                    'Accept': 'application/json'
                },
                mode: 'cors',
                cache: 'no-cache',
                credentials: 'omit'
            });
            
            if (!response.ok) {
                throw new Error(`Error: ${response.status} - ${response.statusText}`);
            }
        } catch (error) {
            console.error(`Failed to toggle status for todo with ID ${id}:`, error);
            throw error;
        }
    }
}
