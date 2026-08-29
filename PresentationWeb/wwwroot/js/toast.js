// Lightweight toast notifications (replaces default alert/confirm-ish popups).
// Usage: showToast('Fabric created', 'success');  showToast('Something went wrong', 'error');
(function () {
    const ICONS = {
        success: 'bi-check-circle-fill',
        error: 'bi-x-octagon-fill',
        info: 'bi-info-circle-fill',
        warning: 'bi-exclamation-triangle-fill'
    };

    function ensureContainer() {
        let c = document.querySelector('.toast-container-app');
        if (!c) {
            c = document.createElement('div');
            c.className = 'toast-container-app';
            document.body.appendChild(c);
        }
        return c;
    }

    window.showToast = function (message, type, duration) {
        type = type || 'info';
        duration = duration || 3000;

        const container = ensureContainer();
        const el = document.createElement('div');
        el.className = 'toast-item ' + type;
        el.innerHTML = '<span class="toast-icon bi ' + (ICONS[type] || ICONS.info) + '"></span><span>' + (message || '') + '</span>';

        // close button
        const close = document.createElement('button');
        close.type = 'button';
        close.className = 'btn-close btn-close-white ms-auto';
        close.style.fontSize = '0.7rem';
        close.addEventListener('click', function () { dismiss(el); });
        el.appendChild(close);

        container.appendChild(el);

        const timer = setTimeout(function () { dismiss(el); }, duration);
        function dismiss(target) {
            clearTimeout(timer);
            target.classList.add('toast-hide');
            setTimeout(function () {
                if (target.parentNode) target.parentNode.removeChild(target);
            }, 300);
        }

        return el;
    };

    // Convenience: show an error from a response body's message field.
    window.showResponseError = function (data, fallback) {
        showToast((data && data.message) || fallback || 'Something went wrong.', 'error');
    };
})();
