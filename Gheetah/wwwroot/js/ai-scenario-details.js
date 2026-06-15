'use strict';

const ScenarioManager = (() => {
    let scenarios = [];
    let selectedScenarioId = null;
    const projectId = () => window.AI_PROJECT_ID;
    const token = () => document.querySelector('input[name="__RequestVerificationToken"]')?.value;

    function escHtml(str) {
        if (!str) return '';
        return String(str).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
    }

    // Dual-keyed maps — handle both numeric (old JSON) and string (JsonStringEnumConverter) enum values
    const statusColors = {
        0: 'bg-secondary-lt', Draft: 'bg-secondary-lt',
        1: 'bg-blue-lt',      Ready: 'bg-blue-lt',
        2: 'bg-warning-lt',   Running: 'bg-warning-lt',
        3: 'bg-success-lt',   Passed: 'bg-success-lt',
        4: 'bg-danger-lt',    Failed: 'bg-danger-lt',
    };
    const statusLabels = {
        0: 'Draft',   Draft: 'Draft',
        1: 'Ready',   Ready: 'Ready',
        2: 'Running', Running: 'Running',
        3: 'Passed',  Passed: 'Passed',
        4: 'Failed',  Failed: 'Failed',
    };
    const sourceLabels = {
        0: 'Manual',        Manual: 'Manual',
        1: 'AI Generated',  AiGenerated: 'AI Generated',
        2: 'Jira',          JiraImported: 'Jira',
    };

    function isAiGenerated(source) {
        return source === 1 || source === 'AiGenerated';
    }

    async function loadScenarios() {
        try {
            const res = await fetch(`/AiScenario/GetScenarios?projectId=${encodeURIComponent(projectId())}`);
            scenarios = await res.json();
            initJsTree(scenarios);
        } catch (err) {
            const tv = document.getElementById('treeview');
            if (tv) tv.innerHTML = `<div class="p-3 text-danger">Failed to load scenarios: ${err.message}</div>`;
        }
    }

    function buildJsTreeData(list) {
        const groups = {};
        list.forEach(s => {
            const feature = s.featureName || 'Unassigned';
            if (!groups[feature]) groups[feature] = [];
            groups[feature].push(s);
        });

        const nodes = [];
        Object.entries(groups).forEach(([feature, items]) => {
            const fId = 'f-' + feature.replace(/[^a-z0-9]/gi, '-').toLowerCase();
            nodes.push({
                id: fId,
                text: `<span class="fw-semibold">${escHtml(feature)}</span>&nbsp;<span class="badge bg-secondary-lt">${items.length}</span>`,
                icon: 'jstree-folder',
                state: { opened: true },
                parent: '#'
            });
            items.forEach(s => {
                const badge = `<span class="badge ${statusColors[s.status] || 'bg-secondary-lt'}" style="font-size:.6rem;vertical-align:middle">${statusLabels[s.status] || '?'}</span>`;
                const aiDot = isAiGenerated(s.source) ? '&nbsp;<span class="text-purple" title="AI Generated" style="font-size:.75rem">✦</span>' : '';
                nodes.push({
                    id: 's-' + s.id,
                    text: `${badge}&nbsp;${escHtml(s.title)}${aiDot}`,
                    icon: false,
                    parent: fId,
                    scenarioId: s.id  // top-level so data.node.original.scenarioId works correctly
                });
            });
        });
        return nodes;
    }

    function initJsTree(list) {
        const tv = document.getElementById('treeview');
        if (!tv) return;

        if ($.jstree && $.jstree.reference('#treeview')) {
            $('#treeview').jstree('destroy');
        }

        if (!list.length) {
            tv.innerHTML = `<div class="p-4 text-center text-muted">
                No scenarios yet.<br>
                <button class="btn btn-sm btn-purple mt-2" onclick="openGenerateModal()">Add Scenario with AI</button>
            </div>`;
            return;
        }

        $('#treeview').jstree({
            plugins: ['wholerow', 'search'],
            search: {
                case_insensitive: true,
                show_only_matches: true,
                show_only_matches_children: true
            },
            core: {
                data: buildJsTreeData(list),
                themes: { icons: true },
                check_callback: false
            }
        }).on('select_node.jstree', function(e, data) {
            // data.node.original is the full node data object — scenarioId is at top level
            const sid = data.node.original && data.node.original.scenarioId;
            if (sid) {
                selectedScenarioId = sid;
                const scenario = scenarios.find(s => s.id === sid);
                if (scenario) showDetail(scenario);
            }
        });

        if (selectedScenarioId) {
            setTimeout(() => {
                try { $('#treeview').jstree('select_node', 's-' + selectedScenarioId); } catch { /* not ready */ }
            }, 150);
        }
    }

    function showDetail(scenario) {
        document.getElementById('scenarioEmptyState').classList.add('d-none');
        document.getElementById('scenarioDetailCard').classList.remove('d-none');

        document.getElementById('detailTitle').textContent = scenario.title || '';
        document.getElementById('detailFeatureName').textContent = scenario.featureName || '';

        const statusBadge = document.getElementById('detailStatusBadge');
        statusBadge.className = `badge ${statusColors[scenario.status] || 'bg-secondary-lt'}`;
        statusBadge.textContent = statusLabels[scenario.status] || String(scenario.status || 'Draft');

        const sourceBadge = document.getElementById('detailSourceBadge');
        sourceBadge.textContent = sourceLabels[scenario.source] ?? 'Manual';
        sourceBadge.className = `badge ${isAiGenerated(scenario.source) ? 'bg-purple-lt' : scenario.source === 2 || scenario.source === 'JiraImported' ? 'bg-blue-lt' : 'bg-secondary-lt'} ms-1`;

        const codeEl = document.getElementById('detailGherkin');
        if (codeEl) {
            codeEl.removeAttribute('data-highlighted');
            const content = scenario.gherkinContent || scenario.GherkinContent || '';
            if (content) {
                try {
                    codeEl.innerHTML = hljs.highlight(content, { language: 'gherkin' }).value;
                    codeEl.classList.add('hljs');
                } catch {
                    codeEl.textContent = content;
                }
            } else {
                codeEl.textContent = '';
            }
        }

        const tagsEl = document.getElementById('detailTags');
        if (tagsEl) tagsEl.innerHTML = (scenario.tags || scenario.Tags || []).map(t => `<span class="badge bg-blue-lt me-1">${escHtml(t)}</span>`).join('');

        if (scenario.lastExecutionId || scenario.LastExecutionId) loadLastExecution(scenario.lastExecutionId || scenario.LastExecutionId);
        else {
            const panel = document.getElementById('lastExecutionPanel');
            if (panel) panel.innerHTML = '<div class="text-muted small">No executions yet.</div>';
        }

        const runBtn = document.getElementById('runBtn');
        if (runBtn) runBtn.dataset.scenarioId = scenario.id || scenario.Id;
    }

    async function loadLastExecution(sessionId) {
        try {
            const res = await fetch(`/AiExecution/GetResult/${sessionId}`);
            if (!res.ok) return;
            const result = await res.json();
            const panel = document.getElementById('lastExecutionPanel');
            const dur = result.totalDurationMs ? `${(result.totalDurationMs / 1000).toFixed(1)}s` : '—';
            const statusClass = { Passed: 'success', Failed: 'danger', Running: 'warning', Cancelled: 'secondary' }[result.status] || 'secondary';
            panel.innerHTML = `
                <div class="d-flex align-items-center mb-2">
                    <span class="badge bg-${statusClass}-lt me-2">${result.status}</span>
                    <span class="text-muted small">${result.startTime ? new Date(result.startTime).toLocaleString() : ''}</span>
                    <span class="ms-auto text-muted small">Duration: ${dur}</span>
                </div>
                ${result.errorMessage ? `<div class="alert alert-danger small py-2">${escHtml(result.errorMessage)}</div>` : ''}
                <div class="progress mb-2" style="height:6px">
                    <div class="progress-bar bg-${statusClass}" style="width:${result.status === 'Passed' ? 100 : result.status === 'Failed' ? 100 : 50}%"></div>
                </div>
                ${(result.stepResults || []).slice(0, 5).map(sr => `
                    <div class="d-flex align-items-start small mb-1">
                        <span class="me-2">${sr.passed ? '✅' : '❌'}</span>
                        <span class="text-muted">${escHtml(sr.keyword)} </span>
                        <span class="ms-1">${escHtml(sr.text)}</span>
                    </div>
                `).join('')}
            `;
        } catch { /* ignore */ }
    }

    return {
        init() { loadScenarios(); },

        selectScenario(scenarioId) {
            selectedScenarioId = scenarioId;
            try {
                $('#treeview').jstree('deselect_all', true);
                $('#treeview').jstree('select_node', 's-' + scenarioId);
            } catch { /* jstree may not be ready */ }
            const scenario = scenarios.find(s => s.id === scenarioId);
            if (scenario) showDetail(scenario);
        },

        getSelectedScenario() {
            return scenarios.find(s => s.id === selectedScenarioId);
        },

        async refresh() { await loadScenarios(); },

        async deleteSelected() {
            if (!selectedScenarioId) return;
            const s = scenarios.find(x => x.id === selectedScenarioId);
            if (!confirm(`Delete scenario "${s?.title}"?`)) return;
            try {
                const res = await fetch(`/AiScenario/Delete/${selectedScenarioId}?projectId=${encodeURIComponent(projectId())}`, {
                    method: 'DELETE',
                    headers: { 'RequestVerificationToken': token() }
                });
                const result = await res.json();
                if (!result.success) throw new Error(result.message);
                showCustomToast('success', 'Scenario deleted.');
                selectedScenarioId = null;
                document.getElementById('scenarioEmptyState').classList.remove('d-none');
                document.getElementById('scenarioDetailCard').classList.add('d-none');
                await loadScenarios();
            } catch (err) {
                showCustomToast('danger', err.message);
            }
        }
    };
})();

