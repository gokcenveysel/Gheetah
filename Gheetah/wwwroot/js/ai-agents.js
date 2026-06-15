'use strict';

const PROVIDER_CONFIG = {
    Claude: {
        hint: 'Claude Computer Use — advanced browser automation, screenshot capture, and file system access via Anthropic API.',
        apiKeyPlaceholder: 'sk-ant-api03-...',
        defaultEndpoint: 'https://api.anthropic.com/v1',
        endpointRequired: false,
        models: ['claude-opus-4-8', 'claude-sonnet-4-6', 'claude-haiku-4-5-20251001'],
        defaultModel: 'claude-sonnet-4-6',
        hasModel: true,
        defaultCapabilities: ['BrowserAutomation', 'ScreenshotCapture', 'FormInteraction', 'FileSystem', 'CodeExecution'],
        extraFields: []
    },
    OpenAI: {
        hint: 'OpenAI Operator — web browsing and form interaction. Optionally provide an Organization ID for enterprise accounts.',
        apiKeyPlaceholder: 'sk-...',
        defaultEndpoint: 'https://api.openai.com/v1',
        endpointRequired: false,
        models: ['gpt-4o', 'gpt-4o-mini', 'gpt-4-turbo', 'o1-preview'],
        defaultModel: 'gpt-4o',
        hasModel: true,
        defaultCapabilities: ['BrowserAutomation', 'FormInteraction', 'APITesting'],
        extraFields: ['orgId']
    },
    Gemini: {
        hint: 'Google Gemini — multimodal reasoning with vision, browser automation, and API testing capabilities.',
        apiKeyPlaceholder: 'AIzaSy...',
        defaultEndpoint: 'https://generativelanguage.googleapis.com/v1beta',
        endpointRequired: false,
        models: ['gemini-2.0-flash-exp', 'gemini-1.5-pro', 'gemini-1.5-flash'],
        defaultModel: 'gemini-2.0-flash-exp',
        hasModel: true,
        defaultCapabilities: ['BrowserAutomation', 'ScreenshotCapture', 'FormInteraction', 'APITesting'],
        extraFields: []
    },
    Grok: {
        hint: 'xAI Grok — API testing and code execution. Focused on reasoning tasks rather than browser automation.',
        apiKeyPlaceholder: 'xai-...',
        defaultEndpoint: 'https://api.x.ai/v1',
        endpointRequired: false,
        models: ['grok-2-latest', 'grok-2-vision'],
        defaultModel: 'grok-2-latest',
        hasModel: true,
        defaultCapabilities: ['APITesting', 'CodeExecution'],
        extraFields: []
    },
    MCP: {
        hint: 'MCP Server — capabilities depend entirely on the server\'s exposed tools. Set the server URL and transport type, then select which capabilities this server provides.',
        apiKeyPlaceholder: 'Bearer token (or leave empty if no auth required)',
        defaultEndpoint: 'http://localhost:3000/mcp',
        endpointRequired: true,
        models: [],
        defaultModel: '',
        hasModel: false,
        defaultCapabilities: [],
        extraFields: ['transport']
    },
    Custom: {
        hint: 'Custom Agent — OpenAI-compatible endpoint. Define all settings manually based on your provider\'s API.',
        apiKeyPlaceholder: 'API key or auth token',
        defaultEndpoint: '',
        endpointRequired: true,
        models: [],
        defaultModel: '',
        hasModel: true,
        defaultCapabilities: [],
        extraFields: []
    },
    Mock: {
        hint: 'Mock Agent — for UI testing only. No API key or endpoint needed. Returns deterministic fake Gherkin output without any real API calls.',
        apiKeyPlaceholder: '(not required)',
        defaultEndpoint: '',
        endpointRequired: false,
        models: [],
        defaultModel: '',
        hasModel: false,
        defaultCapabilities: ['BrowserAutomation', 'FormInteraction'],
        extraFields: []
    }
};

