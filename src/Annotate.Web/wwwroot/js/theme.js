// Applies the saved theme before first paint (no flash) and persists changes.
// External file because the content security policy forbids inline scripts.
(function () {
    "use strict";
    const key = "annotate-theme";
    const root = document.documentElement;
    root.classList.toggle("dark", localStorage.getItem(key) === "dark");
    window.annotateTheme = {
        isDark: () => root.classList.contains("dark"),
        setDark: (dark) => {
            root.classList.toggle("dark", dark);
            localStorage.setItem(key, dark ? "dark" : "light");
        },
    };
})();
