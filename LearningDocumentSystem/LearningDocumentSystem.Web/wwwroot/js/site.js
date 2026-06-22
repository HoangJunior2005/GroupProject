// ================================================================
// LDS System — site.js
// Sidebar toggle, toast auto-dismiss, misc helpers
// ================================================================

window.refreshPageFragments = async function (selectors, afterRefresh) {
    const response = await fetch(window.location.href, {
        headers: { 'X-Requested-With': 'XMLHttpRequest' },
        cache: 'no-store'
    });

    if (!response.ok) {
        throw new Error(`Refresh failed with status ${response.status}`);
    }

    const html = await response.text();
    const doc = new DOMParser().parseFromString(html, 'text/html');

    selectors.forEach(selector => {
        const current = document.querySelector(selector);
        const incoming = doc.querySelector(selector);
        if (current && incoming) {
            current.replaceWith(incoming);
        }
    });

    if (typeof afterRefresh === 'function') {
        afterRefresh();
    }
};

document.addEventListener('DOMContentLoaded', function () {

    // ============================================================
    // Sidebar Toggle (mobile)
    // ============================================================
    const toggleBtn = document.getElementById('sidebarToggle');
    const sidebar   = document.getElementById('sidebar');
    if (toggleBtn && sidebar) {
        toggleBtn.addEventListener('click', function () {
            sidebar.classList.toggle('open');
        });

        // Close sidebar when clicking outside on mobile
        document.addEventListener('click', function (e) {
            if (window.innerWidth <= 992
                && !sidebar.contains(e.target)
                && !toggleBtn.contains(e.target)) {
                sidebar.classList.remove('open');
            }
        });
    }
    // ============================================================
    // Auto dismiss toasts after 4 seconds
    // ============================================================
    document.querySelectorAll('.toast').forEach(toastEl => {
        setTimeout(() => {
            const bsToast = bootstrap.Toast.getInstance(toastEl);
            if (bsToast) bsToast.hide();
            else toastEl.classList.remove('show');
        }, 4000);
    });

    // ============================================================
    // Form loading states
    // ============================================================
    document.querySelectorAll('form[data-loading]').forEach(form => {
        form.addEventListener('submit', function () {
            const btn = form.querySelector('[type="submit"]');
            if (btn) {
                btn.disabled = true;
                btn.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span>Đang xử lý...';
            }
        });
    });

    // ============================================================
    // Confirm before delete (fallback for non-modal deletes)
    // ============================================================
    document.querySelectorAll('[data-confirm]').forEach(el => {
        el.addEventListener('click', function (e) {
            if (!confirm(this.dataset.confirm)) {
                e.preventDefault();
            }
        });
    });

    // ============================================================
    // Animate stat numbers on dashboard load
    // ============================================================
    document.querySelectorAll('.stat-number').forEach(el => {
        const target = parseInt(el.textContent, 10);
        if (isNaN(target) || target === 0) return;

        let current = 0;
        const duration = 800;
        const step = Math.ceil(target / (duration / 16));

        const timer = setInterval(() => {
            current = Math.min(current + step, target);
            el.textContent = current.toLocaleString('vi-VN');
            if (current >= target) clearInterval(timer);
        }, 16);
    });

    // ============================================================
    // Progress bars animation
    // ============================================================
    document.querySelectorAll('.progress-bar').forEach(bar => {
        const width = bar.style.width;
        bar.style.width = '0';
        setTimeout(() => {
            bar.style.transition = 'width 0.8s ease-out';
            bar.style.width = width;
        }, 200);
    });

});