function applyProviderUI(provider, resetValues) {
    const cfg = PROVIDER_CONFIG[provider] || PROVIDER_CONFIG.Custom;

    // Hint banner
    const hint = document.getElementById('providerHint');
    hint.textContent = cfg.hint;
    hint.className = 'alert alert-purple py-2 small mb-0';

    // API Key: placeholder + optional label
    document.getElementById('agentApiKey').placeholder = cfg.apiKeyPlaceholder;
    const apiKeyOptional = ['MCP', 'Custom', 'Mock'].includes(provider);
    document.getElementById('apiKeyLabel').innerHTML =
        'API Key' + (apiKeyOptional ? ' <span class="text-muted fw-normal">(optional)</span>' : ' <span class="text-danger">*</span>');

    // Endpoint: required indicator + default value
    const endpointInput = document.getElementById('agentEndpoint');
    endpointInput.required = cfg.endpointRequired;
    document.getElementById('endpointLabel').innerHTML =
        'API Endpoint' + (cfg.endpointRequired
            ? ' <span class="text-danger">*</span>'
            : ' <span class="text-muted fw-normal">(optional)</span>');
    if (resetValues) {
        endpointInput.value = cfg.defaultEndpoint;
    }

    // Model field: show/hide + populate datalist
    const modelRow = document.getElementById('modelFieldRow');
    const modelInput = document.getElementById('agentModel');
    const modelList = document.getElementById('agentModelSuggestions');
    modelRow.style.display = cfg.hasModel ? '' : 'none';
    if (cfg.hasModel) {
        modelList.innerHTML = cfg.models.map(m => `<option value="${m}">`).join('');
        if (resetValues) {
            modelInput.value = cfg.defaultModel;
        }
    } else if (resetValues) {
        modelInput.value = '';
    }

    // Capabilities: check defaults on provider switch
    if (resetValues) {
        document.querySelectorAll('.capability-cb').forEach(cb => {
            cb.checked = cfg.defaultCapabilities.includes(cb.value);
        });
    }

    // Extra fields
    document.getElementById('extraOrgId').style.display = cfg.extraFields.includes('orgId') ? '' : 'none';
    document.getElementById('extraTransport').style.display = cfg.extraFields.includes('transport') ? '' : 'none';
    if (resetValues) {
        document.getElementById('extraOrgIdInput').value = '';
        document.getElementById('extraTransportSelect').value = 'HTTP';
    }
}

