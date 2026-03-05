/**
 * panel.js  –  Side-panel main script
 * ────────────────────────────────────
 * Responsible for:
 *   1. Loading config from storage (via configService)
 *   2. Fetching active PRs from Azure DevOps (via azureDevopsClient)
 *   3. Rendering PR cards and handling interactions
 *   4. Managing local "done" flags
 *   5. Listening for auto-refresh messages from the background worker
 */

import {
  loadConfig,
  isConfigComplete,
  loadDonePrIds,
  saveDonePrIds,
} from '../lib/configService.js';

import {
  getMyIdentity,
  fetchActivePullRequests,
  classifyApproval,
  voteLabel,
} from '../lib/azureDevopsClient.js';

/* ── DOM references ── */
const listEl      = document.getElementById('pr-list');
const loadingEl   = document.getElementById('loading');
const emptyEl     = document.getElementById('empty-state');
const statusBar   = document.getElementById('status-bar');
const btnRefresh  = document.getElementById('btn-refresh');
const btnSettings = document.getElementById('btn-settings');
const btnOpenOpt  = document.getElementById('btn-open-options');
const filterBtns  = document.querySelectorAll('.filter-btn');

/* ── State ── */
let currentFilter = 'all';
let prData = [];      // enriched PR objects
let donePrIds = new Set();
let myUserId = null;

/* ── Helpers ── */

/** Show / hide elements */
function show(el) { el.classList.remove('hidden'); }
function hide(el) { el.classList.add('hidden'); }

/** Display a message in the status bar */
function showStatus(msg, type = 'info') {
  statusBar.textContent = msg;
  statusBar.className = type; // error | info | success
  show(statusBar);
}
function clearStatus() { hide(statusBar); }

