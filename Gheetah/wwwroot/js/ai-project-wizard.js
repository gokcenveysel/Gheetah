'use strict';

// Requirement fields per test type — rendered dynamically in Step 2
const TEST_TYPE_REQUIREMENTS = {
    'UI Testing': [
        { id: 'ui_userRoles',    label: 'User Roles / Personas',           type: 'text',     placeholder: 'Admin, Regular User, Guest' },
        { id: 'ui_keyPages',     label: 'Key Pages / Features to Test',    type: 'textarea', placeholder: 'Login page, Dashboard, User profile, Settings panel...' },
        { id: 'ui_authRequired', label: 'Authentication Required',         type: 'select',   options: ['No', 'Yes — Username / Password', 'Yes — SSO / OAuth', 'Yes — Token-based'] },
        { id: 'ui_authDetails',  label: 'Auth Credentials or Setup Notes', type: 'textarea', placeholder: 'Username: admin@test.com  Password: Test123!\nOr describe the auth flow...', conditional: 'ui_authRequired:!No' }
    ],
    'API Testing': [
        { id: 'api_docUrl',           label: 'API Docs / Swagger URL',            type: 'text',     placeholder: 'https://api.example.com/swagger' },
        { id: 'api_authMethod',       label: 'Authentication Method',             type: 'select',   options: ['None', 'Bearer Token', 'API Key (header)', 'Basic Auth', 'OAuth 2.0'] },
        { id: 'api_authValue',        label: 'Auth Value / Token',                type: 'text',     placeholder: 'Bearer eyJhbGci...', conditional: 'api_authMethod:!None' },
        { id: 'api_keyEndpoints',     label: 'Key Endpoints to Test',             type: 'textarea', placeholder: 'POST /api/users\nGET /api/products\nPUT /api/orders/{id}\nDELETE /api/items/{id}' },
        { id: 'api_expectedResponses',label: 'Expected Responses / Behaviors',    type: 'textarea', placeholder: '201 for create, 200 for get, 400 for invalid input, 401 for unauth...' }
    ],
    'E2E Testing': [
        { id: 'e2e_userFlows',       label: 'User Flows to Test',              type: 'textarea', placeholder: '1. User registers → verifies email → logs in\n2. User browses catalog → adds to cart → completes checkout\n3. User updates profile → changes password', required: true },
        { id: 'e2e_testCredentials', label: 'Test Account Credentials',        type: 'textarea', placeholder: 'Email: test@example.com\nPassword: Test123!\nAdmin: admin@example.com / Admin456!' },
        { id: 'e2e_testData',        label: 'Preconditions / Test Data Setup', type: 'textarea', placeholder: 'At least 3 products must exist in the catalog\nTest user must already be registered\nPayment gateway sandbox mode enabled' }
    ],
    'Regression': [
        { id: 'reg_criticalPaths',  label: 'Critical Paths to Verify',             type: 'textarea', placeholder: 'Login flow, Checkout process, Report generation, Notification sending...' },
        { id: 'reg_recentChanges',  label: 'Recent Changes / Areas to Focus',      type: 'textarea', placeholder: 'New payment gateway integrated in v2.3\nUser profile page refactored\nEmail service provider changed' },
        { id: 'reg_knownIssues',    label: 'Known Flaky Areas to Skip or Flag',    type: 'text',     placeholder: 'Email delivery timing, third-party widget loading' }
    ],
    'Smoke Testing': [
        { id: 'smoke_criticalChecks',    label: 'Critical Smoke Checks',        type: 'textarea', placeholder: 'Home page loads\nLogin succeeds for admin user\nMain navigation links work\nDashboard shows data\nNo 5xx errors in console' },
        { id: 'smoke_maxResponseTime',   label: 'Max Acceptable Response Time', type: 'text',     placeholder: '3000ms' }
    ],
    'Accessibility': [
        { id: 'acc_wcagLevel',    label: 'WCAG Compliance Target',             type: 'select',   options: ['WCAG 2.1 Level A', 'WCAG 2.1 Level AA', 'WCAG 2.1 Level AAA', 'WCAG 2.2 Level AA'] },
        { id: 'acc_keyPages',     label: 'Pages / Components to Audit',        type: 'textarea', placeholder: 'Homepage, Login form, Registration, Checkout flow, Dashboard...' },
        { id: 'acc_knownIssues',  label: 'Known Issues to Verify / Track',     type: 'textarea', placeholder: 'Alt text missing on hero banner images\nFocus order broken on modal dialogs' }
    ]
};

