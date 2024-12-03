class ChatApp {
    constructor() {
        this.userId = null;
        this.currentSessionId = null;
        this.elements = {
            messageInput: document.getElementById('message-input'),
            sendBtn: document.getElementById('send-btn'),
            messages: document.getElementById('messages'),
            sessionList: document.getElementById('session-list')
        };
        this.API_URL = 'http://localhost:5000';

        this.initEventListeners();
        this.authenticateUser();
    }

    authenticateUser() {
        // Simulate JWT token parsing
        const token = localStorage.getItem('token');
        if (token) {
            // In real app, use jwt-decode or similar
            this.userId = JSON.parse(atob(token.split('.')[1])).sub;
            this.fetchChatHistory();
        }
    }

    async fetchChatHistory() {
        try {
            const response = await fetch(`${this.API_URL}/api/chat/history?userId=${this.userId}`);
            const sessions = await response.json();
            this.renderSessions(sessions);
        } catch (error) {
            console.error('Failed to fetch chat history', error);
        }
    }

    renderSessions(sessions) {
        this.elements.sessionList.innerHTML = sessions.map(session => 
            `<div class="session" data-session-id="${session.sessionId}">
                ${session.SessionName}
            </div>`
        ).join('');

        document.querySelectorAll('.session').forEach(el => {
            el.addEventListener('click', (e) => {
                const sessionId = e.target.dataset.sessionId;
                this.loadSession(sessionId);
            });
        });
    }

    async loadSession(sessionId) {
        try {
            const response = await fetch(`${this.API_URL}/api/chat/sessions/${this.userId}/${sessionId}`);
            const session = await response.json();
            this.renderMessages(session.Messages);
            this.currentSessionId = sessionId;
        } catch (error) {
            console.error('Failed to load session', error);
        }
    }

    renderMessages(messages) {
        this.elements.messages.innerHTML = messages.map(msg => 
            `<div class="${msg.IsUserMessage ? 'user-msg' : 'ai-msg'}">
                ${msg.Text}
            </div>`
        ).join('');
    }

    initEventListeners() {
        this.elements.sendBtn.addEventListener('click', () => this.sendMessage());
        this.elements.messageInput.addEventListener('keypress', (e) => {
            if (e.key === 'Enter') this.sendMessage();
        });
    }

    async sendMessage() {
        const messageText = this.elements.messageInput.value.trim();
        if (!messageText) return;

        try {
            const response = await fetch(`${this.API_URL}/api/chat`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    userId: this.userId,
                    userMessage: messageText
                })
            });
            const data = await response.json();
            
            // Refresh chat history to show new session
            this.fetchChatHistory();
            
            // Clear input
            this.elements.messageInput.value = '';
        } catch (error) {
            console.error('Failed to send message', error);
        }
    }
}

// Initialize app when DOM is ready
document.addEventListener('DOMContentLoaded', () => new ChatApp());
