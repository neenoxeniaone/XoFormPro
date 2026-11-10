import { useEffect, useMemo, useState } from "react";
import { useSearchParams } from "react-router-dom";
import { Inbox, Search, Star, Trash2, Download, Mail, MailOpen, Globe, Monitor, Clock, FileText, Mails } from "lucide-react";
import { api, type FormSummary, type Submission } from "../api";
import { Badge, Button, Card, EmptyState, Input, PageHeader, Skeleton } from "../components/ui";
import { toast, confirmDialog } from "../components/feedback";

export function SubmissionsPage() {
  const [searchParams] = useSearchParams();
  const initialFormId = searchParams.get("formId") ?? "";

  const [kindTab, setKindTab] = useState<"form" | "list">("form");
  const [forms, setForms] = useState<FormSummary[]>([]);
  const [data, setData] = useState<Submission[]>([]);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [formId, setFormId] = useState(initialFormId);
  const [q, setQ] = useState("");
  const [search, setSearch] = useState("");
  const [sort, setSort] = useState<"newest" | "oldest">("newest");
  const [includeSpam, setIncludeSpam] = useState(false);
  const [starredOnly, setStarredOnly] = useState(false);
  const [unreadOnly, setUnreadOnly] = useState(false);
  const [loading, setLoading] = useState(true);
  const [expanded, setExpanded] = useState<string | null>(null);
  const [selected, setSelected] = useState<Set<string>>(new Set());

  // Load the forms/lists for the filter dropdown whenever the tab changes.
  useEffect(() => {
    api.listForms(kindTab).then(setForms);
  }, [kindTab]);

  const isList = kindTab === "list";

  const filters = useMemo(
    () => ({ formId: formId || undefined, includeSpam, starredOnly, unreadOnly, q: search || undefined, sort, formKind: kindTab, page }),
    [formId, includeSpam, starredOnly, unreadOnly, search, sort, kindTab, page]
  );

  const load = () => {
    setLoading(true);
    api.listSubmissions(filters)
      .then((res) => { setData(res.items); setTotal(res.total); })
      .finally(() => setLoading(false));
  };

  useEffect(() => { load(); setSelected(new Set()); /* eslint-disable-next-line */ }, [JSON.stringify(filters)]);

  const pageSize = 25;
  const pages = Math.max(1, Math.ceil(total / pageSize));

  const toggleStar = async (s: Submission) => {
    await api.patchSubmission(s.id, { isStarred: !s.isStarred });
    setData((d) => d.map((x) => (x.id === s.id ? { ...x, isStarred: !x.isStarred } : x)));
  };
  const markRead = async (s: Submission, read: boolean) => {
    await api.patchSubmission(s.id, { isRead: read });
    setData((d) => d.map((x) => (x.id === s.id ? { ...x, isRead: read } : x)));
  };
  const remove = async (s: Submission) => {
    if (!(await confirmDialog({ title: "Delete submission?", message: "This can't be undone.", confirmLabel: "Delete", danger: true }))) return;
    await api.deleteSubmission(s.id);
    toast("Submission deleted");
    load();
  };
  const bulk = async (action: "delete" | "read" | "star") => {
    if (action === "delete" && !(await confirmDialog({ title: `Delete ${selected.size} submission(s)?`, message: "This can't be undone.", confirmLabel: "Delete", danger: true }))) return;
    const res = await api.bulkSubmissions([...selected], action);
    toast(`${res.affected} submission(s) updated`);
    setSelected(new Set());
    load();
  };
  const toggleSelect = (id: string) =>
    setSelected((s) => { const n = new Set(s); n.has(id) ? n.delete(id) : n.add(id); return n; });

  const onSearch = (e: React.FormEvent) => { e.preventDefault(); setPage(1); setSearch(q); };

  const switchTab = (k: "form" | "list") => {
    setKindTab(k);
    setFormId("");
    setPage(1);
    setSelected(new Set());
  };

  return (
    <div className="space-y-5">
      <PageHeader
        title="Submissions"
        subtitle={`${total} ${isList ? "subscriber" : "submission"}${total === 1 ? "" : "s"} ${formId ? (isList ? "for this list" : "for this form") : (isList ? "across all lists" : "across all forms")}`}
        actions={
          <div className="flex gap-2">
            <Button variant="ghost" size="sm" onClick={() => api.exportSubmissions({ ...filters, format: "csv" })}>
              <Download size={14} /> CSV
            </Button>
            <Button variant="ghost" size="sm" onClick={() => api.exportSubmissions({ ...filters, format: "json" })}>
              <Download size={14} /> JSON
            </Button>
          </div>
        }
      />

      {/* Forms vs Lists tabs */}
      <div className="inline-flex gap-1 rounded-lg bg-slate-100 p-1 dark:bg-slate-800">
        <TabButton active={!isList} onClick={() => switchTab("form")} icon={FileText} label="Form submissions" />
        <TabButton active={isList} onClick={() => switchTab("list")} icon={Mails} label="List subscribers" />
      </div>

      {/* Toolbar */}
      <Card className="flex flex-wrap items-center gap-3 p-3">
        <form onSubmit={onSearch} className="relative flex-1 min-w-[200px]">
          <Search size={16} className="absolute left-3 top-1/2 -translate-y-1/2 text-slate-400" />
          <Input value={q} onChange={(e) => setQ(e.target.value)} placeholder={isList ? "Search subscribers…" : "Search submissions…"} className="pl-9" />
        </form>
        <select value={formId} onChange={(e) => { setFormId(e.target.value); setPage(1); }}
          className="rounded-lg border border-slate-200 bg-white px-3 py-2 text-sm dark:border-slate-700 dark:bg-slate-900">
          <option value="">{isList ? "All lists" : "All forms"}</option>
          {forms.map((f) => <option key={f.id} value={f.id}>{f.name}</option>)}
        </select>
        <select value={sort} onChange={(e) => setSort(e.target.value as "newest" | "oldest")}
          className="rounded-lg border border-slate-200 bg-white px-3 py-2 text-sm">
          <option value="newest">Newest first</option>
          <option value="oldest">Oldest first</option>
        </select>
        <FilterChip active={unreadOnly} onClick={() => { setUnreadOnly(!unreadOnly); setPage(1); }}>Unread</FilterChip>
        <FilterChip active={starredOnly} onClick={() => { setStarredOnly(!starredOnly); setPage(1); }}>Starred</FilterChip>
        <FilterChip active={includeSpam} onClick={() => { setIncludeSpam(!includeSpam); setPage(1); }}>Spam</FilterChip>
      </Card>

      {/* Bulk action bar */}
      {selected.size > 0 && (
        <div className="flex items-center gap-3 rounded-xl bg-brand-50 px-4 py-2.5 text-sm">
          <span className="font-medium text-brand-700">{selected.size} selected</span>
          <button onClick={() => bulk("read")} className="text-brand-700 hover:underline">Mark read</button>
          <button onClick={() => bulk("star")} className="text-brand-700 hover:underline">Star</button>
          <button onClick={() => bulk("delete")} className="text-red-600 hover:underline">Delete</button>
          <button onClick={() => setSelected(new Set())} className="ml-auto text-slate-500 hover:underline">Clear</button>
        </div>
      )}

      {loading ? (
        <div className="space-y-2">
          {[0, 1, 2, 3].map((i) => (
            <Card key={i} className="flex items-center justify-between p-4">
              <div className="space-y-2"><Skeleton className="h-4 w-40" /><Skeleton className="h-3 w-64" /></div>
              <Skeleton className="h-3 w-20" />
            </Card>
          ))}
        </div>
      ) : data.length === 0 ? (
        <EmptyState icon={Inbox} title="No submissions found"
          description="Try adjusting your filters, or share your form to start collecting responses." />
      ) : (
        <div className="space-y-2">
          {data.map((s) => {
            const isOpen = expanded === s.id;
            // Lead with the submitter's email/name; fall back to the subject only if neither exists.
            const heading = s.senderEmail
              || (s.data.email as string)
              || (s.data.name as string)
              || s.subject;
            const preview = Object.entries(s.data).slice(0, 3).map(([k, v]) => `${k}: ${String(v)}`).join("  •  ");
            return (
              <Card key={s.id} className={`overflow-hidden ${!s.isRead ? "border-l-4 border-l-brand-400" : ""}`}>
                <div className="flex items-center gap-3 p-4">
                  <input type="checkbox" checked={selected.has(s.id)} onChange={() => toggleSelect(s.id)} />
                  <button onClick={() => toggleStar(s)} title="Star" className="shrink-0">
                    <Star size={16} className={s.isStarred ? "fill-amber-400 text-amber-400" : "text-slate-300 hover:text-amber-400"} />
                  </button>
                  <button className="flex min-w-0 flex-1 items-center gap-3 text-left"
                    onClick={() => { setExpanded(isOpen ? null : s.id); if (!s.isRead) markRead(s, true); }}>
                    <div className="min-w-0 flex-1">
                      <div className="flex items-center gap-2">
                        <span className={`truncate ${s.isRead ? "text-slate-700 dark:text-slate-300" : "font-semibold text-slate-900 dark:text-slate-100"}`}>{heading}</span>
                        {s.isSpam && <Badge color="red">Spam</Badge>}
                      </div>
                      <p className="mt-0.5 truncate text-sm text-slate-500">{preview}</p>
                    </div>
                    <span className="shrink-0 text-xs text-slate-400">{new Date(s.createdAt).toLocaleString()}</span>
                  </button>
                  <button onClick={() => markRead(s, !s.isRead)} title={s.isRead ? "Mark unread" : "Mark read"} className="shrink-0 text-slate-400 hover:text-slate-600">
                    {s.isRead ? <Mail size={15} /> : <MailOpen size={15} />}
                  </button>
                  <button onClick={() => remove(s)} title="Delete" className="shrink-0 text-slate-400 hover:text-red-600">
                    <Trash2 size={15} />
                  </button>
                </div>
                {isOpen && (
                  <div className="border-t border-slate-100 bg-slate-50 p-4">
                    <table className="w-full text-sm">
                      <tbody>
                        {Object.entries(s.data).map(([k, v]) => (
                          <tr key={k} className="border-b border-slate-100 last:border-0">
                            <td className="py-2 pr-4 align-top font-medium text-slate-600">{k}</td>
                            <td className="py-2 whitespace-pre-wrap text-slate-800">{String(v)}</td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                    <div className="mt-3 flex flex-wrap gap-4 border-t border-slate-200 pt-3 text-xs text-slate-500">
                      <span className="inline-flex items-center gap-1"><Clock size={13} /> {new Date(s.createdAt).toLocaleString()}</span>
                      {s.ipAddress && <span className="inline-flex items-center gap-1"><Globe size={13} /> {s.ipAddress}</span>}
                      {s.userAgent && <span className="inline-flex max-w-md items-center gap-1 truncate"><Monitor size={13} /> {s.userAgent}</span>}
                    </div>
                  </div>
                )}
              </Card>
            );
          })}
        </div>
      )}

      {pages > 1 && (
        <div className="flex items-center justify-center gap-3">
          <Button variant="ghost" size="sm" disabled={page <= 1} onClick={() => setPage((p) => p - 1)}>Previous</Button>
          <span className="text-sm text-slate-500">Page {page} of {pages}</span>
          <Button variant="ghost" size="sm" disabled={page >= pages} onClick={() => setPage((p) => p + 1)}>Next</Button>
        </div>
      )}
    </div>
  );
}

function FilterChip({ active, onClick, children }: { active: boolean; onClick: () => void; children: React.ReactNode }) {
  return (
    <button onClick={onClick}
      className={`rounded-lg border px-3 py-2 text-sm font-medium transition ${
        active
          ? "border-brand-300 bg-brand-50 text-brand-700 dark:border-brand-700 dark:bg-brand-500/10 dark:text-brand-300"
          : "border-slate-200 bg-white text-slate-600 hover:bg-slate-50 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-300"
      }`}>
      {children}
    </button>
  );
}

function TabButton({ active, onClick, icon: Icon, label }: { active: boolean; onClick: () => void; icon: typeof FileText; label: string }) {
  return (
    <button onClick={onClick}
      className={`inline-flex items-center gap-1.5 rounded-md px-3 py-1.5 text-sm font-medium transition ${
        active ? "bg-white text-brand-700 shadow-sm dark:bg-slate-700 dark:text-brand-300" : "text-slate-500 hover:text-slate-700 dark:text-slate-400"
      }`}>
      <Icon size={15} /> {label}
    </button>
  );
}
