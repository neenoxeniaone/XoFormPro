import { useCallback, useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { Code2, SlidersHorizontal, Reply, Webhook as WebhookIcon, Trash2, ArrowLeft, Mail } from "lucide-react";
import { Link } from "react-router-dom";
import { api, API_BASE, type FormSummary, type Webhook } from "../api";
import { Badge, Button, Card, Field, Input, SectionHeader } from "../components/ui";
import { CopyBlock } from "../components/CopyBlock";
import { CopyInline } from "../components/CopyInline";
import { toast, confirmDialog } from "../components/feedback";

// Absolute URLs for the embeddable snippet (external sites need the full address).
const SUBMIT_URL = `${location.origin}${API_BASE}/submit`;
const SCRIPT_URL = `${location.origin}${API_BASE}/forms.js`;

// Captcha widget + provider script, injected only when the form requires a captcha.
function captchaParts(form: FormSummary) {
  if (!form.requireCaptcha) return { widget: "", script: "" };
  const siteKey = form.captchaSiteKey || "YOUR_SITE_KEY";
  if (form.captchaProvider === "hcaptcha")
    return {
      widget: `  <div class="h-captcha" data-sitekey="${siteKey}"></div>\n`,
      script: `<script src="https://js.hcaptcha.com/1/api.js" async defer></script>\n`,
    };
  if (form.captchaProvider === "turnstile")
    return {
      widget: `  <div class="cf-turnstile" data-sitekey="${siteKey}"></div>\n`,
      script: `<script src="https://challenges.cloudflare.com/turnstile/v0/api.js" async defer></script>\n`,
    };
  return { widget: "", script: "" };
}

function htmlSnippet(form: FormSummary) {
  const { widget, script } = captchaParts(form);
  return `<form action="${SUBMIT_URL}" method="POST" data-success="formSuccess">
  <input type="hidden" name="access_key" value="${form.accessKey}" />

  <input type="text" name="name" placeholder="Your Name" required />
  <input type="email" name="email" placeholder="Your Email" required />
  <textarea name="message" placeholder="Your Message" required></textarea>

  <!-- Honeypot spam protection (keep hidden) -->
  <input type="checkbox" name="botcheck" style="display:none" />
${widget}  <button type="submit">Send</button>
</form>

<!-- Shown on success — stays on your page, no redirect. Style it however you like. -->
<div id="formSuccess">✅ Thanks! Your message has been sent.</div>

${script}<!-- Include once per page. Handles every XoFormPro form automatically. -->
<script src="${SCRIPT_URL}" async></script>`;
}

function fetchSnippet(key: string) {
  return `const res = await fetch("${SUBMIT_URL}", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify({
    access_key: "${key}",
    name: "Jane Doe",
    email: "jane@example.com",
    message: "Hello!",
  }),
});
const data = await res.json();
console.log(data); // { success: true, ... }`;
}

function reactSnippet(form: FormSummary) {
  const provider = form.requireCaptcha ? form.captchaProvider : "none";
  const siteKey = form.captchaSiteKey || "YOUR_SITE_KEY";
  const scriptSrc =
    provider === "hcaptcha" ? "https://js.hcaptcha.com/1/api.js"
    : provider === "turnstile" ? "https://challenges.cloudflare.com/turnstile/v0/api.js"
    : "";
  const widgetClass = provider === "hcaptcha" ? "h-captcha" : provider === "turnstile" ? "cf-turnstile" : "";
  const hasCaptcha = scriptSrc !== "";

  const imports = hasCaptcha ? `import { useState, useEffect } from "react";` : `import { useState } from "react";`;
  const effect = hasCaptcha
    ? `
  // Load the captcha script once.
  useEffect(() => {
    const s = document.createElement("script");
    s.src = "${scriptSrc}";
    s.async = true; s.defer = true;
    document.body.appendChild(s);
    return () => { document.body.removeChild(s); };
  }, []);
`
    : "";
  const widget = hasCaptcha ? `      <div className="${widgetClass}" data-sitekey="${siteKey}"></div>\n` : "";

  return `${imports}

export default function ContactForm() {
  const [result, setResult] = useState("");
${effect}
  async function onSubmit(event) {
    event.preventDefault();
    setResult("Sending…");

    const formData = new FormData(event.target);
    formData.append("access_key", "${form.accessKey}");

    const res = await fetch("${SUBMIT_URL}", {
      method: "POST",
      body: formData,
    });
    const data = await res.json();

    if (data.success) {
      setResult("Thanks! Your message was sent.");
      event.target.reset();
    } else {
      setResult(data.message);
    }
  }

  return (
    <form onSubmit={onSubmit}>
      <input type="text" name="name" placeholder="Your Name" required />
      <input type="email" name="email" placeholder="Your Email" required />
      <textarea name="message" placeholder="Your Message" required />
${widget}      <button type="submit">Send</button>
      <p>{result}</p>
    </form>
  );
}`;
}

const tabs = [
  { key: "html", label: "HTML" },
  { key: "js", label: "JavaScript" },
  { key: "react", label: "React" },
] as const;

function snippetFor(tab: (typeof tabs)[number]["key"], form: FormSummary) {
  if (tab === "html") return htmlSnippet(form);
  if (tab === "js") return fetchSnippet(form.accessKey);
  return reactSnippet(form);
}

export function FormDetailPage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const [form, setForm] = useState<FormSummary | null>(null);
  const [captchaSecret, setCaptchaSecret] = useState(""); // write-only; never returned by API
  const [tab, setTab] = useState<(typeof tabs)[number]["key"]>("html");
  const [saving, setSaving] = useState(false);
  const [savedAt, setSavedAt] = useState<string | null>(null);

  const reload = useCallback(() => {
    api.listForms().then((forms) => setForm(forms.find((f) => f.id === id) ?? null));
  }, [id]);

  useEffect(() => { reload(); }, [reload]);

  if (!form) return <p className="text-sm text-slate-500">Loading…</p>;

  const patch = (changes: Partial<FormSummary>) => setForm({ ...form, ...changes });

  const save = async () => {
    setSaving(true);
    try {
      await api.updateForm(form.id, {
        name: form.name,
        toEmailOverride: form.toEmailOverride,
        defaultSubject: form.defaultSubject,
        fromName: form.fromName,
        redirectUrl: form.redirectUrl,
        isActive: form.isActive,
        requireCaptcha: form.requireCaptcha,
        requireHumanCheck: form.requireHumanCheck,
        allowedDomains: form.allowedDomains,
        captchaProvider: form.captchaProvider,
        captchaSiteKey: form.captchaSiteKey,
        ...(captchaSecret ? { captchaSecret } : {}),
        autoresponderEnabled: form.autoresponderEnabled,
        autoresponderSubject: form.autoresponderSubject,
        autoresponderBody: form.autoresponderBody,
        emailIntro: form.emailIntro,
        successTitle: form.successTitle,
        successMessage: form.successMessage,
      } as Partial<FormSummary> & { captchaSecret?: string });
      setSavedAt(new Date().toLocaleTimeString());
      toast("Settings saved");
    } catch (err) {
      toast(err instanceof Error ? err.message : "Failed to save", "error");
    } finally {
      setSaving(false);
    }
  };

  const remove = async () => {
    if (!(await confirmDialog({ title: `Delete "${form.name}"?`, message: "This removes the form and all its submissions.", confirmLabel: "Delete", danger: true }))) return;
    await api.deleteForm(form.id);
    toast("Form deleted");
    navigate("/forms");
  };

  return (
    <div className="space-y-6">
      <Link to="/forms" className="inline-flex items-center gap-1.5 text-sm font-medium text-slate-500 hover:text-slate-700">
        <ArrowLeft size={16} /> Back to forms
      </Link>
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-slate-900">{form.name}</h1>
          <div className="mt-2 flex items-center gap-2">
            <span className="text-sm font-medium text-slate-600">Access key</span>
            <CopyInline value={form.accessKey} />
          </div>
        </div>
        {form.isActive ? <Badge color="green">Active</Badge> : <Badge color="slate">Paused</Badge>}
      </div>

      {/* Integration snippets */}
      <Card className="p-5">
        <SectionHeader icon={Code2} title="Implementation Example" className="mb-4" />
        <div className="mb-3 inline-flex gap-1 rounded-lg bg-slate-100 p-1">
          {tabs.map((t) => (
            <button
              key={t.key}
              onClick={() => setTab(t.key)}
              className={`rounded-md px-3 py-1.5 text-sm font-medium transition ${
                tab === t.key ? "bg-white text-brand-700 shadow-sm" : "text-slate-500 hover:text-slate-700"
              }`}
            >
              {t.label}
            </button>
          ))}
        </div>
        <CopyBlock
          code={snippetFor(tab, form)}
          language={tab === "html" ? "html" : tab === "react" ? "jsx" : "javascript"}
        />
      </Card>

      {/* Settings */}
      <Card className="p-5">
        <SectionHeader icon={SlidersHorizontal} title="Settings" className="mb-4" />
        <div className="grid gap-4 sm:grid-cols-2">
          <Field label="Form name">
            <Input value={form.name} onChange={(e) => patch({ name: e.target.value })} />
          </Field>
          <Field label="Recipient(s) override (optional)">
            <Input
              value={form.toEmailOverride ?? ""}
              onChange={(e) => patch({ toEmailOverride: e.target.value || null })}
              placeholder="Defaults to workspace email — separate multiple with commas"
            />
            <p className="mt-1 text-xs text-slate-400">Notify several people by separating addresses with commas, e.g. a@x.com, b@x.com</p>
          </Field>
          <Field label="Default subject">
            <Input value={form.defaultSubject} onChange={(e) => patch({ defaultSubject: e.target.value })} />
          </Field>
          <Field label="From name">
            <Input value={form.fromName} onChange={(e) => patch({ fromName: e.target.value })} />
          </Field>
          <Field label="Allowed domains (comma-separated)" hint="Subdomains of a listed host are allowed; nothing else is. Blank allows any site.">
            <Input
              value={form.allowedDomains ?? ""}
              onChange={(e) => patch({ allowedDomains: e.target.value || null })}
              placeholder="laili.co.uk, www.laili.co.uk"
            />
          </Field>
          <Field label="Success redirect URL (optional)">
            <Input
              value={form.redirectUrl ?? ""}
              onChange={(e) => patch({ redirectUrl: e.target.value || null })}
              placeholder="https://yoursite.com/thank-you"
            />
          </Field>
        </div>

        <div className="mt-4 flex flex-wrap gap-6">
          <label className="flex items-center gap-2 text-sm text-slate-700">
            <input
              type="checkbox"
              checked={form.isActive}
              onChange={(e) => patch({ isActive: e.target.checked })}
            />
            Accepting submissions
          </label>
          <label className="flex items-center gap-2 text-sm text-slate-700">
            <input
              type="checkbox"
              checked={form.requireHumanCheck}
              onChange={(e) => patch({ requireHumanCheck: e.target.checked })}
            />
            Show "I'm not a robot"
          </label>
          <label className="flex items-center gap-2 text-sm text-slate-700">
            <input
              type="checkbox"
              checked={form.requireCaptcha}
              onChange={(e) => patch({ requireCaptcha: e.target.checked })}
            />
            Require captcha
          </label>
        </div>

        {form.requireHumanCheck && (
          <p className="mt-3 rounded-lg bg-slate-50 p-3 text-xs text-slate-500 dark:bg-slate-800/60 dark:text-slate-400">
            forms.js adds the tick box to your form automatically — no change to your site's HTML. Ticking it
            is paired with a single-use token issued when the page loads, so a script that posts straight to
            the endpoint can't simply send the field. Needs forms.js on the page; a form that posts without it
            will be rejected.
          </p>
        )}

        {form.requireCaptcha && (
          <div className="mt-4 grid gap-4 rounded-lg bg-slate-50 p-4 sm:grid-cols-2">
            <Field label="Captcha provider">
              <select
                value={form.captchaProvider}
                onChange={(e) => patch({ captchaProvider: e.target.value })}
                className="w-full rounded-lg border border-slate-200 bg-white px-3 py-2 text-sm"
              >
                <option value="none">None</option>
                <option value="hcaptcha">hCaptcha</option>
                <option value="turnstile">Cloudflare Turnstile</option>
              </select>
            </Field>
            <Field label="Secret key (write-only)">
              <Input
                type="password"
                value={captchaSecret}
                onChange={(e) => setCaptchaSecret(e.target.value)}
                placeholder="Leave blank to keep current"
              />
            </Field>
            <Field label="Site key (public)">
              <Input
                value={form.captchaSiteKey ?? ""}
                onChange={(e) => patch({ captchaSiteKey: e.target.value || null })}
                placeholder="Shown in the widget on your form"
              />
            </Field>
            <p className="text-xs text-slate-400 sm:col-span-2">
              The <strong>site key</strong> is public and gets embedded in the HTML example below; the{" "}
              <strong>secret key</strong> stays on the server for verification. Both come from your
              {form.captchaProvider === "turnstile" ? " Cloudflare Turnstile" : " hCaptcha"} dashboard.
            </p>
          </div>
        )}

        <div className="mt-6 flex items-center justify-between">
          <Button onClick={save} disabled={saving}>
            {saving ? "Saving…" : "Save changes"}
          </Button>
          {savedAt && <span className="text-xs text-slate-400">Saved at {savedAt}</span>}
        </div>
      </Card>

      {/* Autoresponder (Pro) */}
      <Card className="p-5">
        <SectionHeader
          icon={Reply}
          title="Autoresponder"
          className="mb-4"
          action={
            <label className="flex items-center gap-2 text-sm text-slate-700">
              <input
                type="checkbox"
                checked={form.autoresponderEnabled}
                onChange={(e) => patch({ autoresponderEnabled: e.target.checked })}
              />
              Enabled
            </label>
          }
        />
        <p className="mb-4 text-sm text-slate-500">
          Sends a confirmation email back to the submitter (uses their <code>email</code> field). Pro feature.
        </p>
        {form.autoresponderEnabled && (
          <div className="space-y-4">
            <Field label="Subject">
              <Input
                value={form.autoresponderSubject ?? ""}
                onChange={(e) => patch({ autoresponderSubject: e.target.value })}
                placeholder="Thanks for your submission"
              />
            </Field>
            <Field label="Message">
              <textarea
                value={form.autoresponderBody ?? ""}
                onChange={(e) => patch({ autoresponderBody: e.target.value })}
                placeholder="We received your message and will get back to you soon."
                rows={3}
                className="w-full rounded-lg border border-slate-200 bg-white px-3 py-2 text-sm outline-none focus:border-brand-500 focus:ring-2 focus:ring-brand-100"
              />
            </Field>
          </div>
        )}
      </Card>

      {/* Email & success page customization */}
      <Card className="p-5">
        <SectionHeader icon={Mail} title="Email & success page" description="Personalize the notification email and the default success page." className="mb-4" />
        <div className="space-y-4">
          <Field label="Notification email intro" hint="Replaces the default 'A new form has been submitted…' line.">
            <Input value={form.emailIntro ?? ""} onChange={(e) => patch({ emailIntro: e.target.value || null })}
              placeholder="You've received a new contact request." />
          </Field>
          <div className="grid gap-4 sm:grid-cols-2">
            <Field label="Success page title">
              <Input value={form.successTitle ?? ""} onChange={(e) => patch({ successTitle: e.target.value || null })}
                placeholder="Form submitted successfully!" />
            </Field>
            <Field label="Success page message">
              <Input value={form.successMessage ?? ""} onChange={(e) => patch({ successMessage: e.target.value || null })}
                placeholder="Thank you — we'll get back to you soon." />
            </Field>
          </div>
          <p className="text-xs text-slate-400">Shown when a form posts without a redirect URL. Save via the Settings panel above.</p>
        </div>
      </Card>

      <WebhooksCard formId={form.id} />

      <Card className="border-red-200/70 p-5">
        <SectionHeader
          icon={Trash2}
          title="Danger zone"
          description="Permanently delete this form and its submissions."
          action={<Button variant="danger" onClick={remove}>Delete form</Button>}
        />
      </Card>
    </div>
  );
}

