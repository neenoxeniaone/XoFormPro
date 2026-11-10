import { useEffect, useState } from "react";
import { NavLink, Outlet, Route, Routes, Navigate } from "react-router-dom";
import { LayoutDashboard, FileText, Inbox, Settings } from "lucide-react";
import { api, clearToken, getToken, type CurrentUser } from "./api";
import { UserMenu } from "./components/UserMenu";
import { Brand } from "./components/Brand";
import { AuthPage } from "./pages/AuthPage";
import { LandingPage } from "./pages/LandingPage";
import { OverviewPage } from "./pages/OverviewPage";
import { FormsPage } from "./pages/FormsPage";
import { FormDetailPage } from "./pages/FormDetailPage";
import { SubmissionsPage } from "./pages/SubmissionsPage";
import { AccountPage } from "./pages/AccountPage";
import { ListDetailPage } from "./pages/ListDetailPage";
import { ForgotPasswordPage, ResetPasswordPage, VerifyEmailPage, AcceptInvitePage } from "./pages/AuthFlows";

const nav = [
  { to: "/overview", label: "Overview", icon: LayoutDashboard },
  { to: "/forms", label: "Forms & Lists", icon: FileText },
  { to: "/submissions", label: "Submissions", icon: Inbox },
  { to: "/account", label: "Account", icon: Settings },
];

function Sidebar() {
  return (
    <aside className="hidden w-64 shrink-0 flex-col border-r border-slate-200 bg-white px-4 py-5 sm:flex dark:border-slate-800 dark:bg-slate-900">
      <div className="mb-8 px-2">
        <Brand size={34} />
      </div>
      <nav className="space-y-1">
        {nav.map(({ to, label, icon: Icon }) => (
          <NavLink key={to} to={to}
            className={({ isActive }) =>
              `flex items-center gap-3 rounded-lg px-3 py-2 text-sm font-medium transition ${
                isActive
                  ? "bg-brand-50 text-brand-700 dark:bg-brand-500/10 dark:text-brand-300"
                  : "text-slate-600 hover:bg-slate-50 hover:text-slate-900 dark:text-slate-400 dark:hover:bg-slate-800 dark:hover:text-slate-100"
              }`}>
            <Icon size={18} />
            {label}
          </NavLink>
        ))}
      </nav>
      <div className="mt-auto rounded-xl bg-gradient-to-br from-brand-50 to-slate-50 p-4 dark:from-slate-800 dark:to-slate-800/50">
        <p className="text-xs font-medium text-slate-700 dark:text-slate-200">Quick start</p>
        <p className="mt-0.5 text-xs text-slate-500 dark:text-slate-400">Create a form and copy its snippet to start collecting submissions.</p>
        <NavLink to="/forms" className="mt-2 inline-block text-xs font-semibold text-brand-600 hover:underline dark:text-brand-400">
          Go to forms →
        </NavLink>
      </div>
    </aside>
  );
}

function DashboardLayout({ user }: { user: CurrentUser | null }) {
  return (
    <div className="flex min-h-screen">
      <Sidebar />
      <div className="flex flex-1 flex-col">
        <header className="sticky top-0 z-10 flex h-16 shrink-0 items-center justify-between gap-2 border-b border-slate-200 bg-white/80 px-4 backdrop-blur sm:justify-end sm:px-8 dark:border-slate-800 dark:bg-slate-900/80">
          <span className="sm:hidden"><Brand size={26} /></span>
          <UserMenu user={user} />
        </header>
        <main className="flex-1 p-4 pb-20 sm:p-8 sm:pb-8">
          <div className="mx-auto max-w-5xl">
            <Outlet />
          </div>
        </main>
      </div>

      {/* Mobile bottom navigation */}
      <nav className="fixed inset-x-0 bottom-0 z-20 flex border-t border-slate-200 bg-white sm:hidden dark:border-slate-800 dark:bg-slate-900">
        {nav.map(({ to, label, icon: Icon }) => (
          <NavLink key={to} to={to}
            className={({ isActive }) =>
              `flex flex-1 flex-col items-center gap-0.5 py-2.5 text-[11px] font-medium ${
                isActive ? "text-brand-600 dark:text-brand-400" : "text-slate-500 dark:text-slate-400"
              }`}>
            <Icon size={18} />
            {label}
          </NavLink>
        ))}
      </nav>
    </div>
  );
}

export default function App() {
  const [token, setTok] = useState<string | null>(getToken());
  const [user, setUser] = useState<CurrentUser | null>(null);

  useEffect(() => {
    if (!token) return;
    api.me()
      .then(setUser)
      .catch(() => { clearToken(); setTok(null); });
  }, [token]);

  const onAuthed = () => setTok(getToken());

  return (
    <Routes>
      {/* Public auth-flow routes — available in any auth state */}
      <Route path="/forgot-password" element={<ForgotPasswordPage />} />
      <Route path="/reset-password" element={<ResetPasswordPage />} />
      <Route path="/verify-email" element={<VerifyEmailPage />} />
      <Route path="/accept-invite" element={<AcceptInvitePage onAuthed={onAuthed} />} />

      {token ? (
        <Route element={<DashboardLayout user={user} />}>
          <Route path="/" element={<Navigate to="/overview" replace />} />
          <Route path="/overview" element={<OverviewPage />} />
          <Route path="/forms" element={<FormsPage />} />
          <Route path="/forms/:id" element={<FormDetailPage />} />
          <Route path="/lists/:id" element={<ListDetailPage />} />
          <Route path="/submissions" element={<SubmissionsPage />} />
          <Route path="/account" element={<AccountPage />} />
          <Route path="*" element={<Navigate to="/overview" replace />} />
        </Route>
      ) : (
        <>
          <Route path="/login" element={<AuthPage onAuthed={onAuthed} />} />
          <Route path="*" element={<LandingPage />} />
        </>
      )}
    </Routes>
  );
}
