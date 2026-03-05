/**
 * options.js – Settings page logic
 * ─────────────────────────────────
 * Loads existing config from storage on page load, and saves back on submit.
 * Also offers a "Test Connection" button to validate the PAT.
 */

import { loadConfig, saveConfig } from '../lib/configService.js';
import { getMyIdentity } from '../lib/azureDevopsClient.js';

/* ── DOM refs ── */
const form        = document.getElementById('settings-form');
const msgEl       = document.getElementById('message');
const btnTest     = document.getElementById('btn-test');
const btnToggle   = document.getElementById('btn-toggle-pat');
const patInput    = document.getElementById('pat');

/* ── Helpers ── */
function showMsg(text, type = 'success') {
  msgEl.textContent = text;
  msgEl.className = type; // success | error
}
function hideMsg() { msgEl.className = 'hidden'; }

/* ── Load existing settings into the form ── */
async function populateForm() {
  const cfg = await loadConfig();
  document.getElementById('organization').value    = cfg.organization;
  document.getElementById('project').value         = cfg.project;
  document.getElementById('team').value            = cfg.team;
  patInput.value                                   = cfg.pat;
  document.getElementById('autoRefresh').checked   = cfg.autoRefresh;
  document.getElementById('refreshInterval').value = cfg.refreshInterval;
}

/* ── Save ── */
form.addEventListener('submit', async (e) => {
  e.preventDefault();
  hideMsg();

  const config = {
    organization:    document.getElementById('organization').value.trim(),
    project:         document.getElementById('project').value.trim(),
    team:            document.getElementById('team').value.trim(),
    pat:             patInput.value.trim(),
    autoRefresh:     document.getElementById('autoRefresh').checked,
    refreshInterval: Number(document.getElementById('refreshInterval').value) || 5,
  };

  try {
    await saveConfig(config);
    showMsg('Settings saved successfully.', 'success');
  } catch (err) {
    showMsg('Failed to save: ' + err.message, 'error');
  }
});

/* ── Test connection ── */
btnTest.addEventListener('click', async () => {
  hideMsg();
  const org = document.getElementById('organization').value.trim();
  const pat = patInput.value.trim();

  if (!org || !pat) {
    showMsg('Organization and PAT are required to test.', 'error');
    return;
  }

  btnTest.disabled = true;
  btnTest.textContent = 'Testing…';

  try {
    const me = await getMyIdentity(org, pat);
    showMsg(`Connection OK – authenticated as "${me.displayName}" (${me.id})`, 'success');
  } catch (err) {
    showMsg('Connection failed: ' + err.message, 'error');
  } finally {
    btnTest.disabled = false;
    btnTest.textContent = 'Test Connection';
  }
});

/* ── Toggle PAT visibility ── */
btnToggle.addEventListener('click', () => {
  patInput.type = patInput.type === 'password' ? 'text' : 'password';
});

/* ── Init ── */
populateForm();
