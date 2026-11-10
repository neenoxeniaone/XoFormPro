import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { FileText, Mails, Plus, Settings2, Inbox, Users } from "lucide-react";
import { api, type FormSummary } from "../api";
import { Badge, Button, Card, EmptyState, Field, Input, PageHeader, Skeleton } from "../components/ui";
import { CopyInline } from "../components/CopyInline";

type Kind = "form" | "list";

export function FormsPage() {
  const [items, setItems] = useState<FormSummary[]>([]);
  const [loading, setLoading] = useState(true);
  const [creating, setCreating] = useState(false);
  const [newName, setNewName] = useState("");
  const [kind, setKind] = useState<Kind>("form");
  const [error, setError] = useState<string | null>(null);

  const load = async () => {
    setLoading(true);
    try {
      // Fetch both kinds and merge (newest first).
      const [forms, lists] = await Promise.all([api.listForms("form"), api.listForms("list")]);
      setItems([...forms, ...lists].sort((a, b) => +new Date(b.createdAt) - +new Date(a.createdAt)));
    } catch (e) {
      setError(e instanceof Error ? e.message : "Failed to load");
    } finally {
      setLoading(false);
    }
  };
  useEffect(() => { load(); }, []);

  const create = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!newName.trim()) return;
    setCreating(true);
    try {
      await api.createForm(newName.trim(), kind);
      setNewName("");
      await load();
    } catch (e) {
      setError(e instanceof Error ? e.message : "Failed to create");
    } finally {
      setCreating(false);
    }
  };

  return (
    <div className="space-y-6">
      <PageHeader title="Forms & Lists" subtitle="Collect form submissions, or build email subscription lists." />

      {/* Unified create: pick a type, then name it */}
      <Card className="p-4">
        <form onSubmit={create} className="flex flex-wrap items-end gap-3">
          <div>
            <span className="mb-1.5 block text-sm font-medium text-slate-700 dark:text-slate-300">Type</span>
            <div className="inline-flex gap-1 rounded-lg bg-slate-100 p-1 dark:bg-slate-800">
              <TypeTab active={kind === "form"} onClick={() => setKind("form")} icon={FileText} label="Form" />
              <TypeTab active={kind === "list"} onClick={() => setKind("list")} icon={Mails} label="List" />
            </div>
          </div>
          <div className="flex-1 min-w-[200px]">
            <Field label={kind === "list" ? "New list name" : "New form name"}>
              <Input value={newName} onChange={(e) => setNewName(e.target.value)}
                placeholder={kind === "list" ? "Newsletter signups" : "Contact form"} />
            </Field>
          </div>
          <Button type="submit" disabled={creating}>
            <Plus size={16} /> {creating ? "Creating…" : kind === "list" ? "Create list" : "Create form"}
          </Button>
        </form>
        <p className="mt-2 text-xs text-slate-400">
          {kind === "list"
            ? "A list gives you an email + Subscribe snippet; subscribers are collected separately."
            : "A form captures any fields and shows submissions in the Submissions tab."}
        </p>
      </Card>

      {error && <p className="text-sm text-red-600">{error}</p>}

      {(() => {
        const visible = items.filter((f) => f.kind === kind);
        return loading ? (
        <div className="grid gap-4 sm:grid-cols-2">
          {[0, 1].map((i) => (
            <Card key={i} className="space-y-4 p-5">
              <Skeleton className="h-5 w-32" />
              <Skeleton className="h-8 w-full" />
              <Skeleton className="h-9 w-40" />
            </Card>
          ))}
        </div>
      ) : visible.length === 0 ? (
        <EmptyState icon={kind === "list" ? Mails : FileText}
          title={kind === "list" ? "No lists yet" : "No forms yet"}
          description={kind === "list"
            ? "Create a list above to collect email subscribers with a Subscribe button."
            : "Create a form above to get an access key and embed snippet."} />
      ) : (
        <div className="grid gap-4 sm:grid-cols-2">
          {visible.map((f) => {
            const isList = f.kind === "list";
            return (
              <Card key={f.id} className="p-5 transition hover:border-slate-300 hover:shadow-md dark:hover:border-slate-700">
                <div className="flex items-start justify-between">
                  <div className="min-w-0">
                    <div className="flex items-center gap-2">
                      {isList ? <Mails size={16} className="text-brand-500" /> : <FileText size={16} className="text-blue-500" />}
                      <h3 className="truncate font-semibold text-slate-900 dark:text-slate-100">{f.name}</h3>
                      <Badge color={isList ? "brand" : "slate"}>{isList ? "List" : "Form"}</Badge>
                    </div>
                    <div className="mt-1.5"><CopyInline value={f.accessKey} className="max-w-full" /></div>
                  </div>
                  {f.isActive ? <Badge color="green">Active</Badge> : <Badge color="slate">Paused</Badge>}
                </div>
                <div className="mt-4 flex items-center justify-between border-t border-slate-100 pt-4 dark:border-slate-800">
                  <span className="inline-flex items-center gap-1.5 text-sm text-slate-500 dark:text-slate-400">
                    {isList ? <Users size={15} /> : <Inbox size={15} />}
                    {f.submissionCount} {isList ? (f.submissionCount === 1 ? "subscriber" : "subscribers") : (f.submissionCount === 1 ? "submission" : "submissions")}
                  </span>
                  {isList ? (
                    <Link to={`/lists/${f.id}`}><Button variant="ghost" size="sm"><Settings2 size={14} /> Manage</Button></Link>
                  ) : (
                    <div className="flex gap-2">
                      <Link to={`/forms/${f.id}`}><Button variant="ghost" size="sm"><Settings2 size={14} /> Setup</Button></Link>
                      <Link to={`/submissions?formId=${f.id}`}><Button variant="ghost" size="sm"><Inbox size={14} /> Submissions</Button></Link>
                    </div>
                  )}
                </div>
              </Card>
            );
          })}
        </div>
      );
      })()}
    </div>
  );
}

function TypeTab({ active, onClick, icon: Icon, label }: { active: boolean; onClick: () => void; icon: typeof FileText; label: string }) {
  return (
    <button type="button" onClick={onClick}
      className={`inline-flex items-center gap-1.5 rounded-md px-3 py-1.5 text-sm font-medium transition ${
        active ? "bg-white text-brand-700 shadow-sm dark:bg-slate-700 dark:text-brand-300" : "text-slate-500 hover:text-slate-700 dark:text-slate-400"
      }`}>
      <Icon size={15} /> {label}
    </button>
  );
}