function editScenario() { showCustomToast('info', 'Edit coming soon.'); }
function deleteScenario() { ScenarioManager.deleteSelected(); }

let newScenarioActiveTab = 'manual';

function openNewScenarioModal() {
    document.getElementById('newScenarioTitle').value = '';
    document.getElementById('newScenarioFeature').value = '';
    document.getElementById('newScenarioGherkin').value = '';
    document.getElementById('newScenarioTags').value = '';
    document.getElementById('gherkinValidationFeedback').innerHTML = '';
    new bootstrap.Modal(document.getElementById('newScenarioModal')).show();
}

function switchScenarioTab(tab) {
    newScenarioActiveTab = tab;
    document.getElementById('scenarioTabManual').classList.toggle('d-none', tab !== 'manual');
    document.getElementById('scenarioTabAi').classList.toggle('d-none', tab !== 'ai');
    document.querySelectorAll('#newScenarioTabs .nav-link').forEach((a, i) => {
        a.classList.toggle('active', (i === 0 && tab === 'manual') || (i === 1 && tab === 'ai'));
    });
}

async function saveNewScenario() {
    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
    const projectId = window.AI_PROJECT_ID;

    if (newScenarioActiveTab === 'manual') {
        const title = document.getElementById('newScenarioTitle').value.trim();
        const gherkin = document.getElementById('newScenarioGherkin').value.trim();
        if (!title || !gherkin) { showCustomToast('warning', 'Title and Gherkin content are required.'); return; }

        const scenario = {
            projectId,
            title,
            featureName: document.getElementById('newScenarioFeature').value.trim(),
            gherkinContent: gherkin,
            tags: document.getElementById('newScenarioTags').value.split(',').map(t => t.trim()).filter(Boolean),
            status: 0,
            source: 0
        };

        const btn = document.getElementById('saveScenarioBtn');
        btn.disabled = true;
        btn.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span> Saving...';
        try {
            const res = await fetch('/AiScenario/Create', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token },
                body: JSON.stringify(scenario)
            });
            const result = await res.json();
            if (!result.success) throw new Error(result.message);
            bootstrap.Modal.getInstance(document.getElementById('newScenarioModal')).hide();
            showCustomToast('success', 'Scenario created.');
            await ScenarioManager.refresh();
        } catch (err) {
            showCustomToast('danger', err.message);
        } finally {
            btn.disabled = false;
            btn.textContent = 'Save Scenario';
        }
    } else {
        showCustomToast('info', 'AI scenario generation will be implemented soon.');
    }
}

