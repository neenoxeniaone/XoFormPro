import { useEffect, useState } from "react";
import { Building2, Users } from "lucide-react";
import { api, type AccountInfo, type ApiKeyInfo, type Member, type PendingInvite } from "../api";
import { Badge, Button, Card, Field, Input, PageHeader, SectionHeader } from "../components/ui";
import { CopyBlock } from "../components/CopyBlock";
import { toast, confirmDialog } from "../components/feedback";

// Plan/billing + Submissions-API keys are hidden for now. Set to true to re-enable.
const SHOW_BILLING_AND_API = false;

export function AccountPage() {
  const [account, setAccount] = useState<AccountInfo | null>(null);
  const [name, setName] = useState("");
  const [email, setEmail] = useState("");
  const [allowedDomains, setAllowedDomains] = useState("");
  const [savedAt, setSavedAt] = useState<string | null>(null);

  const load = () =>
    api.getAccount().then((a) => {
      setAccount(a);
      setName(a.name);
      setEmail(a.notificationEmail);
      setAllowedDomains(a.allowedDomains ?? "");
    });

  useEffect(() => {
    load();
  }, []);

  if (!account) return <p className="text-sm text-slate-500">Loading…</p>;

  const saveProfile = async () => {
    await api.updateAccount(name, email, allowedDomains);
    setSavedAt(new Date().toLocaleTimeString());
    toast("Profile saved");
    await load();
  };

  const changePlan = async (tier: "free" | "pro") => {
    await api.changePlan(tier);
    await load();
  };

  return (
    <div className="space-y-6">
      <PageHeader title="Account" subtitle="Manage your workspace profile." />

      {/* Profile */}
      <Card className="p-5">
        <SectionHeader icon={Building2} title="Workspace profile" className="mb-4" />
        <div className="grid gap-4 sm:grid-cols-2">
          <Field label="Workspace name">
            <Input value={name} onChange={(e) => setName(e.target.value)} />
          </Field>
          <Field label="Notification email">
            <Input type="email" value={email} onChange={(e) => setEmail(e.target.value)} />
          </Field>
        </div>
        <div className="mt-4">
          <Field label="Allowed domains" hint="Applies to ALL forms and lists. Comma-separated (e.g. example.com, www.example.com). Leave blank to allow any site.">
            <Input value={allowedDomains} onChange={(e) => setAllowedDomains(e.target.value)} placeholder="example.com, www.example.com" />
          </Field>
        </div>
        <div className="mt-4 flex items-center gap-3">
          <Button onClick={saveProfile}>Save profile</Button>
          {savedAt && <span className="text-xs text-slate-400">Saved at {savedAt}</span>}
        </div>
      </Card>

      <TeamCard />

      {/* Plan / billing + API keys — hidden for now (flip SHOW_BILLING_AND_API to re-enable). */}
      {SHOW_BILLING_AND_API && (
        <>
          <Card className="p-5">
            <h2 className="mb-1 font-semibold text-slate-900">Plan &amp; billing</h2>
            <p className="mb-4 text-sm text-slate-500">
              Current plan:{" "}
              {account.planTier === "pro" ? <Badge color="brand">Pro</Badge> : <Badge>Free</Badge>}{" "}
              — {account.monthlyQuota.toLocaleString()} submissions/month.
            </p>
            <div className="grid gap-4 sm:grid-cols-2">
              <PlanCard
                title="Free"
                price="$0"
                features={["250 submissions/mo", "Honeypot + hCaptcha", "Email notifications", "30-day retention"]}
                current={account.planTier !== "pro"}
                action={account.planTier === "pro" ? <Button variant="ghost" onClick={() => changePlan("free")}>Downgrade</Button> : undefined}
              />
              <PlanCard
                title="Pro"
                price="$10/mo"
                highlight
                features={["25,000 submissions/mo", "File uploads + webhooks", "Autoresponder", "Submissions API", "reCaptcha + Turnstile"]}
                current={account.planTier === "pro"}
                action={account.planTier !== "pro" ? <Button onClick={() => changePlan("pro")}>Upgrade to Pro</Button> : undefined}
              />
            </div>
            <p className="mt-3 text-xs text-slate-400">Billing is a development stub — upgrading is instant and free for now.</p>
          </Card>

          <ApiKeysCard isPro={account.planTier === "pro"} />
        </>
      )}
    </div>
  );
}

function PlanCard({
  title, price, features, current, highlight, action,
}: {
  title: string; price: string; features: string[]; current: boolean; highlight?: boolean; action?: React.ReactNode;
}) {
  return (
    <div className={`rounded-xl border p-5 ${highlight ? "border-brand-300 bg-brand-50/40" : "border-slate-200"}`}>
      <div className="flex items-center justify-between">
        <h3 className="font-semibold text-slate-900">{title}</h3>
        {current && <Badge color="green">Current</Badge>}
      </div>
      <p className="mt-1 text-2xl font-bold text-slate-900">{price}</p>
      <ul className="mt-3 space-y-1 text-sm text-slate-600">
        {features.map((f) => (
          <li key={f}>• {f}</li>
        ))}
      </ul>
      {action && <div className="mt-4">{action}</div>}
    </div>
  );
}

