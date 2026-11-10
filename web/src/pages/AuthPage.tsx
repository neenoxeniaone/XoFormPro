import { useState } from "react";
import { Link } from "react-router-dom";
import { api, setToken } from "../api";
import { Button, Field, Input } from "../components/ui";
import { Brand } from "../components/Brand";

/** Combined sign in / sign up screen. */
export function AuthPage({ onAuthed }: { onAuthed: () => void }) {
  const [mode, setMode] = useState<"login" | "register">("login");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [workspace, setWorkspace] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    setBusy(true);
    setError(null);
    try {
      const res =
        mode === "login"
          ? await api.login(email, password)
          : await api.register(email, password, workspace || undefined);
      setToken(res.token);
      onAuthed();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Something went wrong");
    } finally {
      setBusy(false);
    }
  };

  return (
    <div className="relative flex min-h-screen items-center justify-center overflow-hidden bg-slate-50 p-4">
      {/* Decorative gradient blobs */}
      <div className="pointer-events-none absolute -top-40 -right-40 h-96 w-96 rounded-full bg-brand-200/40 blur-3xl" />
      <div className="pointer-events-none absolute -bottom-40 -left-40 h-96 w-96 rounded-full bg-accent-200/40 blur-3xl" />

      <div className="relative w-full max-w-md">
        <div className="mb-6 flex flex-col items-center text-center">
          <Brand size={44} textClassName="text-2xl text-slate-900" />
          <p className="mt-3 text-sm text-slate-500">
            {mode === "login" ? "Welcome back — sign in to continue" : "Create your free workspace"}
          </p>
        </div>

        <div className="rounded-2xl border border-slate-200/80 bg-white p-8 shadow-xl shadow-slate-200/60">
          <form onSubmit={submit} className="space-y-4">
            {mode === "register" && (
              <Field label="Workspace name" hint="Optional — defaults to your email.">
                <Input value={workspace} onChange={(e) => setWorkspace(e.target.value)} placeholder="Acme Inc" />
              </Field>
            )}
            <Field label="Email">
              <Input type="email" value={email} onChange={(e) => setEmail(e.target.value)} placeholder="you@company.com" required />
            </Field>
            <Field label="Password">
              <Input
                type="password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                placeholder={mode === "register" ? "At least 5 characters" : "••••••••"}
                required
              />
            </Field>
            {mode === "login" && (
              <div className="-mt-2 text-right">
                <Link to="/forgot-password" className="text-sm font-medium text-brand-600 hover:underline">Forgot password?</Link>
              </div>
            )}
            {error && (
              <p className="rounded-lg bg-red-50 px-3 py-2 text-sm text-red-600">{error}</p>
            )}
            <Button type="submit" disabled={busy} className="w-full">
              {busy ? "Please wait…" : mode === "login" ? "Sign in" : "Create workspace"}
            </Button>
          </form>

          <p className="mt-5 text-center text-sm text-slate-500">
            {mode === "login" ? "No account yet?" : "Already have an account?"}{" "}
            <button
              onClick={() => { setMode(mode === "login" ? "register" : "login"); setError(null); }}
              className="font-semibold text-brand-600 hover:underline"
            >
              {mode === "login" ? "Sign up" : "Sign in"}
            </button>
          </p>
        </div>
      </div>
    </div>
  );
}
