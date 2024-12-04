TODO:

    -[X] add authentication redirect (check if the redirect is valid... look up how to do this)
    -[X] add chat with history enabled (using the openai function for this)
        -[X] Will need to add the ability to turn this on and off (to save on cost)
        -[X] Adjust 'ChatMessage.cs' and 'ChatRequest.cs' to include history of the chat (from session)
        -[X] Adjust '.ChatMessage.cs' and 'ChatRequest.cs' with a bool to enable history (or do it in chat controller)
        -[X] Adjust 'ChatController.cs' to include history of the chat (from session if enabled)
        -[X] Adjust frontend to add a toggle for history (use a checkbox)
        -[X] Adjust app.js to include history of the chat (from session if enabled)
    -[X] FIx swal css