const WizardState = {
    currentStep: 1,
    totalSteps: 4,
    projectName: '',
    description: '',
    tags: [],
    testTypes: [],
    selectedAgentId: null,
    selectedAgentName: '',
    targetUrl: '',
    browserType: 'chromium',
    requirements: {},
    generatedPrePrompt: '',
    envName: '',
    envBaseUrl: '',
    envVars: {},
    scenarioSource: 'manual'
};

const token = () => document.querySelector('input[name="__RequestVerificationToken"]')?.value;

function goToStep(step) {
    if (step > WizardState.currentStep && !validateCurrentStep()) return;
    if (step < 1 || step > WizardState.totalSteps) return;

    document.getElementById(`step${WizardState.currentStep}`).classList.add('d-none');
    WizardState.currentStep = step;
    document.getElementById(`step${step}`).classList.remove('d-none');

    updateTabStates();
    updateNavButtons();

    if (step === 2) {
        loadAgentCards();
        renderRequirementFields();
    }
    if (step === 4) {
        collectCurrentStep();
        renderReviewSummary();
        generatePrePromptPreview();
    }
}

function updateTabStates() {
    for (let i = 1; i <= WizardState.totalSteps; i++) {
        const tab = document.getElementById(`tab-step${i}`);
        tab.classList.toggle('active', i === WizardState.currentStep);
    }
}

function updateNavButtons() {
    const prevBtn = document.getElementById('prevBtn');
    const nextBtn = document.getElementById('nextBtn');
    const createBtn = document.getElementById('createBtn');

    prevBtn.style.display = WizardState.currentStep > 1 ? '' : 'none';
    const isLast = WizardState.currentStep === WizardState.totalSteps;
    nextBtn.classList.toggle('d-none', isLast);
    createBtn.classList.toggle('d-none', !isLast);
}

function nextStep() {
    if (!validateCurrentStep()) return;
    collectCurrentStep();
    goToStep(WizardState.currentStep + 1);
}

function prevStep() {
    collectCurrentStep();
    goToStep(WizardState.currentStep - 1);
}

function validateCurrentStep() {
    switch (WizardState.currentStep) {
        case 1: {
            const name = document.getElementById('projName').value.trim();
            if (!name) { showCustomToast('warning', 'Project name is required.'); return false; }
            const types = [...document.querySelectorAll('.test-type-cb:checked')];
            if (!types.length) { showCustomToast('warning', 'Select at least one Test Type.'); return false; }
            return true;
        }
        case 2: {
            if (!WizardState.selectedAgentId) { showCustomToast('warning', 'Please select an AI agent.'); return false; }
            const reqFields = document.querySelectorAll('#requirementsSection [data-required="true"]');
            for (const field of reqFields) {
                if (!field.value.trim()) {
                    showCustomToast('warning', `"${field.dataset.label}" is required.`);
                    field.focus();
                    return false;
                }
            }
            return true;
        }
        case 3: {
            if (!document.getElementById('envName').value.trim()) {
                showCustomToast('warning', 'Environment name is required.');
                return false;
            }
            return true;
        }
        default: return true;
    }
}

function collectCurrentStep() {
    switch (WizardState.currentStep) {
        case 1:
            WizardState.projectName = document.getElementById('projName').value.trim();
            WizardState.description = document.getElementById('projDescription').value.trim();
            WizardState.tags = document.getElementById('projTags').value.split(',').map(t => t.trim()).filter(Boolean);
            WizardState.testTypes = [...document.querySelectorAll('.test-type-cb:checked')].map(c => c.value);
            break;
        case 2:
            WizardState.targetUrl = document.getElementById('targetUrl').value.trim();
            WizardState.browserType = document.getElementById('browserType').value;
            WizardState.requirements = collectRequirements();
            WizardState.requirements.targetUrl = WizardState.targetUrl;
            WizardState.requirements.appName = WizardState.requirements.appName || WizardState.projectName;
            break;
        case 3:
            WizardState.envName = document.getElementById('envName').value.trim();
            WizardState.envBaseUrl = document.getElementById('envBaseUrl').value.trim();
            WizardState.envVars = collectEnvVars();
            break;
        case 4:
            WizardState.scenarioSource = document.querySelector('input[name="scenarioSource"]:checked')?.value || 'manual';
            break;
    }
}

