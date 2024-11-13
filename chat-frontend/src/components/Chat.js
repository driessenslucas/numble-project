import React, { useState, useEffect } from 'react';
import axios from 'axios';
import { Loader } from 'lucide-react';
import "./Chat.css";

function Chat() {
    const [userMessage, setUserMessage] = useState('');
    const [response, setResponse] = useState('');
    const [isLoading, setIsLoading] = useState(false);
    const [error, setError] = useState('');
    const [chatHistory, setChatHistory] = useState([]);

    const handleMessageChange = (event) => {
        setUserMessage(event.target.value);
    };

    const handleSubmit = async (event) => {
        event.preventDefault();
        setIsLoading(true);
        setError('');

        try {
            const result = await axios.post('https://localhost:5001/api/Chat/chat', { userMessage });
            setResponse(result.data.response);
            addToHistory(userMessage, result.data.response);
        } catch (err) {
            setError('Error occurred while fetching the response');
        }

        setUserMessage('');
        setIsLoading(false);
    };

    const addToHistory = (message, response) => {
        setChatHistory([...chatHistory, { message, response }]);
    };

    useEffect(() => {
        // Load chat history from server or local storage on component mount
        loadChatHistory();
    }, []);

    const loadChatHistory = async () => {
        try {
            const result = await axios.get('https://localhost:5001/api/Chat/history');
            setChatHistory(result.data);
        } catch (err) {
            setError('Error occurred while loading chat history');
        }
    };

    return (
        <div className="chatbox">
            <h1>Chat with AI</h1>
            <div className="chat-history">
                {chatHistory.map((entry, index) => (
                    <div key={index} className="chat-entry">
                        <p className="user-message">{entry.message}</p>
                        <p className="assistant-response">{entry.response}</p>
                    </div>
                ))}
            </div>
            <form onSubmit={handleSubmit}>
                <textarea
                    value={userMessage}
                    onChange={handleMessageChange}
                    placeholder="Enter your message..."
                    rows="4"
                    cols="50"
                />
                <br />
                <button type="submit" disabled={isLoading}>
                    {isLoading ? <Loader className="animate-spin h-5 w-5" /> : 'Send'}
                </button>
            </form>
            {error && <p style={{ color: 'red' }}>{error}</p>}
        </div>
    );
}

export default Chat;