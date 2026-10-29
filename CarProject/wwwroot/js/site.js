// Password eye toggle
document.addEventListener('DOMContentLoaded', function () {
    document.querySelectorAll('.pwd-toggle').forEach(function (btn) {
        btn.addEventListener('mousedown', function (e) {
            e.preventDefault();
            var input = this.closest('.pwd-wrapper').querySelector('.pwd-input');
            input.type = 'text';
            this.querySelector('i').className = 'fas fa-eye-slash';
        });
        btn.addEventListener('mouseup', function () {
            var input = this.closest('.pwd-wrapper').querySelector('.pwd-input');
            input.type = 'password';
            this.querySelector('i').className = 'fas fa-eye';
        });
        btn.addEventListener('mouseleave', function () {
            var input = this.closest('.pwd-wrapper').querySelector('.pwd-input');
            input.type = 'password';
            this.querySelector('i').className = 'fas fa-eye';
        });
    });
});

// Nav menu: centre active link on load, allow wheel-scroll hint
(function() {
    var scroll = document.getElementById('navMenuScroll');
    if (!scroll) return;

    // Bring the active (current page) link into view when the menu overflows
    var activeLink = scroll.querySelector('.nav-link.active');
    if (activeLink) {
        window.addEventListener('load', function() {
            if (scroll.scrollWidth > scroll.clientWidth) {
                var target = activeLink.offsetLeft - (scroll.clientWidth - activeLink.offsetWidth) / 2;
                scroll.scrollLeft = Math.max(0, Math.min(target, scroll.scrollWidth - scroll.clientWidth));
            }
        });
    }

    // Vertical wheel over the menu scrolls it horizontally (only when it overflows)
    scroll.addEventListener('wheel', function(e) {
        if (scroll.scrollWidth <= scroll.clientWidth) return;   // nothing to scroll
        // Shift+wheel scrolls vertically in most browsers; translate to horizontal.
        var delta = e.shiftKey ? (e.deltaY || e.deltaX) : (e.deltaY + e.deltaX);
        if (!delta) return;
        var next = scroll.scrollLeft + delta;
        if (next === scroll.scrollLeft) return;
        scroll.scrollLeft = Math.max(0, Math.min(next, scroll.scrollWidth - scroll.clientWidth));
        e.preventDefault();
    }, { passive: false });
})();

// "Giới thiệu" dropdown as a body-level portal (avoids clipping by the horizontal scroll container)
(function() {
    var trigger = document.getElementById('navAboutItem');
    var toggle = document.getElementById('navAboutToggle');
    if (!trigger) return;
    var menu = trigger.querySelector('.nav-dropdown-menu');
    if (!menu) return;

    // Move the menu to <body> so the scroll container's overflow can't clip it
    document.body.appendChild(menu);
    menu.classList.add('nav-dropdown-fixed');

    var open = false;

    function position() {
        var r = toggle.getBoundingClientRect();
        var mw = menu.offsetWidth;
        var left = r.left + r.width / 2 - mw / 2;
        left = Math.max(12, Math.min(left, window.innerWidth - mw - 12));
        menu.style.top = (r.bottom + 6) + 'px';
        menu.style.left = left + 'px';
    }

    function show() {
        position();
        open = true;
        menu.classList.add('open');
    }
    function hide() {
        open = false;
        menu.classList.remove('open');
    }

    function mouseInsideMenu(e) {
        return menu.contains(e.relatedTarget);
    }

    trigger.addEventListener('mousemove', function() { if (!open) show(); });
    trigger.addEventListener('mouseleave', function(e) { if (!mouseInsideMenu(e)) hide(); });
    menu.addEventListener('mouseenter', show);
    menu.addEventListener('mouseleave', hide);

    toggle.addEventListener('click', function(e) {
        e.preventDefault();
        e.stopPropagation();
        open ? hide() : show();
    });

    // Keep the caret rotation in sync with the open state
    trigger.addEventListener('mouseenter', function() { trigger.classList.add('hover-open'); });
    trigger.addEventListener('mouseleave', function(e) { if (!mouseInsideMenu(e)) trigger.classList.remove('hover-open'); });
    menu.addEventListener('mouseenter', function() { trigger.classList.add('hover-open'); });
    menu.addEventListener('mouseleave', function() { trigger.classList.remove('hover-open'); });
    toggle.addEventListener('mouseenter', function() { trigger.classList.add('hover-open'); });
    toggle.addEventListener('mouseleave', function(e) { if (!mouseInsideMenu(e)) trigger.classList.remove('hover-open'); });

    document.addEventListener('click', function(e) {
        if (!trigger.contains(e.target) && !menu.contains(e.target) && open) hide();
    });
    document.addEventListener('keydown', function(e) {
        if (e.key === 'Escape' && open) hide();
    });
    window.addEventListener('resize', function() { if (open) position(); });
    window.addEventListener('blur', hide);
})();

