import { useState } from "react";

/** Inline value (e.g. an access key) shown in mono with a solid copy button. */
export function CopyInline({ value, className = "" }: { value: string; className?: string }) {
  const [copied, setCopied] = useState(false);

  const copy = async (e: React.MouseEvent) => {
    e.preventDefault();
    e.stopPropagation();
    await navigator.clipboard.writeText(value);
    setCopied(true);
    setTimeout(() => setCopied(false), 1500);
  };

  return (
    <span className={`inline-flex items-center gap-2 rounded-lg border border-slate-300 bg-white py-1 pl-3 pr-1 shadow-sm ${className}`}>
      <code className="truncate font-mono text-sm text-slate-800">{value}</code>
      <button
        onClick={copy}
        title="Copy access key"
        className={`shrink-0 rounded-md px-2.5 py-1 text-xs font-semibold transition ${
          copied ? "bg-green-600 text-white" : "bg-brand-600 text-white hover:bg-brand-700"
        }`}
      >
        {copied ? "✓ Copied" : "Copy"}
      </button>
    </span>
  );
}
