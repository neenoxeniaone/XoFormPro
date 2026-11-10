import { useEffect, useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import { Moon, Sun } from "lucide-react";
import { clearToken, type CurrentUser } from "../api";
import { getTheme, toggleTheme, type Theme } from "../theme";

/** Top-right account dropdown: avatar → email, plan, Account settings, Sign out. */
export function UserMenu({ user }: { user: CurrentUser | null }) {
  const [open, setOpen] = useState(false);
  const [theme, setTheme] = useState<Theme>(getTheme());
  const ref = useRef<HTMLDivElement>(null);
  const navigate = useNavigate();

  // Close on outside click.
  useEffect(() => {
    const onClick = (e: MouseEvent) => {
      if (ref.current && !ref.current.contains(e.target as Node)) setOpen(false);
    };
    document.addEventListener("mousedown", onClick);
    return () => document.removeEventListener("mousedown", onClick);
  }, []);

  const initial = (user?.email?.[0] ?? "?").toUpperCase();

  return (
    <div className="relative" ref={ref}>
      <button
        onClick={() => setOpen((o) => !o)}
        className="flex items-center gap-2 rounded-full border border-slate-200 bg-white py-1 pl-1 pr-3 text-sm hover:bg-slate-50 dark:border-slate-700 dark:bg-slate-800 dark:hover:bg-slate-700"
      >
        <span className="flex h-7 w-7 items-center justify-center rounded-full bg-brand-600 text-xs font-semibold text-white">
          {initial}
        </span>
        <span className="hidden max-w-[160px] truncate text-slate-700 sm:block dark:text-slate-200">{user?.email ?? "Account"}</span>
        <svg className="h-4 w-4 text-slate-400" viewBox="0 0 20 20" fill="currentColor">
          <path fillRule="evenodd" d="M5.23 7.21a.75.75 0 011.06.02L10 11.06l3.71-3.83a.75.75 0 111.08 1.04l-4.25 4.39a.75.75 0 01-1.08 0L5.21 8.27a.75.75 0 01.02-1.06z" clipRule="evenodd" />
        </svg>
      </button>

      {open && (
        <div className="absolute right-0 z-20 mt-2 w-60 overflow-hidden rounded-xl border border-slate-200 bg-white shadow-lg dark:border-slate-700 dark:bg-slate-800">
          <div className="border-b border-slate-100 px-4 py-3 dark:border-slate-700">
            <p className="truncate text-sm font-medium text-slate-900 dark:text-slate-100">{user?.email}</p>
            <p className="mt-0.5 text-xs text-slate-500 dark:text-slate-400">
              {user?.workspaceName} · {user?.planTier === "pro" ? "Pro plan" : "Free plan"}
            </p>
          </div>
          <button
            onClick={() => setTheme(toggleTheme())}
            className="flex w-full items-center gap-2 px-4 py-2.5 text-left text-sm text-slate-700 hover:bg-slate-50 dark:text-slate-200 dark:hover:bg-slate-700"
          >
            {theme === "dark" ? <Sun size={16} /> : <Moon size={16} />}
            {theme === "dark" ? "Light mode" : "Dark mode"}
          </button>
          <button
            onClick={() => { setOpen(false); navigate("/account"); }}
            className="flex w-full items-center gap-2 border-t border-slate-100 px-4 py-2.5 text-left text-sm text-slate-700 hover:bg-slate-50 dark:border-slate-700 dark:text-slate-200 dark:hover:bg-slate-700"
          >
            Account settings
          </button>
          <button
            onClick={() => { clearToken(); location.reload(); }}
            className="flex w-full items-center gap-2 border-t border-slate-100 px-4 py-2.5 text-left text-sm text-red-600 hover:bg-red-50 dark:border-slate-700 dark:hover:bg-red-500/10"
          >
            Sign out
          </button>
        </div>
      )}
    </div>
  );
}
