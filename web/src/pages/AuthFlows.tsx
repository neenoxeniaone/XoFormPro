import { useEffect, useState } from "react";
import { Link, useNavigate, useSearchParams } from "react-router-dom";
import { CheckCircle2, AlertCircle } from "lucide-react";
import { api, setToken } from "../api";
import { Button, Field, Input } from "../components/ui";
import { Brand } from "../components/Brand";

function Shell({ title, subtitle, children }: { title: string; subtitle?: string; children: React.ReactNode }) {
  return (
    <div className="relative flex min-h-screen items-center justify-center overflow-hidden bg-slate-50 p-4">
      <div className="pointer-events-none absolute -top-40 -right-40 h-96 w-96 rounded-full bg-brand-200/40 blur-3xl" />
      <div className="relative w-full max-w-md">
        <div className="mb-6 flex flex-col items-center text-center">
          <Brand size={40} textClassName="text-xl text-slate-900" />
          <h1 className="mt-3 text-lg font-semibold tracking-tight text-slate-900">{title}</h1>
          {subtitle && <p className="mt-1 text-sm text-slate-500">{subtitle}</p>}
        </div>
        <div className="rounded-2xl border border-slate-200/80 bg-white p-8 shadow-xl shadow-slate-200/60">{children}</div>
      </div>
    </div>
  );
}

export function ForgotPasswordPage() {
  const [email, setEmail] = useState("");
  const [done, setDone] = useState(false);
  const [busy, setBusy] = useState(false);

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    setBusy(true);
    try { await api.forgotPassword(email); setDone(true); } finally { setBusy(false); }
  };

  return (
    <Shell title="Forgot password" subtitle="We'll email you a reset link.">
      {done ? (
        <div className="text-center">
          <CheckCircle2 className="mx-auto mb-3 text-green-600" size={32} />
          <p className="text-sm text-slate-600">If that email has an account, a reset link is on its way.</p>
          <Link to="/login" className="mt-4 inline-block text-sm font-semibold text-brand-600 hover:underline">Back to sign in</Link>
        </div>
      ) : (
        <form onSubmit={submit} className="space-y-4">
          <Field label="Email">
            <Input type="email" value={email} onChange={(e) => setEmail(e.target.value)} placeholder="you@company.com" required />
          </Field>
          <Button type="submit" disabled={busy} className="w-full">{busy ? "Sending…" : "Send reset link"}</Button>
          <p className="text-center text-sm text-slate-500">
            <Link to="/login" className="font-medium text-brand-600 hover:underline">Back to sign in</Link>
          </p>
        </form>
      )}
    </Shell>
  );
}

export function ResetPasswordPage() {
  const [params] = useSearchParams();
  const token = params.get("token") ?? "";
  const [password, setPassword] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [done, setDone] = useState(false);

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    setBusy(true); setError(null);
    try { await api.resetPassword(token, password); setDone(true); }
    catch (err) { setError(err instanceof Error ? err.message : "Reset failed"); }
    finally { setBusy(false); }
  };

  return (
    <Shell title="Set a new password">
      {done ? (
        <div className="text-center">
          <CheckCircle2 className="mx-auto mb-3 text-green-600" size={32} />
          <p className="text-sm text-slate-600">Your password has been updated.</p>
          <Link to="/login" className="mt-4 inline-block text-sm font-semibold text-brand-600 hover:underline">Sign in</Link>
        </div>
      ) : (
        <form onSubmit={submit} className="space-y-4">
          <Field label="New password">
            <Input type="password" value={password} onChange={(e) => setPassword(e.target.value)} placeholder="At least 5 characters" required />
          </Field>
          {error && <p className="rounded-lg bg-red-50 px-3 py-2 text-sm text-red-600">{error}</p>}
          <Button type="submit" disabled={busy} className="w-full">{busy ? "Saving…" : "Update password"}</Button>
        </form>
      )}
    </Shell>
  );
}

export function VerifyEmailPage() {
  const [params] = useSearchParams();
  const token = params.get("token") ?? "";
  const [state, setState] = useState<"loading" | "ok" | "error">("loading");

  useEffect(() => {
    api.verifyEmail(token).then(() => setState("ok")).catch(() => setState("error"));
  }, [token]);

  return (
    <Shell title="Email verification">
      <div className="text-center">
        {state === "loading" && <p className="text-sm text-slate-500">Verifying…</p>}
        {state === "ok" && (<>
          <CheckCircle2 className="mx-auto mb-3 text-green-600" size={32} />
          <p className="text-sm text-slate-600">Your email is verified. Thank you!</p>
          <Link to="/login" className="mt-4 inline-block text-sm font-semibold text-brand-600 hover:underline">Continue</Link>
        </>)}
        {state === "error" && (<>
          <AlertCircle className="mx-auto mb-3 text-red-600" size={32} />
          <p className="text-sm text-slate-600">This verification link is invalid or has expired.</p>
        </>)}
      </div>
    </Shell>
  );
}

export function AcceptInvitePage({ onAuthed }: { onAuthed: () => void }) {
  const [params] = useSearchParams();
  const token = params.get("token") ?? "";
  const navigate = useNavigate();
  const [password, setPassword] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    setBusy(true); setError(null);
    try {
      const res = await api.acceptInvite(token, password);
      setToken(res.token);
      onAuthed();
      navigate("/overview");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Could not accept invitation");
    } finally { setBusy(false); }
  };

  return (
    <Shell title="Join the workspace" subtitle="Set a password to accept your invitation.">
      <form onSubmit={submit} className="space-y-4">
        <Field label="Create a password">
          <Input type="password" value={password} onChange={(e) => setPassword(e.target.value)} placeholder="At least 5 characters" required />
        </Field>
        {error && <p className="rounded-lg bg-red-50 px-3 py-2 text-sm text-red-600">{error}</p>}
        <Button type="submit" disabled={busy} className="w-full">{busy ? "Joining…" : "Accept invitation"}</Button>
      </form>
    </Shell>
  );
}
