import { useEffect, useState } from "react";
import { CheckCircle2, AlertCircle, Info, X } from "lucide-react";

// ─────────────────────────────── Toasts ───────────────────────────────
type ToastType = "success" | "error" | "info";
interface ToastItem { id: number; message: string; type: ToastType; }

let toastSeq = 0;
const toastListeners = new Set<(t: ToastItem[]) => void>();
let toasts: ToastItem[] = [];

function emit() { toastListeners.forEach((l) => l([...toasts])); }

export function toast(message: string, type: ToastType = "success") {
  const id = ++toastSeq;
  toasts = [...toasts, { id, message, type }];
  emit();
  setTimeout(() => { toasts = toasts.filter((t) => t.id !== id); emit(); }, 3500);
}

const toastIcon = { success: CheckCircle2, error: AlertCircle, info: Info };
const toastColor = {
  success: "text-green-600",
  error: "text-red-600",
  info: "text-brand-600",
};

export function Toaster() {
  const [items, setItems] = useState<ToastItem[]>([]);
  useEffect(() => { toastListeners.add(setItems); return () => { toastListeners.delete(setItems); }; }, []);

  return (
    <div className="pointer-events-none fixed bottom-6 right-6 z-50 flex flex-col gap-2">
      {items.map((t) => {
        const Icon = toastIcon[t.type];
        return (
          <div key={t.id}
            className="pointer-events-auto flex items-center gap-3 rounded-xl border border-slate-200 bg-white px-4 py-3 shadow-lg shadow-slate-300/40 animate-[slideIn_0.2s_ease-out]">
            <Icon size={18} className={toastColor[t.type]} />
            <span className="text-sm text-slate-800">{t.message}</span>
          </div>
        );
      })}
    </div>
  );
}

// ─────────────────────────────── Confirm modal ───────────────────────────────
interface ConfirmOptions { title: string; message?: string; confirmLabel?: string; danger?: boolean; }
interface ConfirmState extends ConfirmOptions { resolve: (ok: boolean) => void; }

let confirmListener: ((s: ConfirmState | null) => void) | null = null;

export function confirmDialog(options: ConfirmOptions): Promise<boolean> {
  return new Promise((resolve) => {
    confirmListener?.({ ...options, resolve });
  });
}

export function ConfirmHost() {
  const [state, setState] = useState<ConfirmState | null>(null);
  useEffect(() => { confirmListener = setState; return () => { confirmListener = null; }; }, []);

  if (!state) return null;
  const close = (ok: boolean) => { state.resolve(ok); setState(null); };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/40 p-4" onClick={() => close(false)}>
      <div className="w-full max-w-sm rounded-2xl bg-white p-6 shadow-xl" onClick={(e) => e.stopPropagation()}>
        <div className="mb-2 flex items-start justify-between">
          <h3 className="text-lg font-semibold text-slate-900">{state.title}</h3>
          <button onClick={() => close(false)} className="text-slate-400 hover:text-slate-600"><X size={18} /></button>
        </div>
        {state.message && <p className="mb-5 text-sm text-slate-500">{state.message}</p>}
        <div className="flex justify-end gap-2">
          <button onClick={() => close(false)}
            className="rounded-lg border border-slate-200 bg-white px-4 py-2 text-sm font-medium text-slate-700 hover:bg-slate-50">
            Cancel
          </button>
          <button onClick={() => close(true)}
            className={`rounded-lg px-4 py-2 text-sm font-medium text-white ${state.danger ? "bg-red-600 hover:bg-red-700" : "bg-brand-600 hover:bg-brand-700"}`}>
            {state.confirmLabel ?? "Confirm"}
          </button>
        </div>
      </div>
    </div>
  );
}
