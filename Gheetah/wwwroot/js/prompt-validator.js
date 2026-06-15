'use strict';

const PromptValidator = (() => {
    const token = () => document.querySelector('input[name="__RequestVerificationToken"]')?.value;

    const TOKEN_WARN_THRESHOLD = 2500;
    const TOKEN_MAX = 3000;

    function estimateTokens(text) {
        return Math.ceil((text || '').length / 4);
    }

    function renderFeedback(containerId, items) {
        const container = document.getElementById(containerId);
        if (!container) return;
        container.innerHTML = items.map(item => `
            <div class="d-flex align-items-start small mt-1 ${item.type === 'error' ? 'text-danger' : item.type === 'warning' ? 'text-warning' : 'text-muted'}">
                <span class="me-1">${item.type === 'error' ? '⛔' : item.type === 'warning' ? '⚠️' : 'ℹ️'}</span>
                <span>${item.message}</span>
            </div>
        `).join('');
    }

    return {
        validate(content, containerId) {
            const items = [];

            if (!content || !content.trim()) {
                items.push({ type: 'error', message: 'STRUCT-001: Pre-prompt content is empty.' });
                renderFeedback(containerId, items);
                return;
            }

            const tokens = estimateTokens(content);

            if (tokens > TOKEN_MAX) {
                items.push({ type: 'error', message: `STRUCT-002: Content exceeds token budget (~${tokens} tokens, max ${TOKEN_MAX}).` });
            } else if (tokens > TOKEN_WARN_THRESHOLD) {
                items.push({ type: 'warning', message: `STRUCT-003: Large content (~${tokens} tokens). Consider chunking.` });
            } else {
                items.push({ type: 'info', message: `~${tokens} tokens estimated.` });
            }

            if (content.length < 10) {
                items.push({ type: 'warning', message: 'SEM-001: Very short content — add context for better AI guidance.' });
            }

            const templateVarPattern = /\{\{(\w+)\}\}/g;
            const vars = [...content.matchAll(templateVarPattern)].map(m => m[1]);
            if (vars.length) {
                items.push({ type: 'info', message: `Template variables: ${[...new Set(vars)].join(', ')}` });
            }

            renderFeedback(containerId, items);
        },

        async validateGherkin(content, containerId) {
            const items = [];

            if (!content || !content.trim()) {
                renderFeedback(containerId, items);
                return;
            }

            const trimmed = content.trim();

            if (!trimmed.toLowerCase().startsWith('scenario') && !trimmed.toLowerCase().startsWith('feature') && !trimmed.toLowerCase().startsWith('background')) {
                items.push({ type: 'warning', message: 'BDD-001: Should start with Scenario:, Feature:, or Background:' });
            }

            if (!trimmed.toLowerCase().includes('given') && !trimmed.toLowerCase().includes('when') && !trimmed.toLowerCase().includes('then')) {
                items.push({ type: 'warning', message: 'BDD-002: Missing Given/When/Then steps.' });
            }

            if (items.length === 0) {
                items.push({ type: 'info', message: 'Gherkin syntax looks valid.' });

                try {
                    const res = await fetch('/AiScenario/ValidateGherkin', {
                        method: 'POST',
                        headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token() },
                        body: JSON.stringify(content)
                    });
                    if (res.ok) {
                        const result = await res.json();
                        if (!result.isValid) {
                            (result.errors || []).forEach(e => items.push({ type: 'error', message: e.message }));
                            (result.warnings || []).forEach(w => items.push({ type: 'warning', message: w.message }));
                        }
                    }
                } catch { /* ignore server validation failure */ }
            }

            renderFeedback(containerId, items);
        },

        async validateServerSide(content, source, containerId) {
            try {
                const res = await fetch('/AiScenario/ValidatePrompt', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token() },
                    body: JSON.stringify({ content, source })
                });
                if (!res.ok) return;
                const result = await res.json();
                const items = [
                    ...(result.errors || []).map(e => ({ type: 'error', message: `${e.code}: ${e.message}` })),
                    ...(result.warnings || []).map(w => ({ type: 'warning', message: `${w.code}: ${w.message}` }))
                ];
                if (!items.length && result.isValid) {
                    items.push({ type: 'info', message: `Valid (score: ${result.score}/100)` });
                }
                renderFeedback(containerId, items);
            } catch { /* ignore */ }
        }
    };
})();