// --- Requirement Fields ---

function renderRequirementFields() {
    const container = document.getElementById('requirementsSection');
    const types = WizardState.testTypes;

    if (!types.length) {
        container.innerHTML = '';
        return;
    }

    // Collect all unique field IDs to avoid duplicates across types that share fields
    const seen = new Set();
    const sections = [];

    // Common field: appName — shown once at the top if any type uses it
    const typesNeedingAppName = ['UI Testing', 'E2E Testing', 'Regression', 'Smoke Testing', 'Accessibility'];
    if (types.some(t => typesNeedingAppName.includes(t))) {
        sections.push(`
            <div class="mb-4">
                <h5 class="mb-3 text-muted fw-semibold" style="font-size:0.75rem;letter-spacing:.08em;text-transform:uppercase">Common</h5>
                <div class="row g-3">
                    <div class="col-md-6">
                        <label class="form-label required">Application Name</label>
                        <input type="text" class="form-control req-field" id="req_appName" name="appName"
                               placeholder="${WizardState.projectName || 'My Web App'}"
                               data-required="true" data-label="Application Name" />
                    </div>
                </div>
            </div>
        `);
        seen.add('appName');
    }

    // Per test type sections
    for (const tt of types) {
        const fields = TEST_TYPE_REQUIREMENTS[tt];
        if (!fields) continue;

        const fieldHtml = fields.map(f => {
            if (seen.has(f.id)) return '';
            seen.add(f.id);

            const required = f.required ? 'data-required="true"' : '';
            const requiredStar = f.required ? '<span class="text-danger">*</span>' : '';
            const label = `<label class="form-label">${f.label} ${requiredStar}</label>`;
            const commonAttrs = `id="req_${f.id}" name="${f.id}" class="form-control req-field"
                data-label="${f.label}" ${required}
                ${f.conditional ? `data-conditional="${f.conditional}"` : ''}`;

            let input = '';
            if (f.type === 'text') {
                input = `<input type="text" ${commonAttrs} placeholder="${f.placeholder || ''}" />`;
            } else if (f.type === 'textarea') {
                input = `<textarea ${commonAttrs} rows="3" placeholder="${f.placeholder || ''}"></textarea>`;
            } else if (f.type === 'select') {
                const opts = f.options.map(o => `<option value="${o}">${o}</option>`).join('');
                input = `<select ${commonAttrs}>${opts}</select>`;
            }
            return `<div class="col-12 col-md-6 req-field-wrap" id="wrap_${f.id}">${label}${input}</div>`;
        }).join('');

        if (!fieldHtml.trim()) continue;

        sections.push(`
            <div class="mb-4">
                <h5 class="mb-3 text-muted fw-semibold" style="font-size:0.75rem;letter-spacing:.08em;text-transform:uppercase">${tt}</h5>
                <div class="row g-3">${fieldHtml}</div>
            </div>
        `);
    }

    container.innerHTML = `
        <div class="border-top pt-4 mt-2">
            <div class="d-flex align-items-center mb-3 gap-2">
                <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="#ae3ec9" stroke-width="2" class="flex-shrink-0">
                    <path stroke="none" d="M0 0h24v24H0z" fill="none"/><path d="M9 11l3 3l8 -8"/><path d="M20 12v6a2 2 0 0 1 -2 2h-12a2 2 0 0 1 -2 -2v-12a2 2 0 0 1 2 -2h9"/>
                </svg>
                <span class="fw-medium">Requirements <span class="text-muted fw-normal">— your answers will be used to generate a detailed pre-prompt for the AI agent</span></span>
            </div>
            ${sections.join('')}
        </div>
    `;

    // Wire up conditional visibility
    container.querySelectorAll('[data-conditional]').forEach(el => {
        const [depId, depVal] = el.dataset.conditional.split(':');
        const depEl = document.getElementById(`req_${depId}`);
        const wrapper = document.getElementById(`wrap_${el.name}`);
        if (!depEl || !wrapper) return;

        const update = () => {
            const hide = depVal.startsWith('!')
                ? depEl.value === depVal.slice(1)
                : depEl.value !== depVal;
            wrapper.style.display = hide ? 'none' : '';
            if (hide) el.value = '';
        };
        depEl.addEventListener('change', update);
        update();
    });

    // Restore previous values if navigating back
    for (const [key, val] of Object.entries(WizardState.requirements)) {
        const el = document.getElementById(`req_${key}`);
        if (el) el.value = val;
    }
}