function ApiKeysCard({ isPro }: { isPro: boolean }) {
  const [keys, setKeys] = useState<ApiKeyInfo[]>([]);
  const [label, setLabel] = useState("");
  const [newKey, setNewKey] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const load = () => api.listApiKeys().then(setKeys);
  useEffect(() => {
    load();
  }, []);

  const create = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    try {
      const res = await api.createApiKey(label || "API key");
      setNewKey(res.fullKey);
      setLabel("");
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to create key");
    }
  };

  const revoke = async (id: string) => {
    if (!confirm("Revoke this key? Requests using it will immediately fail.")) return;
    await api.revokeApiKey(id);
    await load();
  };

  return (
    <Card className="p-5">
      <h2 className="mb-1 font-semibold text-slate-900">API keys (Submissions API)</h2>
      <p className="mb-4 text-sm text-slate-500">
        Read your submissions programmatically via <code>GET /v1/submissions</code>. Pro feature.
        {!isPro && " Upgrade to Pro to use the Submissions API."}
      </p>

      {newKey && (
        <div className="mb-4 rounded-lg border border-amber-200 bg-amber-50 p-3">
          <p className="mb-2 text-sm font-medium text-amber-800">
            Copy your key now — it won't be shown again.
          </p>
          <CopyBlock code={newKey} language="text" />
        </div>
      )}

      <form onSubmit={create} className="flex items-end gap-3">
        <div className="flex-1">
          <Field label="Create a key">
            <Input value={label} onChange={(e) => setLabel(e.target.value)} placeholder="Production backend" />
          </Field>
        </div>
        <Button type="submit">Generate key</Button>
      </form>
      {error && <p className="mt-2 text-sm text-red-600">{error}</p>}

      {keys.length > 0 && (
        <ul className="mt-4 divide-y divide-slate-100">
          {keys.map((k) => (
            <li key={k.id} className="flex items-center justify-between gap-3 py-3">
              <div className="min-w-0">
                <p className="truncate text-sm font-medium text-slate-800">{k.label}</p>
                <p className="font-mono text-xs text-slate-400">
                  {k.prefix}
                  {k.lastUsedAt ? ` · last used ${new Date(k.lastUsedAt).toLocaleDateString()}` : " · never used"}
                </p>
              </div>
              {k.revokedAt ? (
                <Badge color="red">Revoked</Badge>
              ) : (
                <button onClick={() => revoke(k.id)} className="text-xs text-red-600 hover:underline">
                  Revoke
                </button>
              )}
            </li>
          ))}
        </ul>
      )}
    </Card>
  );
}

function TeamCard() {
  const [members, setMembers] = useState<Member[]>([]);
  const [pending, setPending] = useState<PendingInvite[]>([]);
  const [me, setMe] = useState<string>("");
  const [myRole, setMyRole] = useState<string>("member");
  const [email, setEmail] = useState("");
  const [busy, setBusy] = useState(false);

  const load = async () => {
    const [list, who] = await Promise.all([api.listMembers(), api.me()]);
    setMembers(list.members);
    setPending(list.pending);
    setMe(who.userId);
    setMyRole(who.role);
  };
  useEffect(() => { load(); }, []);

  const isOwner = myRole === "owner";

  const invite = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!email.trim()) return;
    setBusy(true);
    try {
      await api.invite(email.trim(), "member");
      toast("Invitation sent");
      setEmail("");
      await load();
    } catch (err) {
      toast(err instanceof Error ? err.message : "Failed to invite", "error");
    } finally { setBusy(false); }
  };

  const remove = async (m: Member) => {
    if (!(await confirmDialog({ title: `Remove ${m.email}?`, confirmLabel: "Remove", danger: true }))) return;
    await api.removeMember(m.id);
    toast("Member removed");
    await load();
  };
  const cancel = async (p: PendingInvite) => {
    await api.cancelInvite(p.id);
    toast("Invitation cancelled");
    await load();
  };

  return (
    <Card className="p-5">
      <SectionHeader icon={Users} title="Team members" description="Invite people to collaborate in this workspace." className="mb-4" />

      {isOwner && (
        <form onSubmit={invite} className="mb-4 flex items-end gap-3">
          <div className="flex-1">
            <Field label="Invite by email">
              <Input type="email" value={email} onChange={(e) => setEmail(e.target.value)} placeholder="teammate@company.com" />
            </Field>
          </div>
          <Button type="submit" disabled={busy}>{busy ? "Sending…" : "Send invite"}</Button>
        </form>
      )}

      <ul className="divide-y divide-slate-100">
        {members.map((m) => (
          <li key={m.id} className="flex items-center justify-between gap-3 py-3">
            <div className="min-w-0">
              <p className="truncate text-sm font-medium text-slate-800">
                {m.email} {m.id === me && <span className="text-slate-400">(you)</span>}
              </p>
              <p className="text-xs text-slate-400">Joined {new Date(m.createdAt).toLocaleDateString()}</p>
            </div>
            <div className="flex items-center gap-3">
              <Badge color={m.role === "owner" ? "brand" : "slate"}>{m.role}</Badge>
              {isOwner && m.id !== me && (
                <button onClick={() => remove(m)} className="text-xs text-red-600 hover:underline">Remove</button>
              )}
            </div>
          </li>
        ))}
        {pending.map((p) => (
          <li key={p.id} className="flex items-center justify-between gap-3 py-3">
            <div className="min-w-0">
              <p className="truncate text-sm text-slate-600">{p.email}</p>
              <p className="text-xs text-slate-400">Invited {new Date(p.createdAt).toLocaleDateString()}</p>
            </div>
            <div className="flex items-center gap-3">
              <Badge color="amber">pending</Badge>
              {isOwner && <button onClick={() => cancel(p)} className="text-xs text-red-600 hover:underline">Cancel</button>}
            </div>
          </li>
        ))}
      </ul>
    </Card>
  );
}