const AiAgents = (() => {
    let agents = [];
    const token = () => document.querySelector('input[name="__RequestVerificationToken"]').value;

    async function loadAgents() {
        try {
            const res = await fetch('/AiAgents/GetAll');
            agents = await res.json();
            renderTable(agents);
        } catch (err) {
            showCustomToast('danger', 'Failed to load AI agents: ' + err.message);
        }
    }

    const PROVIDER_BADGE = {
        Claude:  'bg-purple-lt text-purple',
        OpenAI:  'bg-green-lt text-green',
        Gemini:  'bg-blue-lt text-blue',
        Grok:    'bg-teal-lt text-teal',
        MCP:     'bg-orange-lt text-orange',
        Custom:  'bg-secondary-lt text-secondary',
        Mock:    'bg-yellow-lt text-yellow',
    };

    const PROVIDER_LABEL = {
        Claude:  'Claude',
        OpenAI:  'OpenAI',
        Gemini:  'Gemini',
        Grok:    'Grok',
        MCP:     'MCP Server',
        Custom:  'Custom',
        Mock:    'Mock',
    };

    function statusCell(a) {
        const lastCheck = a.lastHealthCheckDate
            ? `<div class="text-muted mt-1" style="font-size:.7rem">${new Date(a.lastHealthCheckDate).toLocaleString()}</div>`
            : '';

        if (!a.isEnabled) {
            return `<span class="status status-secondary">
                        <span class="status-dot"></span>Disabled
                    </span>`;
        }
        if (!a.lastHealthCheckStatus) {
            return `<span class="status status-secondary">
                        <span class="status-dot"></span>Not tested
                    </span>`;
        }
        if (a.lastHealthCheckStatus === 'OK') {
            return `<div>
                        <span class="status status-green">
                            <span class="status-dot status-dot-animated"></span>Online
                        </span>
                        ${lastCheck}
                    </div>`;
        }
        return `<div>
                    <span class="status status-red">
                        <span class="status-dot"></span>Offline
                    </span>
                    ${lastCheck}
                </div>`;
    }

    function renderTable(data) {
        const tbody = document.getElementById('agentsTableBody');
        if (!data.length) {
            tbody.innerHTML = `<tr><td colspan="5" class="text-center text-muted py-4">No AI agents configured. Click "Add Agent" to get started.</td></tr>`;
            return;
        }
        tbody.innerHTML = data.map(a => `
            <tr>
                <td>
                    <div class="fw-medium">${escHtml(a.name)}</div>
                    <div class="mt-1">
                        ${a.isDefault ? '<span class="badge bg-yellow-lt text-yellow me-1">★ Default</span>' : ''}
                        ${a.prePromptId ? '<span class="badge bg-blue-lt text-blue">Pre-prompt</span>' : ''}
                    </div>
                </td>
                <td>
                    <span class="badge ${PROVIDER_BADGE[a.providerType] || 'bg-secondary-lt'}">
                        ${escHtml(PROVIDER_LABEL[a.providerType] || a.providerType)}
                    </span>
                </td>
                <td class="text-muted small">${escHtml(a.modelName || '—')}</td>
                <td>${statusCell(a)}</td>
                <td>
                    <div class="btn-list flex-nowrap">
                        <button class="btn btn-sm btn-icon" title="Test Connection" onclick="AiAgents.testConnection('${a.id}', this)">
                            <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                <path stroke="none" d="M0 0h24v24H0z" fill="none"/>
                                <path d="M9 12l2 2l4 -4"/>
                                <path d="M12 3a9 9 0 1 0 0 18a9 9 0 0 0 0 -18"/>
                            </svg>
                        </button>
                        <button class="btn btn-sm btn-icon" title="Edit" onclick="AiAgents.openEditModal('${a.id}')">
                            <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                <path stroke="none" d="M0 0h24v24H0z" fill="none"/>
                                <path d="M7 7h-1a2 2 0 0 0 -2 2v9a2 2 0 0 0 2 2h9a2 2 0 0 0 2 -2v-1"/>
                                <path d="M20.385 6.585a2.1 2.1 0 0 0 -2.97 -2.97l-8.415 8.385v3h3l8.385 -8.415z"/>
                            </svg>
                        </button>
                        <button class="btn btn-sm btn-icon text-danger" title="Delete" onclick="AiAgents.deleteAgent('${a.id}', '${escHtml(a.name)}')">
                            <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                <path stroke="none" d="M0 0h24v24H0z" fill="none"/>
                                <path d="M4 7h16"/><path d="M10 11v6"/><path d="M14 11v6"/>
                                <path d="M5 7l1 12a2 2 0 0 0 2 2h8a2 2 0 0 0 2 -2l1 -12"/>
                                <path d="M9 7v-3a1 1 0 0 1 1 -1h4a1 1 0 0 1 1 1v3"/>
                            </svg>
                        </button>
                    </div>
                </td>
            </tr>
        `).join('');
    }

    function escHtml(str) {
        if (!str) return '';
        return String(str).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
    }

    function getFormData() {
        const caps = [...document.querySelectorAll('.capability-cb:checked')].map(c => c.value);
        const extraConfig = {};
        const orgId = document.getElementById('extraOrgIdInput').value.trim();
        if (orgId) extraConfig.organizationId = orgId;
        const transport = document.getElementById('extraTransportSelect').value;
        if (transport) extraConfig.transportType = transport;
        return {
            id: document.getElementById('agentId').value || null,
            name: document.getElementById('agentName').value.trim(),
            providerType: document.getElementById('agentProvider').value,
            apiEndpoint: document.getElementById('agentEndpoint').value.trim(),
            apiKey: document.getElementById('agentApiKey').value,
            modelName: document.getElementById('agentModel').value.trim(),
            maxConcurrentSessions: parseInt(document.getElementById('agentMaxSessions').value) || 1,
            timeoutSeconds: parseInt(document.getElementById('agentTimeout').value) || 120,
            capabilities: caps,
            isEnabled: document.getElementById('agentEnabled').checked,
            isDefault: document.getElementById('agentDefault').checked,
            extraConfig
        };
    }

    return {
        init() {
            loadAgents();
        },

        openEditModal(agentId) {
            const a = agents.find(x => x.id === agentId);
            if (!a) return;
            document.getElementById('agentModalTitle').textContent = 'Edit AI Agent';
            document.getElementById('agentId').value = a.id;
            document.getElementById('agentName').value = a.name || '';
            document.getElementById('agentProvider').value = a.providerType || 'Claude';
            document.getElementById('agentEndpoint').value = a.apiEndpoint || '';
            document.getElementById('agentApiKey').value = '';
            document.getElementById('agentModel').value = a.modelName || '';
            document.getElementById('agentMaxSessions').value = a.maxConcurrentSessions || 1;
            document.getElementById('agentTimeout').value = a.timeoutSeconds || 120;
            document.getElementById('agentEnabled').checked = a.isEnabled !== false;
            document.getElementById('agentDefault').checked = a.isDefault === true;
            document.querySelectorAll('.capability-cb').forEach(cb => {
                cb.checked = (a.capabilities || []).includes(cb.value);
            });
            const ec = a.extraConfig || {};
            document.getElementById('extraOrgIdInput').value = ec.organizationId || '';
            document.getElementById('extraTransportSelect').value = ec.transportType || 'HTTP';
            applyProviderUI(a.providerType || 'Claude', false);
            new bootstrap.Modal(document.getElementById('agentModal')).show();
        },

        async saveAgent() {
            const data = getFormData();
            if (!data.name) { showCustomToast('warning', 'Agent name is required.'); return; }
            const cfg = PROVIDER_CONFIG[data.providerType] || PROVIDER_CONFIG.Custom;
            if (cfg.endpointRequired && !data.apiEndpoint) {
                showCustomToast('warning', 'API Endpoint is required for ' + data.providerType + '.');
                return;
            }

            const btn = document.getElementById('saveAgentBtn');
            btn.disabled = true;
            btn.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span> Saving...';
            try {
                const isEdit = !!data.id;
                const res = await fetch(isEdit ? '/AiAgents/Update' : '/AiAgents/Create', {
                    method: isEdit ? 'PUT' : 'POST',
                    headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token() },
                    body: JSON.stringify(data)
                });
                const result = await res.json();
                if (!result.success) throw new Error(result.message);
                bootstrap.Modal.getInstance(document.getElementById('agentModal')).hide();
                showCustomToast('success', isEdit ? 'Agent updated.' : 'Agent created.');
                await loadAgents();
            } catch (err) {
                showCustomToast('danger', err.message);
            } finally {
                btn.disabled = false;
                btn.textContent = 'Save Agent';
            }
        },

        async deleteAgent(agentId, agentName) {
            if (!confirm(`Delete agent "${agentName}"?`)) return;
            try {
                const res = await fetch(`/AiAgents/Delete/${agentId}`, {
                    method: 'DELETE',
                    headers: { 'RequestVerificationToken': token() }
                });
                const result = await res.json();
                if (!result.success) throw new Error(result.message);
                showCustomToast('success', 'Agent deleted.');
                await loadAgents();
            } catch (err) {
                showCustomToast('danger', err.message);
            }
        },

        async testConnection(agentId, btn) {
            const original = btn.innerHTML;
            btn.disabled = true;
            btn.innerHTML = '<span class="spinner-border spinner-border-sm"></span>';
            try {
                const res = await fetch(`/AiAgents/TestConnection/${agentId}`, {
                    method: 'POST',
                    headers: { 'RequestVerificationToken': token() }
                });
                const result = await res.json();
                if (result.success) {
                    showCustomToast('success', `Connected! Latency: ${result.latencyMs}ms`);
                } else {
                    showCustomToast('warning', result.message || 'Connection failed.');
                }
                await loadAgents();
            } catch (err) {
                showCustomToast('danger', err.message);
            } finally {
                btn.disabled = false;
                btn.innerHTML = original;
            }
        }
    };
})();

function openAgentModal() {
    document.getElementById('agentModalTitle').textContent = 'Add AI Agent';
    document.getElementById('agentId').value = '';
    document.getElementById('agentName').value = '';
    document.getElementById('agentProvider').value = 'Claude';
    document.getElementById('agentApiKey').value = '';
    document.getElementById('agentMaxSessions').value = 1;
    document.getElementById('agentTimeout').value = 120;
    document.getElementById('agentEnabled').checked = true;
    document.getElementById('agentDefault').checked = false;
    applyProviderUI('Claude', true);
    new bootstrap.Modal(document.getElementById('agentModal')).show();
}

function onProviderChange() {
    applyProviderUI(document.getElementById('agentProvider').value, true);
}

function toggleApiKeyVisibility() {
    const input = document.getElementById('agentApiKey');
    input.type = input.type === 'password' ? 'text' : 'password';
}

function saveAgent() { AiAgents.saveAgent(); }

async function testAgentConnection() {
    const agentId = document.getElementById('agentId').value;
    if (!agentId) { showCustomToast('warning', 'Save the agent first before testing.'); return; }
    await AiAgents.testConnection(agentId, document.getElementById('testConnBtn'));
}

document.addEventListener('DOMContentLoaded', () => AiAgents.init());