// ── Generate Scenario with AI modal ──────────────────────────────────────

let _genSessionId = null;

function openGenerateModal() {
    document.getElementById('genTopic').value = '';
    document.getElementById('genContext').value = '';
    document.getElementById('genPreviewSection').classList.add('d-none');
    document.getElementById('genErrorAlert').classList.add('d-none');
    document.getElementById('genSaveBtn').classList.add('d-none');
    document.getElementById('genBtn').classList.remove('d-none');
    _genSessionId = null;
    new bootstrap.Modal(document.getElementById('generateScenarioModal')).show();
}

async function generateScenario() {
    const topic = document.getElementById('genTopic').value.trim();
    if (!topic) { showCustomToast('warning', 'Please enter a scenario topic.'); return; }

    const btn = document.getElementById('genBtn');
    const errAlert = document.getElementById('genErrorAlert');
    btn.disabled = true;
    btn.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span> Generating...';
    errAlert.classList.add('d-none');

    try {
        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
        const res = await fetch('/AiScenario/Generate', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token },
            body: JSON.stringify({
                projectId: window.AI_PROJECT_ID,
                topic,
                additionalContext: document.getElementById('genContext').value.trim()
            })
        });
        const data = await res.json();
        if (!data.success) throw new Error(data.message);

        _genSessionId = data.sessionId;
        document.getElementById('genPreview').value = data.gherkin;
        document.getElementById('genSessionBadge').textContent = `Session: ${data.sessionId?.substring(0, 8)}`;
        document.getElementById('genPreviewSection').classList.remove('d-none');
        document.getElementById('genSaveBtn').classList.remove('d-none');
        btn.classList.add('d-none');
    } catch (err) {
        errAlert.textContent = err.message;
        errAlert.classList.remove('d-none');
    } finally {
        btn.disabled = false;
        btn.innerHTML = `<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="icon me-1"><path stroke="none" d="M0 0h24v24H0z" fill="none"/><path d="M9.5 14.5l-1.5 3.5l3.5 -1.5l7 -7l-2 -2z"/></svg> Generate`;
    }
}

