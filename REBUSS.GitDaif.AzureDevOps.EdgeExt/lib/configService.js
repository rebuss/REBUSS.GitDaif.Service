/**
 * configService.js
 * ────────────────
 * Centralises all access to user-provided configuration (organization,
 * project, team, PAT).  Nothing is hard-coded – every value is read from
 * chrome.storage.local, which is populated via the Options page.
 *
 * SECURITY NOTE
 *   • The PAT is stored in chrome.storage.local which is sandboxed to
 *     this extension.  It is never exposed to web pages.
 *   • For additional hardening you could encrypt the PAT before storing,
 *     but that is out-of-scope for this MVP.
 *
 * STORAGE KEYS
 *   organization  – e.g. "myorg"  (just the slug, NOT the full URL)
 *   project       – e.g. "MyProject"
 *   team          – e.g. "MyTeam"  (can be name or ID)
 *   pat           – Personal Access Token
 *   autoRefresh   – boolean, whether to auto-refresh every N minutes
 *   refreshInterval – number, minutes between auto-refreshes (default 5)
 */

const CONFIG_KEYS = [
  'organization',
  'project',
  'team',
  'pat',
  'autoRefresh',
  'refreshInterval',
];

/**
 * Load all configuration values from storage.
 * @returns {Promise<{organization:string, project:string, team:string, pat:string, autoRefresh:boolean, refreshInterval:number}>}
 */
export async function loadConfig() {
  return new Promise((resolve) => {
    chrome.storage.local.get(CONFIG_KEYS, (result) => {
      resolve({
        organization: result.organization || '',
        project: result.project || '',
        team: result.team || '',
        pat: result.pat || '',
        autoRefresh: result.autoRefresh ?? true,
        refreshInterval: result.refreshInterval ?? 5,
      });
    });
  });
}

/**
 * Persist all configuration values to storage.
 * @param {{organization:string, project:string, team:string, pat:string, autoRefresh:boolean, refreshInterval:number}} config
 * @returns {Promise<void>}
 */
export async function saveConfig(config) {
  return new Promise((resolve) => {
    chrome.storage.local.set(config, resolve);
  });
}

/**
 * Quick check: are all required fields filled in?
 * @param {object} config
 * @returns {boolean}
 */
export function isConfigComplete(config) {
  return !!(config.organization && config.project && config.pat);
  // `team` is optional – if omitted we fetch PRs for the whole project.
}

/* ── "Done" flags (local-only) ── */

const DONE_KEY = 'donePrIds';

/**
 * Load the set of PR IDs that the user manually marked as "done".
 * @returns {Promise<Set<number>>}
 */
export async function loadDonePrIds() {
  return new Promise((resolve) => {
    chrome.storage.local.get([DONE_KEY], (result) => {
      resolve(new Set(result[DONE_KEY] || []));
    });
  });
}

/**
 * Persist the set of "done" PR IDs.
 * @param {Set<number>} ids
 * @returns {Promise<void>}
 */
export async function saveDonePrIds(ids) {
  return new Promise((resolve) => {
    chrome.storage.local.set({ [DONE_KEY]: [...ids] }, resolve);
  });
}
