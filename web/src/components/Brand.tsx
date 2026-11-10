// Brand mark: the logo (the "XO") followed by "FormPro" => XOFormPro.
// import.meta.env.BASE_URL respects the deployment subpath (/FormSubmissionPro/).
const LOGO = `${import.meta.env.BASE_URL}logo.ico`;

export function Brand({
  size = 32,
  textClassName = "text-[15px] text-slate-900 dark:text-slate-100",
  showText = true,
}: { size?: number; textClassName?: string; showText?: boolean }) {
  return (
    <span className="inline-flex items-center gap-2">
      <img src={LOGO} alt="XO" width={size} height={size} className="rounded-lg" style={{ width: size, height: size }} />
      {showText && (
        <span className={`font-bold tracking-tight ${textClassName}`}>
          Form<span className="text-accent-600 dark:text-accent-500">Pro</span>
        </span>
      )}
    </span>
  );
}
