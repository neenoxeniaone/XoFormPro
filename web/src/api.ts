// API client. Auth is a JWT bearer token persisted in localStorage.

// Same-domain, relative API. In dev this is empty and Vite proxies /api + /submit.
// In production set VITE_BASE_PATH to your subpath (e.g. /FormPro) and the host reverse-
// proxies {base}/api, {base}/submit, {base}/v1 to the backend. No absolute domain needed.
export const API_BASE = (import.meta.env.VITE_BASE_PATH ?? "").replace(/\/$/, "");

const TOKEN_KEY = "fsp.token";

export function getToken(): string | null {
  return localStorage.getItem(TOKEN_KEY);
}
export function setToken(t: string) {
  localStorage.setItem(TOKEN_KEY, t);
}
export function clearToken() {
  localStorage.removeItem(TOKEN_KEY);
}

function submissionQuery(params: SubmissionFilters): string {
  const q = new URLSearchParams();
  if (params.formId) q.set("formId", params.formId);
  if (params.includeSpam) q.set("includeSpam", "true");
  if (params.starredOnly) q.set("starredOnly", "true");
  if (params.unreadOnly) q.set("unreadOnly", "true");
  if (params.q) q.set("q", params.q);
  if (params.sort) q.set("sort", params.sort);
  if (params.formKind) q.set("formKind", params.formKind);
  if (params.page) q.set("page", String(params.page));
  return q.toString();
}

async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  const headers: Record<string, string> = {
    "Content-Type": "application/json",
    ...(init.headers as Record<string, string>),
  };
  const token = getToken();
  if (token) headers["Authorization"] = `Bearer ${token}`;

  const res = await fetch(API_BASE + path, { ...init, headers });

  // A 401 on a request we sent WITH a token means the session expired — sign out.
  // A 401 without a token (e.g. a failed login) should surface the server's message.
  if (res.status === 401 && token) {
    clearToken();
    throw new Error("Your session expired. Please sign in again.");
  }
  if (!res.ok) {
    let message = `${res.status} ${res.statusText}`;
    try {
      const body = await res.json();
      if (body?.message) message = body.message;
    } catch {
      /* ignore */
    }
    throw new Error(message);
  }
  if (res.status === 204) return undefined as T;
  return res.json() as Promise<T>;
}

// ── Types ──
export interface CurrentUser {
  userId: string;
  email: string;
  role: string;
  emailVerified: boolean;
  organizationId: string;
  workspaceName: string;
  notificationEmail: string;
  planTier: string;
  monthlyQuota: number;
}

export interface Member {
  id: string;
  email: string;
  role: string;
  emailVerified: boolean;
  createdAt: string;
}
export interface PendingInvite {
  id: string;
  email: string;
  role: string;
  createdAt: string;
}

export interface AccountInfo {
  id: string;
  name: string;
  notificationEmail: string;
  planTier: string;
  monthlyQuota: number;
  allowedDomains: string | null;
  usageThisMonth: number;
}

export interface FormSummary {
  id: string;
  accessKey: string;
  name: string;
  kind: string;
  notifyByEmail: boolean;
  toEmailOverride: string | null;
  defaultSubject: string;
  fromName: string;
  redirectUrl: string | null;
  isActive: boolean;
  requireCaptcha: boolean;
  allowedDomains: string | null;
  captchaProvider: string;
  captchaSiteKey: string | null;
  autoresponderEnabled: boolean;
  autoresponderSubject: string | null;
  autoresponderBody: string | null;
  emailIntro: string | null;
  successTitle: string | null;
  successMessage: string | null;
  createdAt: string;
  submissionCount: number;

  // Bot checks, rendered by forms.js on the embedding site (no markup change there).
  requireHumanCheck: boolean;
  requireToken: boolean;
  captchaAutoInject: boolean;
}

export interface Webhook {
  id: string;
  url: string;
  isActive: boolean;
  createdAt: string;
}

export interface Submission {
  id: string;
  formId: string;
  subject: string;
  senderEmail: string | null;
  isSpam: boolean;
  isRead: boolean;
  isStarred: boolean;
  ipAddress: string | null;
  userAgent: string | null;
  createdAt: string;
  data: Record<string, unknown>;
}

export interface SubmissionPage {
  total: number;
  unread: number;
  page: number;
  pageSize: number;
  items: Submission[];
}

export interface SubmissionFilters {
  formId?: string;
  includeSpam?: boolean;
  starredOnly?: boolean;
  unreadOnly?: boolean;
  q?: string;
  sort?: "newest" | "oldest";
  formKind?: "form" | "list";
  page?: number;
}

export interface ApiKeyInfo {
  id: string;
  label: string;
  prefix: string;
  createdAt: string;
  lastUsedAt: string | null;
  revokedAt: string | null;
}

