const KEY = "fsp.theme";

export type Theme = "light" | "dark";

export function getTheme(): Theme {
  return (localStorage.getItem(KEY) as Theme) ?? "light";
}

export function applyTheme(theme: Theme) {
  document.documentElement.classList.toggle("dark", theme === "dark");
  localStorage.setItem(KEY, theme);
}

export function toggleTheme(): Theme {
  const next: Theme = getTheme() === "dark" ? "light" : "dark";
  applyTheme(next);
  return next;
}

/** Call once on startup to apply the saved theme before render. */
export function initTheme() {
  applyTheme(getTheme());
}