// Mobile hamburger toggle
(function() {
    var toggle = document.getElementById('mobileMenuToggle');
    var panel = document.getElementById('mobileNavPanel');
    var backdrop = document.getElementById('mobileNavBackdrop');
    if (!toggle || !panel) return;

    function closePanel() {
        panel.classList.remove('open');
        if (backdrop) backdrop.classList.remove('open');
        toggle.querySelectorAll('.toggler-bar').forEach(function(bar) {
            bar.style.transform = '';
            bar.style.opacity = '';
        });
    }

    function openPanel() {
        panel.classList.add('open');
        if (backdrop) backdrop.classList.add('open');
        void panel.offsetHeight;
        var bars = toggle.querySelectorAll('.toggler-bar');
        bars[0].style.transform = 'rotate(45deg) translate(4px, 4px)';
        bars[1].style.opacity = '0';
        bars[2].style.transform = 'rotate(-45deg) translate(4px, -4px)';
    }
    toggle.addEventListener('click', function() {
        if (panel.classList.contains('open')) {
            closePanel();
        } else {
            openPanel();
        }
    });

    if (backdrop) {
        backdrop.addEventListener('click', closePanel);
    }

    // Close panel when clicking any link inside (except dropdown toggles)
    panel.querySelectorAll('.nav-link, .mobile-auth-link, .nav-dropdown-item').forEach(function(link) {
        if (link.closest('.nav-item-dropdown') && link.classList.contains('dropdown-toggle')) return;
        link.addEventListener('click', function() {
            closePanel();
        });
    });

    document.addEventListener('keydown', function(e) {
        if (e.key === 'Escape') closePanel();
    });
})();

// Theme toggle (dark/light) with curtain animation
(function() {
    var html = document.documentElement;
    var toggle = document.getElementById('themeToggle');
    var icon = document.getElementById('themeIcon');
    var curtain = document.getElementById('themeCurtain');
    if (!toggle || !icon || !curtain) return;

    function setTheme(theme) {
        html.setAttribute('data-theme', theme);
        localStorage.setItem('site-theme', theme);
        icon.className = theme === 'light' ? 'fas fa-sun' : 'fas fa-moon';
    }

    // Load saved theme
    var saved = localStorage.getItem('site-theme');
    if (saved) setTheme(saved);

    toggle.addEventListener('click', function() {
        var current = html.getAttribute('data-theme');
        var next = current === 'light' ? 'dark' : 'light';

        // Curtain close
        curtain.classList.remove('active', 'closing');
        void curtain.offsetHeight;
        curtain.classList.add('closing');

        setTimeout(function() {
            setTheme(next);
            // Curtain open
            curtain.classList.remove('closing');
            curtain.classList.add('active');
        }, 350);

        setTimeout(function() {
            curtain.classList.remove('active');
        }, 950);
    });
})();

// Scroll reveal animation (plays on every scroll-down, no reverse when scrolling up)
(function() {
    var observer = new IntersectionObserver(function(entries) {
        entries.forEach(function(entry) {
            if (entry.isIntersecting) {
                entry.target.classList.add('reveal-visible');
            } else {
                entry.target.style.transition = 'none';
                entry.target.classList.remove('reveal-visible');
                void entry.target.offsetHeight;
                entry.target.style.transition = '';
            }
        });
    }, { threshold: 0.15 });

    function observeReveals() {
        document.querySelectorAll('.reveal').forEach(function(el) {
            observer.observe(el);
        });
    }

    document.addEventListener('DOMContentLoaded', observeReveals);
    if (window.MutationObserver) {
        var mo = new MutationObserver(observeReveals);
        mo.observe(document.body, { childList: true, subtree: true });
    }
})();