function collectRequirements() {
    const result = {};
    document.querySelectorAll('.req-field').forEach(el => {
        if (el.name) result[el.name] = el.value.trim();
    });
    return result;
}

// --- Agent Cards ---

async function loadAgentCards() {
    const container = document.getElementById('agentCards');
    container.innerHTML = `<div class="col-12 text-center py-4 text-muted"><span class="spinner-border spinner-border-sm me-2"></span> Loading agents...</div>`;
    try {
        const res = await fetch('/AiAgents/GetAll');
        const agents = await res.json();
        const enabled = agents.filter(a => a.isEnabled);
        if (!enabled.length) {
            container.innerHTML = `<div class="col-12 text-center text-muted py-4">No agents configured. <a href="/SiteSettings/Index">Add one in Site Settings</a>.</div>`;
            return;
        }
        container.innerHTML = enabled.map(a => {
            const selected = WizardState.selectedAgentId === a.id;
            const caps = (a.capabilities || []).map(c => `<span class="badge bg-purple-lt me-1">${escHtml(c)}</span>`).join('');
            return `
            <div class="col-md-6 col-lg-4">
                <div class="card card-sm h-100 selectable-agent-card ${selected ? 'border-primary shadow-sm' : ''}"
                     onclick="selectAgent('${a.id}', '${escHtml(a.name)}')"
                     data-agent-id="${a.id}" style="cursor:pointer;transition:box-shadow .15s,border-color .15s">
                    <div class="card-body">
                        <div class="d-flex align-items-center justify-content-between mb-2">
                            <span class="badge bg-purple-lt">${escHtml(a.providerType)}</span>
                            ${a.isDefault ? '<span class="badge bg-yellow-lt">Default</span>' : ''}
                            ${selected ? '<svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="#206bc4" stroke-width="2.5"><path stroke="none" d="M0 0h24v24H0z" fill="none"/><path d="M5 12l5 5l10 -10"/></svg>' : ''}
                        </div>
                        <div class="fw-medium">${escHtml(a.name)}</div>
                        <div class="text-muted small mb-2">${escHtml(a.modelName || '')}</div>
                        <div class="mt-1">${caps}</div>
                    </div>
                </div>
            </div>`;
        }).join('');
    } catch (err) {
        container.innerHTML = `<div class="col-12 text-danger small">Failed to load agents: ${err.message}</div>`;
    }
}

function selectAgent(agentId, agentName) {
    WizardState.selectedAgentId = agentId;
    WizardState.selectedAgentName = agentName;
    document.querySelectorAll('.selectable-agent-card').forEach(card => {
        const isSelected = card.dataset.agentId === agentId;
        card.classList.toggle('border-primary', isSelected);
        card.classList.toggle('shadow-sm', isSelected);
        // Update checkmark
        const check = card.querySelector('svg[stroke="#206bc4"]');
        if (isSelected && !check) {
            const badge = card.querySelector('.d-flex');
            badge.insertAdjacentHTML('beforeend', '<svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="#206bc4" stroke-width="2.5"><path stroke="none" d="M0 0h24v24H0z" fill="none"/><path d="M5 12l5 5l10 -10"/></svg>');
        } else if (!isSelected && check) {
            check.remove();
        }
    });
}

// --- Review + Pre-Prompt ---

function renderReviewSummary() {
    document.getElementById('rev-name').textContent = WizardState.projectName;
    document.getElementById('rev-desc').textContent = WizardState.description || '—';
    document.getElementById('rev-tags').textContent = WizardState.tags.join(', ') || '—';
    document.getElementById('rev-testtypes').textContent = WizardState.testTypes.join(', ') || '—';
    document.getElementById('rev-agent').textContent = WizardState.selectedAgentName || '—';
    document.getElementById('rev-url').textContent = WizardState.targetUrl || '—';
    document.getElementById('rev-browser').textContent = WizardState.browserType;
    document.getElementById('rev-env').textContent = WizardState.envName || '—';
}

