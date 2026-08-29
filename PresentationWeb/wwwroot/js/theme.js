(function () {
    const root = document.documentElement;
    const systemPrefersDark = window.matchMedia("(prefers-color-scheme: dark)");

    function resolveTheme(mode) {
        if (mode === "system") return systemPrefersDark.matches ? "dark" : "light";
        return mode;
    }

    function applyTheme(mode) {
        root.setAttribute("data-bs-theme", resolveTheme(mode));
        updateIcons(mode);
    }

    function updateIcons(mode) {
        document.querySelectorAll("[data-theme-icon]").forEach(function (icon) {
            if (mode === "light") icon.className = "bi bi-sun";
            else if (mode === "dark") icon.className = "bi bi-moon-stars";
            else icon.className = "bi bi-circle-half";
        });
        document.querySelectorAll("[data-theme-option]").forEach(function (item) {
            item.classList.toggle("active", item.getAttribute("data-theme-option") === mode);
        });
    }

    window.setTheme = function (mode) {
        localStorage.setItem("theme", mode);
        applyTheme(mode);
    };

    const saved = localStorage.getItem("theme") || "system";

    systemPrefersDark.addEventListener("change", function () {
        if ((localStorage.getItem("theme") || "system") === "system") applyTheme("system");
    });

    applyTheme(saved);

    document.addEventListener("DOMContentLoaded", function () {
        updateIcons(saved);
    });
})();