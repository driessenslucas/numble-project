class ChatApp {
    constructor() {
        this.userId = null;
        this.currentSessionId = null;
        this.cachedSessions = localStorage.getItem('cachedSessions') ? JSON.parse(localStorage.getItem('cachedSessions')) : null;
        this.cachedMessages = {};
        this.token = null;
        this.elements = {
            messageInput: document.getElementById('message-input'),
            sendBtn: document.getElementById('send-btn'),
            messages: document.getElementById('messages'),
            sessionList: document.getElementById('session-list')
        };
        this.API_URL = 'https://localhost:5001';
        this.initializeEventListeners();
        this.initializeProfile();
        this.authenticateUser();
        this.fetchChatHistory(true);
        this.historyToggle = document.getElementById('history-toggle');
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

            const response = await fetch(`${this.API_URL}/api/chat/history?userId=${this.userId}`, {
                credentials: 'include',
                headers: {
                    ...this.getAuthHeaders(),
                }
            });
            
            const sessions = await response.json();
            localStorage.setItem('cachedSessions', JSON.stringify(sessions));
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

        const loading = this.showLoading();
        try {
            const response = await fetch(`${this.API_URL}/api/chat/sessions/${this.userId}/${sessionId}`, {
                credentials: 'include',
                headers: {
                    ...this.getAuthHeaders(),
                }
            });
            const session = await response.json();
            this.cachedMessages[sessionId] = session.messages;
            this.renderMessages(session.messages);
        } catch (error) {
            console.error('Error loading session:', error);
            this.showToast('Failed to load chat session', 'error');
        } finally {
            this.hideLoading(loading);
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

    initializeEventListeners() {
        this.elements.sendBtn.addEventListener('click', () => this.sendMessage());
        this.elements.messageInput.addEventListener('keypress', (e) => {
            if (e.key === 'Enter') this.sendMessage();
        });
        
        document.getElementById('new-message-btn').addEventListener('click', () => this.newSession());
        document.getElementById('logout-btn').addEventListener('click', () => this.logout());
    }

    initializeProfile() {
        const profileName = document.getElementById('profile-name');
        const profileEmail = document.getElementById('profile-email');

        // load name and email from local storage
        const name = localStorage.getItem('name');
        const email = localStorage.getItem('email');

        // Set profile info
        profileName.innerHTML = email;

        // Set avatar initials
        const initialsSpan = document.querySelector('.initials');
        if (name) {
            const initials = name.split(' ')
                .map(word => word[0])
                .join('')
                .substring(0, 2);
            initialsSpan.textContent = initials;
        }

        // Add expand/collapse functionality
        const profileBox = document.querySelector('.profile-box');
        const profilePreview = document.querySelector('.profile-preview');
        const expandButton = document.querySelector('.expand-profile');

        function toggleProfileDetails() {
            profileBox.classList.toggle('expanded');
        }

        profilePreview.addEventListener('click', toggleProfileDetails);
        expandButton.addEventListener('click', (e) => {
            e.stopPropagation();
            toggleProfileDetails();
        });
    }

    async newSession() {
        try {
            sessionStorage.removeItem('currentSessionId');
            this.currentSessionId = null;

            this.elements.messages.innerHTML = '';

            // Create suggestion box
            const suggestionBox = document.createElement('div');
            suggestionBox.id = 'suggestion-box';
            suggestionBox.innerHTML = `
                <div class="suggestion suggestion-1" onclick="document.getElementById('message-input').value = 'I need help with my account'">I need help with my account</div>
                <div class="suggestion suggestion-2" onclick="document.getElementById('message-input').value = 'I want to place an order'">I want to place an order</div>
                <div class="suggestion suggestion-3" onclick="document.getElementById('message-input').value = 'I have a question about a product'">I have a question about a product</div>
            `;
            this.elements.messages.appendChild(suggestionBox);

            this.elements.messageInput.value = '';
        } catch (error) {
            console.error('Failed to create new session', error);
        }
    }

    async deleteSession(sessionId) {
        const result = await Swal.fire({
            title: 'Delete Session',
            text: "Are you sure? This can't be undone.",
            icon: 'warning',
            showCancelButton: true,
            confirmButtonColor: '#dc2626',
            cancelButtonColor: '#6b7280',
            confirmButtonText: 'Delete',
            backdrop: true, // Keep default backdrop
            scrollbarPadding: false, // Disable automatic scrollbar padding
            customClass: {
                popup: 'swal2-popup'
            }
        });
        
        if (!result.isConfirmed) {
            return;
        }

        const loading = this.showLoading();
        try {
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
                this.showToast('Session deleted successfully', 'success');
            } else {
                throw new Error('Failed to delete session');
            }
        } catch (error) {
            console.error('Error deleting session:', error);
            this.showToast('Failed to delete session', 'error');
        } finally {
            this.hideLoading(loading);
        }
    }

    getIncludeHistory() {
        return this.historyToggle.checked;
    }

    async sendMessage() {
        const messageText = this.elements.messageInput.value.trim();
        if (!messageText) return;

        try {
            // if suggestion box is present, remove it
            const suggestionBox = document.getElementById('suggestion-box');
            if (suggestionBox) {
                suggestionBox.remove();
            }

            this.elements.messageInput.value = '';
            this.elements.messageInput.style.height = 'auto';
            
            const body = {
                "userId": this.userId,
                "userMessage": messageText,
                "includeHistory": this.getIncludeHistory(),
                ...(this.currentSessionId && { "sessionId": this.currentSessionId })
            };

            console.log(body);

            this.showToast('Sending message...', 'info');

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
            this.showToast('Failed to send message', 'error');
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

    showLoading() {
        const overlay = document.createElement('div');
        overlay.className = 'loading-overlay';
        overlay.innerHTML = '<div class="loading-spinner"></div>';
        document.body.appendChild(overlay);
        return overlay;
    }

    hideLoading(overlay) {
        if (overlay && overlay.parentNode) {
            overlay.parentNode.removeChild(overlay);
        }
    }

    showToast(message, type = 'info') {
        const toast = Swal.mixin({
            toast: true,
            position: 'top-end',
            showConfirmButton: false,
            timer: 3000,
            timerProgressBar: true,
            didOpen: (toast) => {
                toast.addEventListener('mouseenter', Swal.stopTimer)
                toast.addEventListener('mouseleave', Swal.resumeTimer)
            }
        });

        toast.fire({
            icon: type,
            title: message
        });
    }
}

document.addEventListener('DOMContentLoaded', () => new ChatApp());