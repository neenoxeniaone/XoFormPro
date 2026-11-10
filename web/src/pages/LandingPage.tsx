import { Link } from "react-router-dom";
import { Zap, Mail, Code2, Sparkles, ArrowRight, Check } from "lucide-react";
import { Brand } from "../components/Brand";

const features = [
  { icon: Mail, title: "Email notifications", desc: "Submissions land in your inbox instantly — no server or backend code required." },
  { icon: Code2, title: "Works everywhere", desc: "Plain HTML, React, Vue, Next.js, Astro, Webflow — if it renders a form, it works." },
  { icon: Zap, title: "Fast & reliable", desc: "Durable delivery with retries, rate limiting, and submission analytics." },
];

const steps = [
  { n: "1", title: "Create an access key", desc: "Sign up and create a form to get a public access key." },
  { n: "2", title: "Add it to your form", desc: "Paste a small HTML snippet — point your form at our endpoint." },
  { n: "3", title: "Receive submissions", desc: "Get emails, view them in your dashboard, and trigger integrations." },
];

export function LandingPage() {
  return (
    <div className="min-h-screen bg-white text-slate-900">
      {/* Nav */}
      <header className="sticky top-0 z-10 border-b border-slate-100 bg-white/80 backdrop-blur">
        <div className="mx-auto flex max-w-6xl items-center justify-between px-6 py-4">
          <Brand size={30} textClassName="text-base text-slate-900" />
          <div className="flex items-center gap-3">
            <Link to="/login" className="text-sm font-medium text-slate-600 hover:text-slate-900">Sign in</Link>
            <Link to="/login" className="rounded-lg bg-brand-600 px-4 py-2 text-sm font-medium text-white hover:bg-brand-700">
              Get started
            </Link>
          </div>
        </div>
      </header>

      {/* Hero */}
      <section className="relative overflow-hidden">
        <div className="pointer-events-none absolute -top-32 left-1/2 h-96 w-[42rem] -translate-x-1/2 rounded-full bg-brand-200/40 blur-3xl" />
        <div className="relative mx-auto max-w-4xl px-6 py-24 text-center">
          <span className="inline-flex items-center gap-1.5 rounded-full border border-brand-200 bg-brand-50 px-3 py-1 text-xs font-medium text-brand-700">
            <Sparkles size={13} /> Forms without a backend
          </span>
          <h1 className="mt-6 text-4xl font-bold tracking-tight sm:text-5xl">
            Receive form submissions <span className="text-brand-600">straight to your inbox</span>
          </h1>
          <p className="mx-auto mt-5 max-w-2xl text-lg text-slate-500">
            Add a contact form to any website — static or dynamic — without writing a single line of backend code.
            Paste one snippet and you're done.
          </p>
          <div className="mt-8 flex items-center justify-center gap-3">
            <Link to="/login" className="inline-flex items-center gap-2 rounded-lg bg-brand-600 px-6 py-3 text-sm font-semibold text-white shadow-sm hover:bg-brand-700">
              Start for free <ArrowRight size={16} />
            </Link>
            <a href="#how" className="rounded-lg border border-slate-200 px-6 py-3 text-sm font-semibold text-slate-700 hover:bg-slate-50">
              How it works
            </a>
          </div>
          <p className="mt-3 text-xs text-slate-400">Free plan includes 250 submissions/month. No credit card required.</p>

          {/* Code snippet visual */}
          <div className="mx-auto mt-12 max-w-xl overflow-hidden rounded-xl border border-slate-800 bg-slate-900 text-left shadow-2xl">
            <div className="border-b border-slate-700/60 px-4 py-2 font-mono text-xs text-slate-400">index.html</div>
            <pre className="overflow-x-auto p-4 text-xs leading-relaxed text-slate-100">
{`<form action="https://api.formsubmissionpro.com/submit" method="POST">
  <input type="hidden" name="access_key" value="YOUR_KEY" />
  <input type="text" name="name" />
  <input type="email" name="email" />
  <textarea name="message"></textarea>
  <button type="submit">Send</button>
</form>`}
            </pre>
          </div>
        </div>
      </section>

      {/* How it works */}
      <section id="how" className="border-t border-slate-100 bg-slate-50/60 py-20">
        <div className="mx-auto max-w-5xl px-6">
          <h2 className="text-center text-2xl font-bold tracking-tight">How it works</h2>
          <div className="mt-12 grid gap-8 sm:grid-cols-3">
            {steps.map((s) => (
              <div key={s.n} className="text-center">
                <span className="mx-auto flex h-12 w-12 items-center justify-center rounded-full bg-brand-600 text-lg font-bold text-white">{s.n}</span>
                <h3 className="mt-4 font-semibold">{s.title}</h3>
                <p className="mt-1 text-sm text-slate-500">{s.desc}</p>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* Features */}
      <section className="py-20">
        <div className="mx-auto max-w-6xl px-6">
          <h2 className="text-center text-2xl font-bold tracking-tight">Everything you need</h2>
          <p className="mx-auto mt-2 max-w-xl text-center text-slate-500">A complete form backend — so you can focus on building your site.</p>
          <div className="mt-12 grid gap-6 sm:grid-cols-2 lg:grid-cols-3">
            {features.map((f) => (
              <div key={f.title} className="rounded-2xl border border-slate-200 p-6">
                <span className="flex h-10 w-10 items-center justify-center rounded-lg bg-brand-50 text-brand-600">
                  <f.icon size={20} />
                </span>
                <h3 className="mt-4 font-semibold">{f.title}</h3>
                <p className="mt-1 text-sm text-slate-500">{f.desc}</p>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* CTA */}
      <section className="border-t border-slate-100 py-20">
        <div className="mx-auto max-w-3xl px-6 text-center">
          <h2 className="text-3xl font-bold tracking-tight">Ready to collect submissions?</h2>
          <p className="mt-3 text-slate-500">Set up your first form in under two minutes.</p>
          <div className="mt-6 flex items-center justify-center gap-4">
            <Link to="/login" className="inline-flex items-center gap-2 rounded-lg bg-brand-600 px-6 py-3 text-sm font-semibold text-white hover:bg-brand-700">
              Create your free account <ArrowRight size={16} />
            </Link>
          </div>
          <div className="mt-6 flex flex-wrap items-center justify-center gap-4 text-sm text-slate-500">
            {["No backend code", "Spam protection", "Free to start"].map((t) => (
              <span key={t} className="inline-flex items-center gap-1.5"><Check size={15} className="text-green-600" /> {t}</span>
            ))}
          </div>
        </div>
      </section>

      <footer className="border-t border-slate-100 py-8">
        <div className="mx-auto max-w-6xl px-6 text-center text-sm text-slate-400">
          © {new Date().getFullYear()} XOFormPro. Built for developers.
        </div>
      </footer>
    </div>
  );
}