/** Extract short branch name from e.g. "refs/heads/feature/foo" */
function shortRef(ref) {
  return (ref || '').replace(/^refs\/heads\//, '');
}

/* ── Rendering ── */

/**
 * Build a single PR card element.
 * @param {object} pr  enriched PR object
 * @returns {HTMLElement}
 */
function renderCard(pr) {
  const card = document.createElement('div');
  card.className = 'pr-card';
  card.dataset.prId = pr.pullRequestId;

  // State classes
  if (donePrIds.has(pr.pullRequestId)) {
    card.classList.add('done-card');
  }
  if (pr._approval.isReviewer) {
    card.classList.add(pr._approval.hasApproved ? 'approved-card' : 'needs-review');
  } else {
    card.classList.add('not-reviewer');
  }

  // PR URL in Azure DevOps
  const prUrl =
    `https://dev.azure.com/${pr._org}/${encodeURIComponent(pr.repository.project.name)}` +
    `/_git/${encodeURIComponent(pr.repository.name)}/pullrequest/${pr.pullRequestId}`;

  // Badge
  let badgeHtml = '';
  if (pr._approval.isReviewer) {
    if (pr._approval.hasApproved) {
      badgeHtml = `<span class="badge badge-approved">${voteLabel(pr._approval.vote)}</span>`;
    } else if (pr._approval.vote === 0) {
      badgeHtml = `<span class="badge badge-needs">Needs my review</span>`;
    } else if (pr._approval.vote === -5) {
      badgeHtml = `<span class="badge badge-waiting">${voteLabel(pr._approval.vote)}</span>`;
    } else if (pr._approval.vote === -10) {
      badgeHtml = `<span class="badge badge-rejected">${voteLabel(pr._approval.vote)}</span>`;
    } else {
      badgeHtml = `<span class="badge badge-no-vote">${voteLabel(pr._approval.vote)}</span>`;
    }
  } else {
    badgeHtml = `<span class="badge badge-not-reviewer">Not a reviewer</span>`;
  }

  const isDone = donePrIds.has(pr.pullRequestId);

  card.innerHTML = `
    <div class="pr-title">
      <a href="${prUrl}" target="_blank" rel="noopener" title="Open in Azure DevOps">
        ${escapeHtml(pr.title)}
      </a>
    </div>
    <div class="pr-meta">
      <span title="Repository">${escapeHtml(pr.repository.name)}</span>
      <span title="Author">by ${escapeHtml(pr.createdBy?.displayName || 'Unknown')}</span>
      <span title="Project">${escapeHtml(pr.repository.project?.name || '')}</span>
    </div>
    <div class="pr-branches">
      <code>${escapeHtml(shortRef(pr.sourceRefName))}</code>
      &rarr;
      <code>${escapeHtml(shortRef(pr.targetRefName))}</code>
    </div>
    <div class="pr-badges">
      ${badgeHtml}
      <label class="done-toggle" title="Mark as locally done">
        <input type="checkbox" class="done-cb" data-pr-id="${pr.pullRequestId}" ${isDone ? 'checked' : ''} />
        Done
      </label>
    </div>
  `;

  return card;
}

/**
 * Re-render the full PR list according to the current filter.
 */
function renderList() {
  listEl.innerHTML = '';

  const visible = prData.filter((pr) => {
    const isDone = donePrIds.has(pr.pullRequestId);
    switch (currentFilter) {
      case 'needs-review':
        return pr._approval.isReviewer && !pr._approval.hasApproved && !isDone;
      case 'approved':
        return pr._approval.isReviewer && pr._approval.hasApproved && !isDone;
      case 'done':
        return isDone;
      default:
        return true;
    }
  });

  if (visible.length === 0 && prData.length > 0) {
    listEl.innerHTML = '<p style="text-align:center;color:#888;padding:24px;">No PRs match this filter.</p>';
    return;
  }

  // Sort: needs-review first, then no-vote, then approved, then done
  visible.sort((a, b) => {
    const aDone = donePrIds.has(a.pullRequestId) ? 1 : 0;
    const bDone = donePrIds.has(b.pullRequestId) ? 1 : 0;
    if (aDone !== bDone) return aDone - bDone;

    // Needs-review (vote 0 & reviewer) first
    const aNeeds = a._approval.isReviewer && !a._approval.hasApproved ? 0 : 1;
    const bNeeds = b._approval.isReviewer && !b._approval.hasApproved ? 0 : 1;
    return aNeeds - bNeeds;
  });

  for (const pr of visible) {
    listEl.appendChild(renderCard(pr));
  }
}

/** Simple HTML-escape */
function escapeHtml(str) {
  const d = document.createElement('div');
  d.textContent = str;
  return d.innerHTML;
}

/* ── Data loading ── */

async function loadData() {
  clearStatus();
  show(loadingEl);
  hide(emptyEl);
  listEl.innerHTML = '';

  try {
    const config = await loadConfig();

    if (!isConfigComplete(config)) {
      hide(loadingEl);
      show(emptyEl);
      return;
    }

    // Resolve my identity
    if (!myUserId) {
      const me = await getMyIdentity(config.organization, config.pat);
      myUserId = me.id;
      console.log('[REBUSS] Authenticated as:', me.displayName, me.id);
    }

    // Fetch PRs
    const prs = await fetchActivePullRequests(config);

    // Enrich
    donePrIds = await loadDonePrIds();
    prData = prs.map((pr) => ({
      ...pr,
      _org: config.organization,
      _approval: classifyApproval(pr, myUserId),
    }));

    hide(loadingEl);

    if (prData.length === 0) {
      show(emptyEl);
      emptyEl.querySelector('p').textContent = 'No active pull requests found.';
      emptyEl.querySelector('.hint').textContent = '';
    } else {
      hide(emptyEl);
      showStatus(`${prData.length} active PR(s) loaded`, 'success');
      setTimeout(clearStatus, 3000);
      renderList();
    }
  } catch (err) {
    hide(loadingEl);
    console.error('[REBUSS]', err);
    showStatus(err.message, 'error');
  }
}

/* ── Event listeners ── */

// Refresh
btnRefresh.addEventListener('click', () => {
  myUserId = null; // re-resolve identity
  loadData();
});

// Open settings
btnSettings.addEventListener('click', () => {
  chrome.runtime.openOptionsPage();
});
btnOpenOpt.addEventListener('click', () => {
  chrome.runtime.openOptionsPage();
});

// Filter tabs
filterBtns.forEach((btn) => {
  btn.addEventListener('click', () => {
    filterBtns.forEach((b) => b.classList.remove('active'));
    btn.classList.add('active');
    currentFilter = btn.dataset.filter;
    renderList();
  });
});

// Done checkboxes (event delegation)
listEl.addEventListener('change', async (e) => {
  if (!e.target.classList.contains('done-cb')) return;
  const prId = Number(e.target.dataset.prId);
  if (e.target.checked) {
    donePrIds.add(prId);
  } else {
    donePrIds.delete(prId);
  }
  await saveDonePrIds(donePrIds);
  renderList();
});

// Listen for auto-refresh from background
chrome.runtime.onMessage.addListener((msg) => {
  if (msg.type === 'AUTO_REFRESH') {
    console.log('[REBUSS] Auto-refresh triggered');
    loadData();
  }
});

// Listen for config changes (e.g. user just saved settings)
chrome.storage.onChanged.addListener((changes, area) => {
  if (area === 'local' && (changes.organization || changes.project || changes.pat)) {
    console.log('[REBUSS] Config changed, reloading…');
    myUserId = null;
    loadData();
  }
});

/* ── Init ── */
loadData();