function WebhooksCard({ formId }: { formId: string }) {
  const [hooks, setHooks] = useState<Webhook[]>([]);
  const [url, setUrl] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const load = () => api.listWebhooks(formId).then(setHooks);
  useEffect(() => {
    load();
  }, [formId]);

  const add = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!url.trim()) return;
    setBusy(true);
    setError(null);
    try {
      await api.createWebhook(formId, url.trim());
      setUrl("");
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to add webhook");
    } finally {
      setBusy(false);
    }
  };

  const remove = async (id: string) => {
    if (!(await confirmDialog({ title: "Remove webhook?", confirmLabel: "Remove", danger: true }))) return;
    await api.deleteWebhook(id);
    toast("Webhook removed");
    await load();
  };

  return (
    <Card className="p-5">
      <SectionHeader icon={WebhookIcon} title="Webhooks" className="mb-1" />
      <p className="mb-4 mt-1 text-sm text-slate-500">
        POST each submission as JSON to your endpoints (Zapier, Make, Slack, Discord, Google Sheets…). Pro feature.
      </p>

      <form onSubmit={add} className="flex items-end gap-3">
        <div className="flex-1">
          <Field label="Endpoint URL">
            <Input value={url} onChange={(e) => setUrl(e.target.value)} placeholder="https://hooks.zapier.com/…" />
          </Field>
        </div>
        <Button type="submit" disabled={busy}>
          {busy ? "Adding…" : "Add webhook"}
        </Button>
      </form>
      {error && <p className="mt-2 text-sm text-red-600">{error}</p>}

      {hooks.length > 0 && (
        <ul className="mt-4 divide-y divide-slate-100">
          {hooks.map((h) => (
            <li key={h.id} className="flex items-center justify-between gap-3 py-3">
              <span className="min-w-0 truncate font-mono text-xs text-slate-600">{h.url}</span>
              <div className="flex items-center gap-3">
                {h.isActive ? <Badge color="green">Active</Badge> : <Badge>Off</Badge>}
                <button onClick={() => remove(h.id)} className="text-xs text-red-600 hover:underline">
                  Remove
                </button>
              </div>
            </li>
          ))}
        </ul>
      )}
    </Card>
  );
}
