// Site-wide UI helpers: translations, theme, toasts, clipboard, confirmations, dialogs, tabs and local time formatting.
(function () {
    "use strict";

    const TOAST_DURATION_MS = 3000;
    const THEME_KEY = "theme";
    const PERSIAN = "fa";
    // Persian pages use the Solar Hijri calendar and Persian digits (the defaults of the fa-IR locale).
    const PERSIAN_LOCALE = "fa-IR";

    const app = (window.app = window.app || {});

    // Translations rendered by the layout (key = English text); missing keys fall back to the English text.
    const translations = (() => {
        try {
            return JSON.parse(document.getElementById("i18n")?.textContent || "{}");
        } catch {
            return {};
        }
    })();

    /** Translates a text and fills in {0}-style placeholders. */
    app.t = function (key, ...args) {
        const text = translations[key] ?? key;
        return text.replace(/\{(\d+)\}/g, (match, index) => (index < args.length ? String(args[index]) : match));
    };

    // undefined keeps the browser's own locale on English pages.
    app.locale = document.documentElement.lang === PERSIAN ? PERSIAN_LOCALE : undefined;

    /** Formats a number for the page language (Persian digits on Persian pages). */
    app.formatNumber = function (value) {
        return app.locale ? Number(value).toLocaleString(app.locale) : String(value);
    };

    app.toast = function (message, type) {
        const host = document.querySelector("[data-toast-host]");
        if (!host) return;
        const toast = document.createElement("div");
        toast.className = "toast" + (type === "error" ? " error" : "");
        toast.textContent = message;
        host.appendChild(toast);
        setTimeout(() => toast.remove(), TOAST_DURATION_MS);
    };

    app.copy = async function (text) {
        try {
            await navigator.clipboard.writeText(text);
        } catch {
            // Fallback for non-secure contexts (plain HTTP).
            const area = document.createElement("textarea");
            area.value = text;
            document.body.appendChild(area);
            area.select();
            document.execCommand("copy");
            area.remove();
        }
        app.toast(app.t("Copied to clipboard"));
    };

    app.formatBytes = function (bytes) {
        const units = ["B", "KB", "MB", "GB", "TB"];
        let value = bytes;
        let index = 0;
        while (value >= 1024 && index < units.length - 1) {
            value /= 1024;
            index++;
        }
        const rounded = index === 0 ? value : value.toFixed(2).replace(/\.?0+$/, "");
        return app.formatNumber(rounded) + " " + units[index];
    };

    app.formatLocalTime = function (root) {
        (root || document).querySelectorAll("time[data-utc]").forEach((element) => {
            const date = new Date(element.dataset.utc);
            if (isNaN(date)) return;
            element.textContent = element.hasAttribute("data-date-only")
                ? date.toLocaleDateString(app.locale)
                : date.toLocaleString(app.locale);
            element.title = date.toISOString();
        });
    };

    // Theme
    function applyTheme(theme) {
        try {
            if (theme === "system") {
                localStorage.removeItem(THEME_KEY);
                document.documentElement.removeAttribute("data-theme");
            } else {
                localStorage.setItem(THEME_KEY, theme);
                document.documentElement.setAttribute("data-theme", theme);
            }
        } catch {
            document.documentElement.setAttribute("data-theme", theme);
        }
        updateThemePicker();
    }

    function currentTheme() {
        const explicit = document.documentElement.getAttribute("data-theme");
        if (explicit) return explicit;
        return window.matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light";
    }

    function updateThemePicker() {
        let stored = null;
        try { stored = localStorage.getItem(THEME_KEY); } catch { /* storage unavailable */ }
        document.querySelectorAll("[data-theme-value]").forEach((button) => {
            button.classList.toggle("active", button.dataset.themeValue === (stored || "system"));
        });
    }

    document.addEventListener("click", (event) => {
        const target = event.target.closest("[data-theme-toggle], [data-theme-value], [data-copy], [data-copy-from], [data-dialog-open], [data-dialog-close], [data-nav-toggle], [data-tab]");
        if (!target) return;

        if (target.matches("[data-theme-toggle]")) {
            applyTheme(currentTheme() === "dark" ? "light" : "dark");
        } else if (target.matches("[data-theme-value]")) {
            applyTheme(target.dataset.themeValue);
        } else if (target.matches("[data-copy]")) {
            app.copy(target.dataset.copy);
        } else if (target.matches("[data-copy-from]")) {
            const source = document.querySelector(target.dataset.copyFrom);
            if (source) app.copy(source.value);
        } else if (target.matches("[data-dialog-open]")) {
            document.getElementById(target.dataset.dialogOpen)?.showModal();
        } else if (target.matches("[data-dialog-close]")) {
            target.closest("dialog")?.close();
        } else if (target.matches("[data-nav-toggle]")) {
            document.querySelector("[data-nav]")?.classList.toggle("open");
        } else if (target.matches("[data-tab]")) {
            const container = target.closest("[data-tabs]");
            container.querySelectorAll("[data-tab]").forEach((tab) => tab.classList.toggle("active", tab === target));
            container.querySelectorAll("[data-tab-panel]").forEach((panel) => {
                panel.hidden = panel.dataset.tabPanel !== target.dataset.tab;
            });
        }
    });

    // Confirmation for destructive forms
    document.addEventListener("submit", (event) => {
        const form = event.target;
        if (form.matches("[data-confirm]") && !window.confirm(form.dataset.confirm)) {
            event.preventDefault();
        }
    });

    // "Never expires" disables the value/unit fields; data-reveal shows a section while a checkbox is checked.
    function syncToggles() {
        document.querySelectorAll("[data-never-toggle]").forEach((checkbox) => {
            const container = checkbox.closest("[data-expiration]");
            container?.querySelectorAll("[data-expiration-field]").forEach((field) => (field.disabled = checkbox.checked));
        });
        document.querySelectorAll("[data-reveal]").forEach((checkbox) => {
            const target = document.querySelector(checkbox.dataset.reveal);
            if (target) target.hidden = !checkbox.checked;
        });
    }

    document.addEventListener("change", (event) => {
        if (event.target.matches("[data-never-toggle], [data-reveal]")) syncToggles();
    });

    // Disabled inputs are not posted; enable them right before submitting so model binding keeps the values.
    document.addEventListener("submit", (event) => {
        if (event.defaultPrevented) return;
        event.target.querySelectorAll("[data-expiration-field]:disabled").forEach((field) => (field.disabled = false));
    });

    document.addEventListener("DOMContentLoaded", () => {
        app.formatLocalTime();
        syncToggles();
        updateThemePicker();
        document.querySelectorAll("dialog[data-open-on-load='true']").forEach((dialog) => dialog.showModal());

        // Open the settings tab that contains the first validation error.
        const tabs = document.querySelector("[data-tabs]");
        const invalid = tabs?.querySelector(".input-validation-error");
        const panel = invalid?.closest("[data-tab-panel]");
        if (panel) tabs.querySelector(`[data-tab="${panel.dataset.tabPanel}"]`)?.click();
    });
})();
