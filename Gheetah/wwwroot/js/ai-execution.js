'use strict';

const AiExecution = (() => {
    let connection = null;
    let activeSessionId = null;
    const token = () => document.querySelector('input[name="__RequestVerificationToken"]')?.value;

    function buildConnection() {
        return new signalR.HubConnectionBuilder()
            .withUrl('/aiExecutionHub')
            .withAutomaticReconnect({
                nextRetryDelayInMilliseconds: retryContext => {
                    return Math.min(retryContext.elapsedMilliseconds * 2, 10000);
                }
            })
            .build();
    }

    function appendTerminal(text) {
        const terminal = document.getElementById('aiTerminal');
        if (!terminal) return;
        terminal.textContent += text + '\n';
        terminal.scrollTop = terminal.scrollHeight;
    }

    function clearTerminal() {
        const terminal = document.getElementById('aiTerminal');
        if (terminal) terminal.textContent = '';
    }

    function updateProgress(current, total) {
        const bar = document.getElementById('execProgressBar');
        const label = document.getElementById('execStepLabel');
        if (!bar || !label) return;
        const pct = total > 0 ? Math.round((current / total) * 100) : 0;
        bar.style.width = pct + '%';
        label.textContent = `Step ${current}/${total}`;
    }

    function setStatus(status, cssClass) {
        const badge = document.getElementById('execStatusBadge');
        const spinner = document.getElementById('execSpinner');
        if (!badge) return;
        badge.textContent = status;
        badge.className = `badge ${cssClass} ms-2`;
        if (spinner) spinner.style.display = status === 'Running' ? '' : 'none';
    }

    async function connectToSession(sessionId) {
        activeSessionId = sessionId;
        sessionStorage.setItem('ai_pending_session', sessionId);

        if (!connection) connection = buildConnection();

        connection.on('ReceiveAiOutput', chunk => appendTerminal(chunk));
        connection.on('ReceiveAiCompletion', resultJson => handleCompletion(JSON.parse(resultJson)));
        connection.on('ReceiveAiError', msg => handleError(msg));
        connection.on('ReceiveSessionStatus', statusJson => {
            const s = JSON.parse(statusJson);
            updateProgress(s.currentStep || 0, s.totalSteps || 0);
        });

        connection.onreconnecting(() => setStatus('Reconnecting', 'bg-warning-lt'));
        connection.onreconnected(() => {
            setStatus('Running', 'bg-purple-lt');
            connection.invoke('SubscribeToSession', sessionId).catch(console.error);
        });
        connection.onclose(() => {
            if (activeSessionId) setStatus('Disconnected', 'bg-danger-lt');
        });

        try {
            if (connection.state === signalR.HubConnectionState.Disconnected) {
                await connection.start();
            }
            await connection.invoke('SubscribeToSession', sessionId);
        } catch (err) {
            handleError('Connection failed: ' + err.message);
        }
    }

    function handleCompletion(result) {
        setStatus(result.status || 'Done', result.status === 'Passed' ? 'bg-success-lt' : 'bg-danger-lt');
        updateProgress(result.totalSteps || 0, result.totalSteps || 0);
        sessionStorage.removeItem('ai_pending_session');

        const footer = document.getElementById('execFooter');
        if (footer) footer.style.display = '';

        const summary = document.getElementById('execResultSummary');
        if (summary) {
            const dur = result.totalDurationMs ? `${(result.totalDurationMs / 1000).toFixed(1)}s` : '';
            summary.textContent = `${result.status} in ${dur}`;
        }

        const cancelBtn = document.getElementById('cancelExecBtn');
        if (cancelBtn) cancelBtn.style.display = 'none';

        if (window.ScenarioManager) ScenarioManager.refresh();
    }

    function handleError(msg) {
        setStatus('Error', 'bg-danger-lt');
        appendTerminal('\n[ERROR] ' + msg);
        sessionStorage.removeItem('ai_pending_session');
        const cancelBtn = document.getElementById('cancelExecBtn');
        if (cancelBtn) cancelBtn.style.display = 'none';
    }

    return {
        async start(projectId, scenarioId, agentId) {
            clearTerminal();
            setStatus('Running', 'bg-purple-lt');
            updateProgress(0, 0);
            document.getElementById('cancelExecBtn').style.display = '';
            const footer = document.getElementById('execFooter');
            if (footer) footer.style.display = 'none';

            new bootstrap.Modal(document.getElementById('aiExecutionModal')).show();

            try {
                const res = await fetch('/AiExecution/Execute', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token() },
                    body: JSON.stringify({ projectId, scenarioId, agentId })
                });
                const result = await res.json();
                if (!result.success) throw new Error(result.message);
                await connectToSession(result.sessionId);
            } catch (err) {
                handleError(err.message);
            }
        },

        async cancel() {
            if (!activeSessionId) return;
            try {
                await fetch('/AiExecution/Cancel', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token() },
                    body: JSON.stringify(activeSessionId)
                });
                setStatus('Cancelled', 'bg-secondary-lt');
                sessionStorage.removeItem('ai_pending_session');
            } catch (err) {
                showCustomToast('danger', err.message);
            }
        },

        async reconnect(sessionId) {
            clearTerminal();
            appendTerminal('[Reconnecting to session ' + sessionId + '...]');
            setStatus('Reconnecting', 'bg-warning-lt');
            new bootstrap.Modal(document.getElementById('aiExecutionModal')).show();
            await connectToSession(sessionId);
        }
    };
})();

function executeScenario() {
    const scenario = window.ScenarioManager?.getSelectedScenario();
    if (!scenario) { showCustomToast('warning', 'No scenario selected.'); return; }
    AiExecution.start(window.AI_PROJECT_ID, scenario.id, null);
}

function cancelExecution() { AiExecution.cancel(); }

function onExecutionModalClosed() {
    if (window.ScenarioManager) ScenarioManager.refresh();
}
