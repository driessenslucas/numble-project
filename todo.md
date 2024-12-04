TODO:

    -[] add authentication redirect (check if the redirect is valid... look up how to do this)
    -[] add chat with history enabled (using the openai function for this)
        -[] Will need to add the ability to turn this on and off (to save on cost)
        -[] Adjust 'ChatMessage.cs' and 'ChatRequest.cs' to include history of the chat (from session)
        -[] Adjust '.ChatMessage.cs' and 'ChatRequest.cs' with a bool to enable history (or do it in chat controller)
        -[] Adjust 'ChatController.cs' to include history of the chat (from session if enabled)
        -[] Adjust frontend to add a toggle for history (use a checkbox)
        -[] Adjust app.js to include history of the chat (from session if enabled)


    -[] FIx swal css