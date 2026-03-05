/**
 * azureDevopsClient.js
 * ────────────────────
 * Thin wrapper around Azure DevOps REST API (v7.1-preview).
 *
 * AUTHENTICATION
 *   Uses Basic Auth with an empty username and the PAT as password.
 *   Header:  Authorization: Basic base64(":" + pat)
 *
 * HOW TO EXTEND
 *   • To filter by repository: add `searchCriteria.repositoryId` to the
 *     pull-requests query string.
 *   • To filter by author: add `searchCriteria.creatorId`.
 *   • See https://learn.microsoft.com/en-us/rest/api/azure/devops/git/pull-requests/get-pull-requests
 */

/**
 * Build the Basic-Auth header value for a PAT.
 * @param {string} pat
 * @returns {string}
 */
function authHeader(pat) {
  return 'Basic ' + btoa(':' + pat);
}

/**
 * Resolve the current user's identity (the person who owns the PAT).
 * We hit the "me" profile endpoint:
 *   GET https://vssps.dev.azure.com/{org}/_apis/profile/profiles/me?api-version=7.1-preview.1
 *
 * Falls back to the connectionData endpoint if profile doesn't give us what we need.
 *
 * @param {string} organization
 * @param {string} pat
 * @returns {Promise<{id:string, displayName:string, uniqueName:string}>}
 */
export async function getMyIdentity(organization, pat) {
  // Primary: connection data gives us authenticatedUser reliably
  const url = `https://dev.azure.com/${encodeURIComponent(organization)}/_apis/connectionData`;
  const resp = await fetch(url, {
    headers: { Authorization: authHeader(pat) },
  });

  if (!resp.ok) {
    throw new Error(`Failed to resolve identity (${resp.status} ${resp.statusText})`);
  }

  const data = await resp.json();
  const user = data.authenticatedUser;
  return {
    id: user.id,                       // GUID
    displayName: user.providerDisplayName || user.customDisplayName || '',
    uniqueName: user.properties?.Account?.$value || '',
  };
}

/**
 * Fetch all active pull requests for a project (optionally scoped to team repos).
 *
 * Endpoint:
 *   GET https://dev.azure.com/{org}/{project}/_apis/git/pullrequests
 *       ?searchCriteria.status=active
 *       &$top=200
 *       &api-version=7.1-preview.1
 *
 * @param {{organization:string, project:string, team:string, pat:string}} config
 * @returns {Promise<Array>}  array of PR objects from the API
 */
export async function fetchActivePullRequests(config) {
  const { organization, project, pat } = config;

  const baseUrl =
    `https://dev.azure.com/${encodeURIComponent(organization)}` +
    `/${encodeURIComponent(project)}` +
    `/_apis/git/pullrequests`;

  const params = new URLSearchParams({
    'searchCriteria.status': 'active',
    '$top': '200',
    'api-version': '7.1-preview.1',
  });

  const resp = await fetch(`${baseUrl}?${params}`, {
    headers: { Authorization: authHeader(pat) },
  });

  if (!resp.ok) {
    const body = await resp.text();
    throw new Error(`Azure DevOps API error ${resp.status}: ${body}`);
  }

  const data = await resp.json();
  return data.value || [];
}

/**
 * Classify a single PR from the perspective of the current user.
 *
 * Azure DevOps reviewer vote values:
 *   10  = approved
 *    5  = approved with suggestions
 *    0  = no vote
 *   -5  = waiting for author
 *  -10  = rejected
 *
 * @param {object} pr         – PR object from the API
 * @param {string} myUserId   – GUID of the current user
 * @returns {{ isReviewer:boolean, hasApproved:boolean, vote:number }}
 */
export function classifyApproval(pr, myUserId) {
  const lowerMyId = myUserId.toLowerCase();

  const reviewer = (pr.reviewers || []).find(
    (r) => r.id?.toLowerCase() === lowerMyId
  );

  if (!reviewer) {
    return { isReviewer: false, hasApproved: false, vote: 0 };
  }

  const vote = reviewer.vote ?? 0;
  // Approved if vote is 5 (approved with suggestions) or 10 (approved)
  const hasApproved = vote >= 5;

  return { isReviewer: true, hasApproved, vote };
}

/**
 * Build a human-readable label from the numeric vote.
 * @param {number} vote
 * @returns {string}
 */
export function voteLabel(vote) {
  switch (vote) {
    case 10: return 'Approved';
    case 5:  return 'Approved with suggestions';
    case 0:  return 'No vote';
    case -5: return 'Waiting for author';
    case -10: return 'Rejected';
    default: return `Vote ${vote}`;
  }
}