async function saveGeneratedScenario() {
    const gherkin = document.getElementById('genPreview').value.trim();
    if (!gherkin) { showCustomToast('warning', 'No Gherkin content to save.'); return; }

    const titleMatch = gherkin.match(/Scenario(?:\s+Outline)?:\s*(.+)/i);
    const title = titleMatch ? titleMatch[1].trim() : document.getElementById('genTopic').value.trim();
    const featureMatch = gherkin.match(/Feature:\s*(.+)/i);
    const featureName = featureMatch ? featureMatch[1].trim() : 'AI Generated';

    const btn = document.getElementById('genSaveBtn');
    btn.disabled = true;

    try {
        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
        const res = await fetch('/AiScenario/Create', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token },
            body: JSON.stringify({
                projectId: window.AI_PROJECT_ID,
                title,
                featureName,
                gherkinContent: gherkin,
                source: 1,
                status: 1
            })
        });
        const data = await res.json();
        if (!data.success) throw new Error(data.message);

        bootstrap.Modal.getInstance(document.getElementById('generateScenarioModal')).hide();
        showCustomToast('success', `Scenario "${title}" saved.`);
        await ScenarioManager.refresh();
        ScenarioManager.selectScenario(data.id);
    } catch (err) {
        showCustomToast('danger', err.message);
    } finally {
        btn.disabled = false;
    }
}

document.addEventListener('DOMContentLoaded', () => {
    if (window.AI_PROJECT_ID) ScenarioManager.init();

    let searchTimeout;
    const treeSearchInput = document.getElementById('treeSearchInput');
    if (treeSearchInput) {
        treeSearchInput.addEventListener('input', function() {
            clearTimeout(searchTimeout);
            const val = this.value;
            searchTimeout = setTimeout(() => {
                try { $('#treeview').jstree(true).search(val); } catch { /* not ready */ }
            }, 250);
        });
    }

    const gherkinInput = document.getElementById('newScenarioGherkin');
    if (gherkinInput) {
        let t;
        gherkinInput.addEventListener('input', () => {
            clearTimeout(t);
            t = setTimeout(() => PromptValidator.validateGherkin(gherkinInput.value, 'gherkinValidationFeedback'), 600);
        });
    }
});
