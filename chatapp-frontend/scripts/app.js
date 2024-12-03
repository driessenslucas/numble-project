// class ChatApp {
//     constructor() {
//         this.userId = null;
//         this.currentSessionId = null;
//         this.cachedSessions = null; // Cache for sessions
//         this.cachedMessages = {}; // Cache for messages per session
//         this.elements = {
//             messageInput: document.getElementById('message-input'),
//             sendBtn: document.getElementById('send-btn'),
//             messages: document.getElementById('messages'),
//             sessionList: document.getElementById('session-list')
//         };
//         this.API_URL = 'https://localhost:5001';
//         this.initEventListeners();
//         this.authenticateUser();
//         this.newSession();
//     }

//     authenticateUser() {
//         // get userId from local storage
//         this.userId = localStorage.getItem('userId');
//         console.log('User ID:', this.userId);
//         if (!this.userId) {
//             // redirect to auth.html
//             console.log('Redirecting to auth.html...');
//             window.location.href = 'index.html';
//         }

//         // fetch chat history
//         this.fetchChatHistory();
//     }

//     async fetchChatHistory(useCache = true) {
//         if (useCache && this.cachedSessions) {
//             console.log('Using cached sessions');
//             this.renderSessions(this.cachedSessions);
//             return;
//         }

//         try {
//             const response = await fetch(`${this.API_URL}/api/chat/history?userId=${this.userId}`);
//             const sessions = await response.json();
//             console.log(sessions);
//             this.cachedSessions = sessions; // Cache the sessions in sessionStorage
//             sessionStorage.setItem('sessions', JSON.stringify(sessions));
//             this.renderSessions(sessions);
//         } catch (error) {
//             console.error('Failed to fetch chat history', error);
//         }
//     }

//     renderSessions(sessions) {
//         this.elements.sessionList.innerHTML = sessions.map(session => 
//             `<div class="session" data-session-id="${session.sessionId}">
//                 ${session.sessionName}
//             </div>`
//         ).join('');

//         document.querySelectorAll('.session').forEach(el => {
//             el.addEventListener('click', (e) => {
//                 const sessionId = e.target.dataset.sessionId;
//                 this.loadSession(sessionId);
//             });
//         });
//     }

//     async loadSession(sessionId) {
//         // Save sessionId to sessionStorage after loading a session
//         sessionStorage.setItem('currentSessionId', sessionId);

//         // Check if cached messages for this session are available
//         if (this.cachedMessages[sessionId]) {
//             console.log('Using cached messages for session:', sessionId);
//             this.renderMessages(this.cachedMessages[sessionId]);
//             this.currentSessionId = sessionId;
//             return;
//         }

//         // Check sessionStorage for cached messages
//         const cachedMessages = sessionStorage.getItem(`messages_${sessionId}`);
//         if (cachedMessages) {
//             console.log('Using cached messages from sessionStorage');
//             this.cachedMessages[sessionId] = JSON.parse(cachedMessages);
//             this.renderMessages(this.cachedMessages[sessionId]);
//             this.currentSessionId = sessionId;
//             return;
//         }

//         // Fetch messages from API if not in cache
//         try {
//             const response = await fetch(`${this.API_URL}/api/chat/sessions/${this.userId}/${sessionId}`);
//             const session = await response.json();
//             this.cachedMessages[sessionId] = session.messages; // Cache the messages
//             sessionStorage.setItem(`messages_${sessionId}`, JSON.stringify(session.messages));
//             this.renderMessages(session.messages);
//             this.currentSessionId = sessionId;
//             console.log(session);
//         } catch (error) {
//             console.error('Failed to load session', error);
//         }
//     }

//     renderMessages(messages) {
//         console.log(messages);
//         // clear chat history
//         this.elements.messages.innerHTML = '';
//         messages.forEach(msg => {
//             this.elements.messages.innerHTML += `<div class="${msg.isUserMessage ? 'user-msg' : 'ai-msg'}">
//                 ${msg.text}
//             </div>`;
//         });
//     }

//     initEventListeners() {
//         this.elements.sendBtn.addEventListener('click', () => this.sendMessage());
//         this.elements.messageInput.addEventListener('keypress', (e) => {
//             if (e.key === 'Enter') this.sendMessage();
//         });
        
//         // Add event listener for new session button
//         document.getElementById('new-message-btn').addEventListener('click', () => this.newSession());
        
//         // Add event listener for logout button
//         document.getElementById('logout-btn').addEventListener('click', () => this.logout());
//     }

//     newSession() {
//         // TODO: implement
//         // clear chat history
//         this.elements.messages.innerHTML = '';
//         this.currentSessionId = null;
        
//         // clear input
//         this.elements.messageInput.value = '';
//         // Show default message
//         this.elements.messages.innerHTML = '<div class="ai-msg">Hello, how can I help you today?</div>';
        
//     }

//     async sendMessage() {
//         const messageText = this.elements.messageInput.value.trim();
//         if (!messageText) return;

//         // Retrieve sessionId from sessionStorage
//         const sessionId = sessionStorage.getItem('currentSessionId');
//         console.log('Sending message:', messageText);

