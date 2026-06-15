'use strict';

const ConflictResolution = (() => {
    let blocks = [];
    let resolutions = {};
    let currentRepoPath = '';
    let currentFilePath = '';

    const token = () => document.querySelector('input[name="__RequestVerificationToken"]')?.value;

    function escHtml(str) {
        if (!str) return '';
        return String(str).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
    }

    function renderBlocks() {
        const container = document.getElementById('conflictBlocksContainer');
        const template = document.getElementById('conflictBlockTemplate');
        container.innerHTML = '';

        blocks.forEach((block, idx) => {
            const clone = template.content.cloneNode(true);
            const card = clone.querySelector('.conflict-block');
            card.dataset.blockIndex = block.blockIndex ?? idx;
            card.querySelector('.conflict-block-num').textContent = (block.blockIndex ?? idx) + 1;
            card.querySelector('.conflict-head-content').textContent = block.headContent || '';
            card.querySelector('.conflict-incoming-content').textContent = block.incomingContent || '';
            card.querySelector('.conflict-manual-content').value = block.headContent || '';

            if (block.isBddScenario) {
                const header = card.querySelector('.card-header');
                const badge = document.createElement('span');
                badge.className = 'badge bg-purple-lt ms-2';
                badge.textContent = 'BDD Scenario';
                header.querySelector('.card-title').appendChild(badge);
            }

            container.appendChild(clone);
        });
    }

    return {
        open(repoPath, filePath, conflictBlocks) {
            currentRepoPath = repoPath;
            currentFilePath = filePath;
            blocks = conflictBlocks || [];
            resolutions = {};

            document.getElementById('conflictFilePath').textContent = filePath;
            renderBlocks();
            new bootstrap.Modal(document.getElementById('conflictResolutionModal')).show();
        },

        resolveBlock(btn, resolution) {
            const card = btn.closest('.conflict-block');
            const blockIndex = parseInt(card.dataset.blockIndex);
            resolutions[blockIndex] = resolution;

            card.querySelectorAll('.resolve-btn').forEach(b => b.classList.remove('active'));
            btn.classList.add('active');

            const manualArea = card.querySelector('.manual-edit-area');
            manualArea.classList.toggle('d-none', resolution !== 'Manual');

            if (resolution === 'Manual') {
                const headContent = card.querySelector('.conflict-head-content').textContent;
                const incomingContent = card.querySelector('.conflict-incoming-content').textContent;
                card.querySelector('.conflict-manual-content').value =
                    headContent + '\n' + incomingContent;
            }
        },

        acceptAllHead() {
            document.querySelectorAll('.conflict-block').forEach(card => {
                const idx = parseInt(card.dataset.blockIndex);
                resolutions[idx] = 'Head';
                card.querySelectorAll('.resolve-btn').forEach(b => {
                    b.classList.toggle('active', b.dataset.resolution === 'Head');
                });
                card.querySelector('.manual-edit-area').classList.add('d-none');
            });
        },

        acceptAllIncoming() {
            document.querySelectorAll('.conflict-block').forEach(card => {
                const idx = parseInt(card.dataset.blockIndex);
                resolutions[idx] = 'Incoming';
                card.querySelectorAll('.resolve-btn').forEach(b => {
                    b.classList.toggle('active', b.dataset.resolution === 'Incoming');
                });
                card.querySelector('.manual-edit-area').classList.add('d-none');
            });
        },

        async applyResolutions() {
            const unresolvedBlocks = blocks.filter((b, i) => !resolutions[b.blockIndex ?? i]);
            if (unresolvedBlocks.length) {
                showCustomToast('warning', `${unresolvedBlocks.length} conflict block(s) still unresolved.`);
                return;
            }

            const resolvedList = blocks.map((block, i) => {
                const idx = block.blockIndex ?? i;
                const res = resolutions[idx];
                const card = document.querySelector(`.conflict-block[data-block-index="${idx}"]`);
                const customContent = res === 'Manual' ? card?.querySelector('.conflict-manual-content')?.value || '' : '';
                return { blockIndex: idx, resolution: res, customContent };
            });

            const btn = document.getElementById('applyResolutionsBtn');
            btn.disabled = true;
            btn.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span> Applying...';

            try {
                const res = await fetch('/GitConflict/ApplyResolutions', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token() },
                    body: JSON.stringify({ repoPath: currentRepoPath, filePath: currentFilePath, resolutions: resolvedList })
                });
                const result = await res.json();
                if (!result.success) throw new Error(result.message);
                bootstrap.Modal.getInstance(document.getElementById('conflictResolutionModal')).hide();
                showCustomToast('success', 'Conflicts resolved successfully.');
            } catch (err) {
                showCustomToast('danger', err.message);
            } finally {
                btn.disabled = false;
                btn.textContent = 'Apply Resolutions';
            }
        }
    };
})();

function resolveBlock(btn, resolution) { ConflictResolution.resolveBlock(btn, resolution); }
function acceptAllHead() { ConflictResolution.acceptAllHead(); }
function acceptAllIncoming() { ConflictResolution.acceptAllIncoming(); }
function applyResolutions() { ConflictResolution.applyResolutions(); }
