import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { FileText, Inbox, Plus, Mails, UserPlus } from "lucide-react";
import { api, type AccountInfo, type FormSummary, type Submission } from "../api";
import { Badge, Button, Card, PageHeader, Skeleton } from "../components/ui";

export function OverviewPage() {
  const [account, setAccount] = useState<AccountInfo | null>(null);
  const [forms, setForms] = useState<FormSummary[]>([]);
  const [lists, setLists] = useState<FormSummary[]>([]);
  const [formSubs, setFormSubs] = useState(0);
  const [listSubs, setListSubs] = useState(0);
  const [recent, setRecent] = useState<Submission[]>([]);
  const [trend, setTrend] = useState<{ date: string; count: number }[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    Promise.all([
      api.getAccount(),
      api.listForms(),
      api.listForms("list"),
      api.listSubmissions({ page: 1 }),
      api.listSubmissions({ formKind: "form", page: 1 }),
      api.listSubmissions({ formKind: "list", page: 1 }),
      api.submissionStats(30),
    ])
      .then(([a, f, l, s, fs, ls, st]) => {
        setAccount(a);
        setForms(f);
        setLists(l);
        setRecent(s.items.slice(0, 5));
        setFormSubs(fs.total);
        setListSubs(ls.total);
        setTrend(st.series);
      })
      .finally(() => setLoading(false));
  }, []);

  if (loading || !account)
    return (
      <div className="space-y-6">
        <Skeleton className="h-8 w-40" />
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
          {[0, 1, 2, 3].map((i) => <Card key={i} className="h-24 p-5"><Skeleton className="h-full w-full" /></Card>)}
        </div>
        <Card className="h-28 p-5"><Skeleton className="h-full w-full" /></Card>
      </div>
    );

  const stats = [
    { label: "Forms", value: forms.length, icon: FileText, tint: "bg-blue-50 text-blue-600" },
    { label: "Lists", value: lists.length, icon: Mails, tint: "bg-brand-50 text-brand-600" },
    { label: "Form submissions", value: formSubs.toLocaleString(), icon: Inbox, tint: "bg-emerald-50 text-emerald-600" },
    { label: "List submissions", value: listSubs.toLocaleString(), icon: UserPlus, tint: "bg-amber-50 text-amber-600" },
  ];

  return (
    <div className="space-y-6">
      <PageHeader
        title="Overview"
        subtitle={account.name}
        actions={<Link to="/forms"><Button><Plus size={16} /> New form</Button></Link>}
      />

      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        {stats.map((s) => (
          <Card key={s.label} className="p-5">
            <div className="flex items-center justify-between">
              <p className="text-sm text-slate-500">{s.label}</p>
              <span className={`flex h-8 w-8 items-center justify-center rounded-lg ${s.tint}`}>
                <s.icon size={16} />
              </span>
            </div>
            <p className="mt-2 text-2xl font-bold text-slate-900">{s.value}</p>
          </Card>
        ))}
      </div>

      <Card className="p-5">
        <div className="mb-4 flex items-center justify-between">
          <h2 className="font-semibold text-slate-900">Submissions — last 30 days</h2>
          <span className="text-sm text-slate-500">{trend.reduce((a, s) => a + s.count, 0)} total</span>
        </div>
        <BarChart data={trend} />
      </Card>

      <Card className="p-5">
        <div className="mb-3 flex items-center justify-between">
          <h2 className="font-semibold text-slate-900">Recent submissions</h2>
          <Link to="/submissions" className="text-sm text-brand-600 hover:underline">View all</Link>
        </div>
        {recent.length === 0 ? (
          <p className="text-sm text-slate-500">No submissions yet.</p>
        ) : (
          <ul className="divide-y divide-slate-100">
            {recent.map((s) => (
              <li key={s.id} className="flex items-center justify-between gap-3 py-3">
                <span className="min-w-0 truncate text-sm text-slate-700">
                  {Object.entries(s.data).slice(0, 2).map(([k, v]) => `${k}: ${String(v)}`).join("  •  ") || s.subject}
                </span>
                <span className="shrink-0 text-xs text-slate-400">{new Date(s.createdAt).toLocaleDateString()}</span>
              </li>
            ))}
          </ul>
        )}
      </Card>

      {forms.length === 0 && (
        <Card className="p-8 text-center">
          <p className="text-sm text-slate-500">You don't have any forms yet.</p>
          <Link to="/forms"><Button className="mt-3">Create your first form</Button></Link>
          <span className="ml-2 align-middle"><Badge color="brand">Get started</Badge></span>
        </Card>
      )}
    </div>
  );
}

/** Lightweight dependency-free SVG bar chart. */
function BarChart({ data }: { data: { date: string; count: number }[] }) {
  if (data.length === 0) return <p className="py-8 text-center text-sm text-slate-400">No data yet.</p>;
  const max = Math.max(1, ...data.map((d) => d.count));
  const barW = 100 / data.length;
  const gap = barW * 0.3;
  const w = barW - gap;

  // Evenly spaced date ticks (incl. first & last) so the axis stays readable with many bars.
  const tickCount = Math.min(6, data.length);
  const ticks = Array.from({ length: tickCount }, (_, i) => {
    const idx = Math.round((i * (data.length - 1)) / (tickCount - 1));
    return { idx, label: data[idx].date.slice(5).replace("-", "/") };
  });

  return (
    <div>
      <svg viewBox="0 0 100 41" preserveAspectRatio="none" className="h-32 w-full">
        {/* baseline */}
        <line x1={0} y1={40} x2={100} y2={40} className="stroke-slate-200 dark:stroke-slate-700" strokeWidth={0.3} />
        {data.map((d, i) => {
          const x = i * barW + gap / 2;
          // Non-zero days always get a visible sliver, even when far below the max.
          const h = d.count > 0 ? Math.max(1.5, (d.count / max) * 38) : 0;
          return (
            <g key={d.date}>
              {/* faint full-height track so every day in the window is visible */}
              <rect x={x} y={2} width={w} height={38} rx={0.6} className="fill-slate-100 dark:fill-slate-800/70" />
              {h > 0 && <rect x={x} y={40 - h} width={w} height={h} rx={0.6} className="fill-brand-500" />}
              <title>{`${d.date}: ${d.count}`}</title>
            </g>
          );
        })}
      </svg>
      <div className="relative mt-1 h-4 text-xs text-slate-400">
        {ticks.map((t, i) => {
          const first = i === 0;
          const last = i === ticks.length - 1;
          return (
            <span
              key={t.idx}
              className={`absolute whitespace-nowrap ${first ? "" : last ? "-translate-x-full" : "-translate-x-1/2"}`}
              style={{ left: first ? 0 : last ? "100%" : `${((t.idx + 0.5) / data.length) * 100}%` }}
            >
              {t.label}
            </span>
          );
        })}
      </div>
    </div>
  );
}
