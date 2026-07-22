/* ============================================================
   JK Apparel — Site Interactions
   ============================================================ */

document.addEventListener('DOMContentLoaded', function () {
    initHeaderScroll();
    initSizeColorPickers();
    initThemeColorPreview();
    initMobileSearch();
    initScrollAnimations();
    initQuantityInputs();
    initToastFromTempData();
    refreshCartBadge();
    refreshWishlistCount();
    initSearchAutocomplete();
    initBackToTop();
    initNewsletterForm();
});

/* ---------- Header scroll effect ---------- */
function initHeaderScroll() {
    var header = document.querySelector('.site-header');
    if (!header) return;
    var lastScroll = 0;
    window.addEventListener('scroll', function () {
        var scroll = window.pageYOffset;
        header.classList.toggle('scrolled', scroll > 20);
        lastScroll = scroll;
    }, { passive: true });
}

/* ---------- Size / Color pill selection ---------- */
function initSizeColorPickers() {
    document.querySelectorAll('.size-option').forEach(function (el) {
        el.addEventListener('click', function () {
            document.querySelectorAll('.size-option').forEach(function (s) { s.classList.remove('active'); });
            el.classList.add('active');
            var input = document.getElementById('selectedSize');
            if (input) input.value = el.dataset.value;
        });
    });

    document.querySelectorAll('.color-option').forEach(function (el) {
        el.addEventListener('click', function () {
            document.querySelectorAll('.color-option').forEach(function (s) { s.classList.remove('active'); });
            el.classList.add('active');
            var input = document.getElementById('selectedColor');
            if (input) input.value = el.dataset.value;
        });
    });
}

/* ---------- Admin theme color live preview ---------- */
function initThemeColorPreview() {
    document.querySelectorAll('.theme-color-input').forEach(function (input) {
        input.addEventListener('input', function () {
            var preview = document.getElementById(input.dataset.previewTarget);
            if (preview) preview.style.background = input.value;
        });
    });
}

/* ---------- Mobile search toggle ---------- */
function initMobileSearch() {
    var toggle = document.getElementById('mobileSearchToggle');
    var form = document.getElementById('mobileSearchForm');
    if (toggle && form) {
        toggle.addEventListener('click', function (e) {
            e.preventDefault();
            form.classList.toggle('d-none');
            var input = form.querySelector('input');
            if (input && !form.classList.contains('d-none')) input.focus();
        });
    }
}

/* ---------- Scroll fade-in animations ---------- */
function initScrollAnimations() {
    var elements = document.querySelectorAll('.fade-in');
    if (!elements.length) return;

    var observer = new IntersectionObserver(function (entries) {
        entries.forEach(function (entry) {
            if (entry.isIntersecting) {
                entry.target.classList.add('visible');
                observer.unobserve(entry.target);
            }
        });
    }, { threshold: 0.1, rootMargin: '0px 0px -40px 0px' });

    elements.forEach(function (el) { observer.observe(el); });
}

/* ---------- Quantity +/- inputs ---------- */
function initQuantityInputs() {
    document.querySelectorAll('.qty-input').forEach(function (wrap) {
        var input = wrap.querySelector('input');
        var minus = wrap.querySelector('.qty-minus');
        var plus = wrap.querySelector('.qty-plus');
        if (!input || !minus || !plus) return;

        if (!wrap.dataset.ajaxInit) {
            minus.addEventListener('click', function () {
                var val = parseInt(input.value) || 1;
                if (val > 1) { input.value = val - 1; input.dispatchEvent(new Event('change')); }
            });

            plus.addEventListener('click', function () {
                var val = parseInt(input.value) || 1;
                var max = parseInt(input.getAttribute('max')) || 999;
                if (val < max) { input.value = val + 1; input.dispatchEvent(new Event('change')); }
            });
            wrap.dataset.ajaxInit = 'true';
        }
    });
}

/* ---------- Toast from TempData ---------- */
function initToastFromTempData() {
    var successMsg = document.querySelector('[data-toast-success]');
    var errorMsg = document.querySelector('[data-toast-error]');

    if (successMsg) showToast(successMsg.dataset.toastSuccess, 'success');
    if (errorMsg) showToast(errorMsg.dataset.toastError, 'error');
}

function showToast(message, type) {
    if (!message) return;
    var container = document.getElementById('toast-container');
    if (!container) {
        container = document.createElement('div');
        container.id = 'toast-container';
        container.className = 'toast-container';
        document.body.appendChild(container);
    }

    var toast = document.createElement('div');
    toast.className = 'toast-notification ' + (type || '');

    var icon = document.createElement('i');
    icon.className = 'bi bi-' + (type === 'success' ? 'check-circle-fill text-success' : type === 'error' ? 'exclamation-circle-fill text-danger' : 'info-circle-fill text-primary');
    toast.appendChild(icon);

    var span = document.createElement('span');
    span.textContent = message;
    toast.appendChild(span);

    var closeBtn = document.createElement('button');
    closeBtn.className = 'toast-close';
    closeBtn.textContent = '\u00d7';
    closeBtn.addEventListener('click', function () { toast.remove(); });
    toast.appendChild(closeBtn);

    container.appendChild(toast);
    setTimeout(function () { toast.style.opacity = '0'; toast.style.transform = 'translateX(100%)'; toast.style.transition = '0.3s ease'; }, 3500);
    setTimeout(function () { toast.remove(); }, 4000);
}

