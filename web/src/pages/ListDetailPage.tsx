import { useEffect, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { ArrowLeft, Code2, SlidersHorizontal, Users, Trash2, Download } from "lucide-react";
import { api, API_BASE, type FormSummary, type Submission } from "../api";
import { Badge, Button, Card, Field, Input, SectionHeader } from "../components/ui";
import { CopyBlock } from "../components/CopyBlock";
import { CopyInline } from "../components/CopyInline";
import { toast, confirmDialog } from "../components/feedback";

const SUBMIT_URL = `${location.origin}${API_BASE}/submit`;
const SCRIPT_URL = `${location.origin}${API_BASE}/forms.js`;

function htmlSnippet(key: string) {
  return `<form action="${SUBMIT_URL}" method="POST" data-success="subscribeSuccess">
  <input type="hidden" name="access_key" value="${key}" />
  <input type="email" name="email" placeholder="Enter your email" required />

  <!-- Honeypot spam protection (keep hidden) -->
  <input type="checkbox" name="botcheck" style="display:none" />

  <button type="submit">Subscribe</button>
</form>

<!-- Shown on success — stays on your page, no redirect. Style it however you like. -->
<div id="subscribeSuccess">✅ Thanks for subscribing!</div>

<!-- Include once per page. Handles every XoFormPro form automatically. -->
<script src="${SCRIPT_URL}" async></script>`;
}

export function ListDetailPage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const [list, setList] = useState<FormSummary | null>(null);
  const [subs, setSubs] = useState<Submission[]>([]);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [saving, setSaving] = useState(false);

  const loadList = () => api.listForms("list").then((all) => setList(all.find((f) => f.id === id) ?? null));
  const loadSubs = () => api.listSubmissions({ formId: id, page }).then((r) => { setSubs(r.items); setTotal(r.total); });

  useEffect(() => { loadList(); }, [id]);
  useEffect(() => { if (id) loadSubs(); }, [id, page]);

  if (!list) return <p className="text-sm text-slate-500">Loading…</p>;
  const patch = (c: Partial<FormSummary>) => setList({ ...list, ...c });

  const save = async () => {
    setSaving(true);
    try {
      await api.updateForm(list.id, {
        name: list.name,
        isActive: list.isActive,
        requireCaptcha: list.requireCaptcha,
        notifyByEmail: list.notifyByEmail,
        defaultSubject: list.defaultSubject,
        fromName: list.fromName,
        toEmailOverride: list.toEmailOverride,
        redirectUrl: list.redirectUrl,
        allowedDomains: list.allowedDomains,
        requireHumanCheck: list.requireHumanCheck,
        captchaProvider: list.captchaProvider,
        autoresponderEnabled: list.autoresponderEnabled,
        autoresponderSubject: list.autoresponderSubject,
        autoresponderBody: list.autoresponderBody,
        emailIntro: list.emailIntro,
        successTitle: list.successTitle,
        successMessage: list.successMessage,
      });
      toast("List saved");
    } catch (err) {
      toast(err instanceof Error ? err.message : "Failed to save", "error");
    } finally { setSaving(false); }
  };

  const removeSub = async (s: Submission) => {
    if (!(await confirmDialog({ title: "Remove subscriber?", confirmLabel: "Remove", danger: true }))) return;
    await api.deleteSubmission(s.id);
    toast("Subscriber removed");
    loadSubs();
  };

  const removeList = async () => {
    if (!(await confirmDialog({ title: `Delete "${list.name}"?`, message: "This removes the list and all subscribers.", confirmLabel: "Delete", danger: true }))) return;
    await api.deleteForm(list.id);
    toast("List deleted");
    navigate("/lists");
  };

  const pages = Math.max(1, Math.ceil(total / 25));

  return (
    <div className="space-y-6">
      <Link to="/lists" className="inline-flex items-center gap-1.5 text-sm font-medium text-slate-500 hover:text-slate-700 dark:text-slate-400">
        <ArrowLeft size={16} /> Back to lists
      </Link>
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-slate-900 dark:text-slate-100">{list.name}</h1>
          <div className="mt-2 flex items-center gap-2">
            <span className="text-sm font-medium text-slate-600 dark:text-slate-300">Access key</span>
            <CopyInline value={list.accessKey} />
          </div>
        </div>
        {list.isActive ? <Badge color="green">Active</Badge> : <Badge color="slate">Paused</Badge>}
      </div>

      {/* Subscribe snippet */}
      <Card className="p-5">
        <SectionHeader icon={Code2} title="Subscribe form" description="Paste this where you want the signup to appear." className="mb-4" />
        <CopyBlock code={htmlSnippet(list.accessKey)} language="html" />
      </Card>

      {/* Settings */}
      <Card className="p-5">
        <SectionHeader icon={SlidersHorizontal} title="Settings" className="mb-4" />
        <div className="grid gap-4 sm:grid-cols-2">
          <Field label="List name">
            <Input value={list.name} onChange={(e) => patch({ name: e.target.value })} />
          </Field>
          <Field label="Recipient(s) override (optional)">
            <Input
              value={list.toEmailOverride ?? ""}
              onChange={(e) => patch({ toEmailOverride: e.target.value || null })}
              placeholder="Defaults to workspace email — separate multiple with commas"
            />
          </Field>
          <Field label="Allowed domains (comma-separated)" hint="Subdomains of a listed host are allowed. Blank allows any site.">
            <Input
              value={list.allowedDomains ?? ""}
              onChange={(e) => patch({ allowedDomains: e.target.value || null })}
              placeholder="example.com, www.example.com"
            />
          </Field>
        </div>
        <div className="mt-4 flex flex-wrap gap-6">
          <label className="flex items-center gap-2 text-sm text-slate-700 dark:text-slate-300">
            <input type="checkbox" checked={list.isActive} onChange={(e) => patch({ isActive: e.target.checked })} />
            Accepting subscribers
          </label>
          <label className="flex items-center gap-2 text-sm text-slate-700 dark:text-slate-300">
            <input type="checkbox" checked={list.notifyByEmail} onChange={(e) => patch({ notifyByEmail: e.target.checked })} />
            Email me on each new subscriber
          </label>
          <label className="flex items-center gap-2 text-sm text-slate-700 dark:text-slate-300">
            <input type="checkbox" checked={list.requireHumanCheck} onChange={(e) => patch({ requireHumanCheck: e.target.checked })} />
            Show "I'm not a robot"
          </label>
          <label className="flex items-center gap-2 text-sm text-slate-700 dark:text-slate-300">
            <input type="checkbox" checked={list.requireCaptcha} onChange={(e) => patch({ requireCaptcha: e.target.checked })} />
            Require captcha
          </label>
        </div>
        <div className="mt-6"><Button onClick={save} disabled={saving}>{saving ? "Saving…" : "Save changes"}</Button></div>
      </Card>

      {/* Subscribers (separate from form submissions) */}
      <Card className="p-5">
        <SectionHeader
          icon={Users}
          title={`Subscribers (${total})`}
          className="mb-4"
          action={
            <div className="flex gap-2">
              <Button variant="ghost" size="sm" onClick={() => api.exportSubmissions({ formId: list.id, format: "csv" })}>
                <Download size={14} /> CSV
              </Button>
            </div>
          }
        />
        {subs.length === 0 ? (
          <p className="py-6 text-center text-sm text-slate-500 dark:text-slate-400">No subscribers yet.</p>
        ) : (
          <ul className="divide-y divide-slate-100 dark:divide-slate-800">
            {subs.map((s) => (
              <li key={s.id} className="flex items-center justify-between gap-3 py-3">
                <div className="min-w-0">
                  <p className="truncate text-sm font-medium text-slate-800 dark:text-slate-200">
                    {(s.data.email as string) || s.senderEmail || "(no email)"}
                  </p>
                  <p className="text-xs text-slate-400">{new Date(s.createdAt).toLocaleString()}</p>
                </div>
                <button onClick={() => removeSub(s)} className="shrink-0 text-slate-400 hover:text-red-600" title="Remove">
                  <Trash2 size={15} />
                </button>
              </li>
            ))}
          </ul>
        )}
        {pages > 1 && (
          <div className="mt-4 flex items-center justify-center gap-3">
            <Button variant="ghost" size="sm" disabled={page <= 1} onClick={() => setPage((p) => p - 1)}>Previous</Button>
            <span className="text-sm text-slate-500">Page {page} of {pages}</span>
            <Button variant="ghost" size="sm" disabled={page >= pages} onClick={() => setPage((p) => p + 1)}>Next</Button>
          </div>
        )}
      </Card>

      <Card className="border-red-200/70 p-5 dark:border-red-900/40">
        <SectionHeader icon={Trash2} title="Danger zone" description="Permanently delete this list and its subscribers."
          action={<Button variant="danger" onClick={removeList}>Delete list</Button>} />
      </Card>
    </div>
  );
}