//         try {
//             const response = await fetch(`${this.API_URL}/api/chat`, {
//                 method: 'POST',
//                 headers: { 'Content-Type': 'application/json' },
//                 body: JSON.stringify({
//                     userId: this.userId,
//                     userMessage: messageText,
//                     sessionId: sessionId // Include sessionId in the request body
//                 })
//             });
//             const data = await response.json();
            
//             console.log(data);
//             // Show message
//             this.elements.messages.innerHTML += `<div class="user-msg">${messageText}</div>`;
//             this.elements.messages.innerHTML += `<div class="ai-msg">${data.response}</div>`;

//             // Refresh chat history to show new session
//             this.fetchChatHistory(false);
            
//             // load this session
//             this.loadSession(sessionId);

//             // Clear input
//             this.elements.messageInput.value = '';
//         } catch (error) {
//             console.error('Failed to send message', error);
//         }
//     }

//     logout() {
//         localStorage.removeItem('userId');
//         sessionStorage.clear();  // Clear cached sessions and messages on logout
//         window.location.href = 'index.html';
//     }
// }

// // Initialize app when DOM is ready
// document.addEventListener('DOMContentLoaded', () => new ChatApp());


class ChatApp {
    constructor() {
        this.userId = null;
        this.currentSessionId = null;
        this.cachedSessions = null;
        this.cachedMessages = {};
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

    authenticateUser() {
        this.userId = localStorage.getItem('userId');
        console.log('User ID:', this.userId);
        if (!this.userId) {
            window.location.href = 'index.html';
            return;
        }

        this.fetchChatHistory();
        this.newSession(); // Automatically start a new session
    }

    async fetchChatHistory(useCache = true) {
        if (useCache && this.cachedSessions) {
            console.log('Using cached sessions');
            this.renderSessions(this.cachedSessions);
            return;
        }

        try {
            const response = await fetch(`${this.API_URL}/api/chat/history?userId=${this.userId}`);
            const sessions = await response.json();
            this.cachedSessions = sessions;
            this.renderSessions(sessions);
        } catch (error) {
            console.error('Failed to fetch chat history', error);
        }
    }

    renderSessions(sessions) {
        this.elements.sessionList.innerHTML = sessions.map(session => 
            `<div class="session" data-session-id="${session.sessionId}">
                ${session.sessionName || 'Unnamed Session'}
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
        this.currentSessionId = sessionId;
        sessionStorage.setItem('currentSessionId', sessionId);

        // Check cache first
        if (this.cachedMessages[sessionId]) {
            this.renderMessages(this.cachedMessages[sessionId]);
            return;
        }

        try {
            const response = await fetch(`${this.API_URL}/api/chat/sessions/${this.userId}/${sessionId}`);
            const session = await response.json();
            this.cachedMessages[sessionId] = session.messages;
            this.renderMessages(session.messages);
            console.log(session);
        } catch (error) {
            console.error('Failed to load session', error);
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
        // Create a new session on the server
        try {
            // delete sessionId from sessionStorage
            sessionStorage.removeItem('currentSessionId');
            this.currentSessionId = null;
            // Clear UI
            this.elements.messages.innerHTML = '<div class="ai-msg">Hello, how can I help you today?</div>';
            this.elements.messageInput.value = '';

            // Refresh sessions list
            this.fetchChatHistory(false);
        } catch (error) {
            console.error('Failed to create new session', error);
        }
    }

    async sendMessage() {
        const messageText = this.elements.messageInput.value.trim();
        if (!messageText) return;

        try {
            // if this.sessionId is null, don't include it in the request
            console.log('Sending message:', messageText);
            console.log(this.currentSessionId);
            
            const body = {
                "userId": this.userId,
                "userMessage": messageText,
                ...(this.currentSessionId && { "sessionId": this.currentSessionId })
            };
            const response = await fetch(`${this.API_URL}/api/chat`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(body),
            });
            const data = await response.json();

            console.log(data);

            this.currentSessionId = data.sessionId;
            // Save sessionId to sessionStorage
            sessionStorage.setItem('currentSessionId', data.sessionId);
            
            // Update cached messages for the current session
            if (!this.cachedMessages[this.currentSessionId]) {
                this.cachedMessages[this.currentSessionId] = [];
            }
            
            this.cachedMessages[this.currentSessionId].push(
                { text: messageText, isUserMessage: true },
                { text: data.response, isUserMessage: false }
            );

            // Render messages
            this.elements.messages.innerHTML += `<div class="user-msg">${messageText}</div>`;
            this.elements.messages.innerHTML += `<div class="ai-msg">${data.response}</div>`;

            // Refresh chat history
            this.fetchChatHistory(false);

            // Clear input
            this.elements.messageInput.value = '';
        } catch (error) {
            console.error('Failed to send message', error);
        }
    }

    logout() {
        localStorage.removeItem('userId');
        sessionStorage.clear();
        window.location.href = 'index.html';
    }
}

// Initialize app when DOM is ready
document.addEventListener('DOMContentLoaded', () => new ChatApp());