export const api = {
  // ── Auth ──
  register: (email: string, password: string, workspaceName?: string) =>
    request<{ token: string; user: CurrentUser }>("/api/auth/register", {
      method: "POST",
      body: JSON.stringify({ email, password, workspaceName }),
    }),
  login: (email: string, password: string) =>
    request<{ token: string; user: CurrentUser }>("/api/auth/login", {
      method: "POST",
      body: JSON.stringify({ email, password }),
    }),
  me: () => request<CurrentUser>("/api/auth/me"),
  forgotPassword: (email: string) =>
    request<{ message: string }>("/api/auth/forgot-password", { method: "POST", body: JSON.stringify({ email }) }),
  resetPassword: (token: string, newPassword: string) =>
    request<{ message: string }>("/api/auth/reset-password", { method: "POST", body: JSON.stringify({ token, newPassword }) }),
  verifyEmail: (token: string) =>
    request<{ message: string }>("/api/auth/verify-email", { method: "POST", body: JSON.stringify({ token }) }),
  acceptInvite: (token: string, password: string) =>
    request<{ token: string; user: CurrentUser }>("/api/auth/accept-invite", { method: "POST", body: JSON.stringify({ token, password }) }),

  // ── Account ──
  getAccount: () => request<AccountInfo>("/api/account"),
  updateAccount: (workspaceName: string, notificationEmail: string, allowedDomains: string) =>
    request<void>("/api/account", {
      method: "PUT",
      body: JSON.stringify({ workspaceName, notificationEmail, allowedDomains }),
    }),
  changePlan: (planTier: "free" | "pro") =>
    request<{ planTier: string; monthlyQuota: number }>("/api/account/plan", {
      method: "POST",
      body: JSON.stringify({ planTier }),
    }),
  listMembers: () => request<{ members: Member[]; pending: PendingInvite[] }>("/api/account/members"),
  invite: (email: string, role: "member" | "owner") =>
    request<{ message: string }>("/api/account/invite", { method: "POST", body: JSON.stringify({ email, role }) }),
  removeMember: (userId: string) => request<void>(`/api/account/members/${userId}`, { method: "DELETE" }),
  cancelInvite: (id: string) => request<void>(`/api/account/invites/${id}`, { method: "DELETE" }),

  // ── Forms & Lists ──
  listForms: (kind: "form" | "list" = "form") => request<FormSummary[]>(`/api/forms?kind=${kind}`),
  createForm: (name: string, kind: "form" | "list" = "form") =>
    request<{ id: string; accessKey: string; name: string; kind: string }>("/api/forms", {
      method: "POST",
      body: JSON.stringify({ name, kind }),
    }),
  updateForm: (id: string, body: Partial<FormSummary> & { captchaSecret?: string }) =>
    request<void>(`/api/forms/${id}`, { method: "PUT", body: JSON.stringify(body) }),
  deleteForm: (id: string) => request<void>(`/api/forms/${id}`, { method: "DELETE" }),

  // ── Submissions ──
  listSubmissions: (params: SubmissionFilters = {}) =>
    request<SubmissionPage>(`/api/submissions?${submissionQuery(params)}`),

  submissionStats: (days = 30) =>
    request<{ days: number; series: { date: string; count: number }[] }>(`/api/submissions/stats?days=${days}`),

  patchSubmission: (id: string, body: { isRead?: boolean; isStarred?: boolean }) =>
    request<void>(`/api/submissions/${id}`, { method: "PATCH", body: JSON.stringify(body) }),

  deleteSubmission: (id: string) => request<void>(`/api/submissions/${id}`, { method: "DELETE" }),

  bulkSubmissions: (ids: string[], action: "delete" | "read" | "unread" | "star" | "unstar") =>
    request<{ affected: number }>("/api/submissions/bulk", {
      method: "POST",
      body: JSON.stringify({ ids, action }),
    }),

  /** Fetches the export with auth and triggers a browser download. */
  exportSubmissions: async (params: SubmissionFilters & { format: "csv" | "json" }) => {
    const q = submissionQuery(params);
    const res = await fetch(`${API_BASE}/api/submissions/export?${q}&format=${params.format}`, {
      headers: getToken() ? { Authorization: `Bearer ${getToken()}` } : {},
    });
    if (!res.ok) throw new Error("Export failed");
    const blob = await res.blob();
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = `submissions.${params.format}`;
    document.body.appendChild(a);
    a.click();
    a.remove();
    URL.revokeObjectURL(url);
  },

  // ── Webhooks ──
  listWebhooks: (formId: string) => request<Webhook[]>(`/api/webhooks?formId=${formId}`),
  createWebhook: (formId: string, url: string) =>
    request<Webhook>("/api/webhooks", { method: "POST", body: JSON.stringify({ formId, url }) }),
  deleteWebhook: (id: string) => request<void>(`/api/webhooks/${id}`, { method: "DELETE" }),

  // ── API keys (Submissions API) ──
  listApiKeys: () => request<ApiKeyInfo[]>("/api/apikeys"),
  createApiKey: (label: string) =>
    request<ApiKeyInfo & { fullKey: string }>("/api/apikeys", {
      method: "POST",
      body: JSON.stringify({ label }),
    }),
  revokeApiKey: (id: string) => request<void>(`/api/apikeys/${id}/revoke`, { method: "POST" }),
};