/* ---------- Cart badge ---------- */
function refreshCartBadge() {
    fetch('/Cart/Count')
        .then(function (r) { return r.ok ? r.json() : { count: 0 }; })
        .then(function (data) {
            var badge = document.getElementById('cart-count-badge');
            if (badge) {
                var count = data.count || 0;
                badge.textContent = count;
                badge.style.display = count > 0 ? 'inline-flex' : 'none';
            }
        })
        .catch(function () { });
}

/* ---------- Wishlist badge ---------- */
function refreshWishlistCount() {
    fetch('/Wishlist/Count')
        .then(function (r) { return r.ok ? r.json() : { count: 0 }; })
        .then(function (data) {
            var badge = document.getElementById('wishlist-count-badge');
            if (badge) {
                var count = data.count || 0;
                badge.textContent = count;
                badge.style.display = count > 0 ? 'inline-flex' : 'none';
            }
        })
        .catch(function () { });
}

/* ---------- Search autocomplete ---------- */
function initSearchAutocomplete() {
    var input = document.getElementById('desktopSearchInput');
    var dropdown = document.getElementById('searchSuggestions');
    if (!input || !dropdown) return;

    var debounceTimer;

    input.addEventListener('input', function () {
        clearTimeout(debounceTimer);
        var q = input.value.trim();
        if (q.length < 2) { dropdown.style.display = 'none'; return; }

        debounceTimer = setTimeout(function () {
            fetch('/Shop/SearchSuggestions?q=' + encodeURIComponent(q))
                .then(function (r) { return r.json(); })
                .then(function (data) {
                    if (!data || data.length === 0) { dropdown.style.display = 'none'; return; }

                    dropdown.innerHTML = '';
                    data.forEach(function (item) {
                        var a = document.createElement('a');
                        a.href = '/shop/product/' + item.slug;
                        a.className = 'search-suggestion-item';

                        var img = document.createElement('img');
                        img.src = item.mainImageUrl || '/images/hero.svg';
                        img.alt = '';
                        img.className = 'search-suggestion-img';
                        a.appendChild(img);

                        var div = document.createElement('div');
                        div.className = 'flex-grow-1';

                        var nameDiv = document.createElement('div');
                        nameDiv.className = 'small fw-medium';
                        nameDiv.textContent = item.name;
                        div.appendChild(nameDiv);

                        var priceDiv = document.createElement('div');
                        priceDiv.className = 'small';
                        priceDiv.style.color = 'var(--color-accent)';
                        priceDiv.textContent = 'AED ' + item.price.toFixed(2);
                        div.appendChild(priceDiv);

                        a.appendChild(div);
                        dropdown.appendChild(a);
                    });
                    dropdown.style.display = 'block';
                })
                .catch(function () { dropdown.style.display = 'none'; });
        }, 300);
    });

    input.addEventListener('blur', function () {
        setTimeout(function () { dropdown.style.display = 'none'; }, 200);
    });

    input.addEventListener('focus', function () {
        if (dropdown.children.length > 0 && input.value.trim().length >= 2) {
            dropdown.style.display = 'block';
        }
    });
}

/* ---------- Back to top button ---------- */
function initBackToTop() {
    var btn = document.createElement('button');
    btn.id = 'backToTop';
    btn.innerHTML = '<i class="bi bi-arrow-up"></i>';
    btn.className = 'back-to-top';
    btn.setAttribute('aria-label', 'Back to top');
    document.body.appendChild(btn);

    window.addEventListener('scroll', function () {
        btn.classList.toggle('visible', window.pageYOffset > 400);
    }, { passive: true });

    btn.addEventListener('click', function () {
        window.scrollTo({ top: 0, behavior: 'smooth' });
    });
}

/* ---------- Newsletter form ---------- */
function initNewsletterForm() {
    var form = document.getElementById('newsletterForm');
    if (!form) return;

    form.addEventListener('submit', function (e) {
        e.preventDefault();
        var emailInput = form.querySelector('input[name="email"]');
        var msg = document.getElementById('newsletterMsg');
        if (!emailInput) return;

        var antiForgeryToken = form.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
        var formData = new FormData(form);

        fetch('/Newsletter/Subscribe', {
            method: 'POST',
            headers: { 'X-Requested-With': 'XMLHttpRequest', 'RequestVerificationToken': antiForgeryToken },
            body: formData
        })
        .then(function (r) { return r.json(); })
        .then(function (data) {
            if (msg) {
                msg.textContent = data.message;
                msg.style.display = 'block';
                msg.style.color = data.success ? 'var(--color-accent)' : '#ef4444';
            }
            if (data.success) emailInput.value = '';
        })
        .catch(function () {
            form.submit();
        });
    });
}

/* ---------- Utility: smooth scroll to element ---------- */
function scrollToElement(selector) {
    var el = document.querySelector(selector);
    if (el) el.scrollIntoView({ behavior: 'smooth', block: 'start' });
}
