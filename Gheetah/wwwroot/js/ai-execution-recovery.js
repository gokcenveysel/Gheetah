'use strict';

document.addEventListener('DOMContentLoaded', async () => {
    const pendingSessionId = sessionStorage.getItem('ai_pending_session');
    if (!pendingSessionId || !window.AI_PROJECT_ID) return;

    try {
        const res = await fetch(`/AiExecution/GetStatus/${pendingSessionId}`);
        if (!res.ok) {
            sessionStorage.removeItem('ai_pending_session');
            return;
        }
        const status = await res.json();

        if (status.status === 'running' && status.isRecoverable) {
            const shouldReconnect = confirm(
                'You have an active AI execution session. Would you like to reconnect to see the live output?'
            );
            if (shouldReconnect) {
                await AiExecution.reconnect(pendingSessionId);
            }
        } else {
            sessionStorage.removeItem('ai_pending_session');
        }
    } catch {
        sessionStorage.removeItem('ai_pending_session');
    }
});
