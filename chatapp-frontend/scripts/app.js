class ChatApp {
    constructor() {
        this.userId = null;
        this.currentSessionId = null;
        this.cachedSessions = null;
        this.cachedMessages = {};
        this.token = null;
        this.elements = {
            messageInput: document.getElementById('message-input'),
            sendBtn: document.getElementById('send-btn'),
            messages: document.getElementById('messages'),
            sessionList: document.getElementById('session-list')
        };
        this.API_URL = 'https://localhost:5001';
        this.initEventListeners();
        this.authenticateUser();
    }

    
    parseJwt(token) {
        try {
            const base64Url = token.split('.')[1];
            const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
            const jsonPayload = decodeURIComponent(atob(base64).split('').map(function(c) {
                return '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2);
            }).join(''));
            return JSON.parse(jsonPayload);
        } catch (e) {
            console.error('Error parsing JWT:', e);
            return null;
        }
    }

    isTokenExpired(tokenData) {
        if (!tokenData || !tokenData.exp) return true;
        const expirationTime = tokenData.exp * 1000; // Convert to milliseconds
        return Date.now() >= expirationTime;
    }

    authenticateUser() {
        this.userId = localStorage.getItem('userId');
        this.token = localStorage.getItem('idToken');
        console.log('User ID:', this.userId);
        if (!this.userId || !this.token) {
            window.location.href = 'index.html';
            return;
        }

        // Check token expiration
        const tokenData = this.parseJwt(this.token);
        if (this.isTokenExpired(tokenData)) {
            this.logout();
            return;
        }

        this.fetchChatHistory();
        this.newSession();
    }

    getAuthHeaders() {
        return {
            'Authorization': `Bearer ${this.token}`,
            'Content-Type': 'application/json'
        };
    }

    async fetchChatHistory(useCache = true) {
        if (useCache && this.cachedSessions) {
            console.log('Using cached sessions');
            this.renderSessions(this.cachedSessions);
            return;
        }

        try {
            Swal.fire({
                title: 'Loading chat history...',
                allowOutsideClick: false,
                didOpen: () => {
                    Swal.showLoading();
                }
            });

            const response = await fetch(`${this.API_URL}/api/chat/history?userId=${this.userId}`, {
                credentials: 'include',
                headers: {
                    ...this.getAuthHeaders(),
                }
            });
            
            const sessions = await response.json();
            this.cachedSessions = sessions;
            this.renderSessions(sessions);
            Swal.close();
        } catch (error) {
            console.error('Error fetching chat history:', error);
            Swal.fire({
                icon: 'error',
                title: 'Oops...',
                text: 'Failed to load chat history. Please try again later.',
            });
        }
    }

    renderSessions(sessions) {
        this.elements.sessionList.innerHTML = sessions.map(session => 
            `<div class="session" data-session-id="${session.sessionId}">
                ${session.sessionName || 'Unnamed Session'}
                <span class="delete-session-btn" data-session-id="${session.sessionId}">
                    🗑️
                </span>
            </div>`
        ).join('');

        document.querySelectorAll('.session').forEach(el => {
            el.addEventListener('click', (e) => {
                const sessionId = e.target.dataset.sessionId || e.target.closest('.session').dataset.sessionId;
                this.loadSession(sessionId);
            });
        });

        document.querySelectorAll('.delete-session-btn').forEach(btn => {
            btn.addEventListener('click', (e) => {
                e.stopPropagation(); // Prevent triggering the session click
                const sessionId = e.target.dataset.sessionId || e.target.closest('.delete-session-btn').dataset.sessionId;
                this.deleteSession(sessionId);
            });
        });
    }

    async loadSession(sessionId) {
        this.currentSessionId = sessionId;
        sessionStorage.setItem('currentSessionId', sessionId);

        if (this.cachedMessages[sessionId]) {
            this.renderMessages(this.cachedMessages[sessionId]);
            return;
        }

        try {
            Swal.fire({
                title: 'Loading messages...',
                allowOutsideClick: false,
                didOpen: () => {
                    Swal.showLoading();
                }
            });

            const response = await fetch(`${this.API_URL}/api/chat/sessions/${this.userId}/${sessionId}`, {
                credentials: 'include',
                headers: {
                    ...this.getAuthHeaders(),
                }
            });
            const session = await response.json();
            this.cachedMessages[sessionId] = session.messages;
            this.renderMessages(session.messages);
            Swal.close();
        } catch (error) {
            console.error('Error loading session:', error);
            Swal.fire({
                icon: 'error',
                title: 'Oops...',
                text: 'Failed to load chat session. Please try again later.',
            });
        }
    }

    renderMessages(messages) {
        this.elements.messages.innerHTML = '';
        messages.forEach(msg => {
            this.elements.messages.innerHTML += `<div class="${msg.isUserMessage ? 'user-msg' : 'ai-msg'}">
                ${msg.text}
            </div>`;
        });
    }

    initEventListeners() {
        this.elements.sendBtn.addEventListener('click', () => this.sendMessage());
        this.elements.messageInput.addEventListener('keypress', (e) => {
            if (e.key === 'Enter') this.sendMessage();
        });
        
        document.getElementById('new-message-btn').addEventListener('click', () => this.newSession());
        document.getElementById('logout-btn').addEventListener('click', () => this.logout());
    }

    async newSession() {
        try {
            sessionStorage.removeItem('currentSessionId');
            this.currentSessionId = null;
            this.elements.messages.innerHTML = '<div class="ai-msg">Hello, how can I help you today?</div>';
            this.elements.messageInput.value = '';

            this.fetchChatHistory(false);
        } catch (error) {
            console.error('Failed to create new session', error);
        }
    }

    async deleteSession(sessionId) {
        const result = await Swal.fire({
            title: 'Are you sure?',
            text: "You won't be able to revert this!",
            icon: 'warning',
            showCancelButton: true,
            confirmButtonColor: '#3085d6',
            cancelButtonColor: '#d33',
            confirmButtonText: 'Yes, delete it!'
        });
        
        if (!result.isConfirmed) {
            return;
        }

        try {
            Swal.fire({
                title: 'Deleting session...',
                allowOutsideClick: false,
                didOpen: () => {
                    Swal.showLoading();
                }
            });

            const response = await fetch(`${this.API_URL}/api/chat/sessions/${this.userId}/${sessionId}`, {
                method: 'DELETE',
                credentials: 'include',
                headers: {
                    ...this.getAuthHeaders(),
                }
            });

            if (response.ok) {
                delete this.cachedMessages[sessionId];
                await this.fetchChatHistory(false);
                
                Swal.fire({
                    icon: 'success',
                    title: 'Deleted!',
                    text: 'Your chat session has been deleted.',
                    timer: 1500
                });
            } else {
                throw new Error('Failed to delete session');
            }
        } catch (error) {
            console.error('Error deleting session:', error);
            Swal.fire({
                icon: 'error',
                title: 'Oops...',
                text: 'Failed to delete chat session. Please try again later.',
            });
        }
    }

    async sendMessage() {
        const messageText = this.elements.messageInput.value.trim();
        if (!messageText) return;

        try {
            this.elements.messageInput.value = '';
            this.elements.messageInput.style.height = 'auto';
            
            const body = {
                "userId": this.userId,
                "userMessage": messageText,
                ...(this.currentSessionId && { "sessionId": this.currentSessionId })
            };

            let loadingMessage = Swal.mixin({
                toast: true,
                position: 'top-end',
                showConfirmButton: false,
                timer: 1000,
                timerProgressBar: true
            });

            loadingMessage.fire({
                icon: 'info',
                title: 'Sending message...'
            });

            const response = await fetch(`${this.API_URL}/api/chat`, {
                method: 'POST',
                credentials: 'include',
                headers: {
                    ...this.getAuthHeaders(),
                },
                body: JSON.stringify(body),
            });
            if (response.status === 401) {
                // Token expired or invalid
                this.logout();
                return;
            }

            const data = await response.json();
            console.log(data);

            this.currentSessionId = data.sessionId;
            sessionStorage.setItem('currentSessionId', data.sessionId);
            
            if (!this.cachedMessages[this.currentSessionId]) {
                this.cachedMessages[this.currentSessionId] = [];
            }
            
            this.cachedMessages[this.currentSessionId].push(
                { text: messageText, isUserMessage: true },
                { text: data.response, isUserMessage: false }
            );

            this.elements.messages.innerHTML += `<div class="user-msg">${messageText}</div>`;
            this.elements.messages.innerHTML += `<div class="ai-msg">${data.response}</div>`;

            this.fetchChatHistory(false);
        } catch (error) {
            console.error('Failed to send message', error);
        }
    }

    logout() {
        const confirmLogout = confirm('Are you sure you want to log out? All unsaved chat sessions will be cleared.');
        
        if (!confirmLogout) {
            return;
        }

        try {
            localStorage.removeItem('userId');
            localStorage.removeItem('idToken');
            sessionStorage.clear();
            
            window.location.href = 'index.html';
        } catch (error) {
            console.error('Logout error:', error);
            alert('An error occurred while logging out. Please try again.');
        }
    }
}

document.addEventListener('DOMContentLoaded', () => new ChatApp());