async function generatePrePromptPreview() {
    const container = document.getElementById('prePromptPreviewContainer');
    const badge = document.getElementById('prePromptTokenBadge');
    container.innerHTML = `<div class="text-center py-4 text-muted"><span class="spinner-border spinner-border-sm me-2"></span> Generating pre-prompt from your requirements...</div>`;
    badge.style.display = 'none';
    try {
        const res = await fetch('/AiProject/GeneratePrePrompt', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token() },
            body: JSON.stringify({
                testTypes: WizardState.testTypes,
                targetUrl: WizardState.targetUrl,
                requirements: WizardState.requirements
            })
        });
        const result = await res.json();
        if (!result.success) throw new Error(result.message);

        WizardState.generatedPrePrompt = result.content;
        container.innerHTML = `<textarea class="form-control font-monospace" rows="14" readonly style="font-size:0.8rem;resize:none">${escHtml(result.content)}</textarea>`;
        badge.textContent = `~${result.tokenEstimate.toLocaleString()} tokens`;
        badge.style.display = '';
    } catch (err) {
        container.innerHTML = `<div class="alert alert-warning py-2 small">Could not generate pre-prompt: ${escHtml(err.message)}</div>`;
    }
}

// --- Environment ---

function collectEnvVars() {
    const vars = {};
    document.querySelectorAll('#envVarsBody tr').forEach(row => {
        const key = row.querySelector('.env-var-key')?.value.trim();
        const val = row.querySelector('.env-var-val')?.value.trim();
        if (key) vars[key] = val || '';
    });
    return vars;
}

function addEnvVar() {
    const tbody = document.getElementById('envVarsBody');
    const row = document.createElement('tr');
    row.innerHTML = `
        <td><input type="text" class="form-control form-control-sm env-var-key" placeholder="KEY" /></td>
        <td><input type="text" class="form-control form-control-sm env-var-val" placeholder="value" /></td>
        <td><button type="button" class="btn btn-sm btn-ghost-danger" onclick="this.closest('tr').remove()">
            <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M4 7h16"/><path d="M10 11v6"/><path d="M14 11v6"/><path d="M5 7l1 12a2 2 0 0 0 2 2h8a2 2 0 0 0 2 -2l1 -12"/><path d="M9 7v-3a1 1 0 0 1 1 -1h4a1 1 0 0 1 1 1v3"/></svg>
        </button></td>
    `;
    tbody.appendChild(row);
}

// --- Create ---

async function createProject() {
    collectCurrentStep();
    const btn = document.getElementById('createBtn');
    btn.disabled = true;
    btn.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span> Creating...';

    try {
        const project = {
            name: WizardState.projectName,
            description: WizardState.description,
            tags: WizardState.tags,
            testTypes: WizardState.testTypes,
            aiAgentId: WizardState.selectedAgentId,
            status: 0
        };

        const res = await fetch('/AiProject/Create', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token() },
            body: JSON.stringify({ project, requirements: WizardState.requirements })
        });
        const result = await res.json();
        if (!result.success) throw new Error(result.message);

        const projectId = result.id;

        if (WizardState.envName) {
            await fetch('/Environment/Create', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token() },
                body: JSON.stringify({
                    name: WizardState.envName,
                    projectId,
                    baseUrl: WizardState.envBaseUrl || WizardState.targetUrl,
                    browserType: WizardState.browserType,
                    variables: { ...WizardState.envVars, BaseUrl: WizardState.targetUrl },
                    isDefault: true
                })
            });
        }

        showCustomToast('success', 'AI project created successfully!');
        setTimeout(() => window.location.href = `/AiProject/Details/${projectId}`, 1200);
    } catch (err) {
        showCustomToast('danger', err.message);
        btn.disabled = false;
        btn.innerHTML = `<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="icon me-1"><path stroke="none" d="M0 0h24v24H0z" fill="none"/><path d="M5 12l5 5l10 -10"/></svg> Create Project`;
    }
}

function escHtml(str) {
    if (!str) return '';
    return String(str).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
}

document.addEventListener('DOMContentLoaded', () => {
    updateTabStates();
    updateNavButtons();
});
