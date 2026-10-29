(function () {
    'use strict';
    if (window.innerWidth < 992) return;

    var panel = null;
    var hideTimer = null;
    var showTimer = null;
    var overPanel = false;
    var overCard = false;
    var activeCard = null;
    var activeImg = null;
    var pointerActive = false;
    var mouseX = -1, mouseY = -1;

    function inHoverZone() {
        var anchor = activeImg || activeCard;
        if (!anchor || !panel || panel.style.display === 'none') return false;
        var cr = anchor.getBoundingClientRect();
        var pr = panel.getBoundingClientRect();
        var pad = 6;
        function inside(r) {
            return mouseX >= r.left - pad && mouseX <= r.right + pad
                && mouseY >= r.top - pad && mouseY <= r.bottom + pad;
        }
        if (inside(cr) || inside(pr)) return true;
        var gL, gR;
        if (pr.right <= cr.left) { gL = pr.left; gR = cr.left; }              // panel bên trái card
        else if (pr.left >= cr.right) { gL = cr.right; gR = pr.left; }      // panel bên phải card
        else return false;
        if (gR - gL > 60) return false; // gap quá xa -> không phải cầu nối
        var gT = Math.max(cr.top, pr.top) - pad;
        var gB = Math.min(cr.bottom, pr.bottom) + pad;
        if (gB - gT <= 0) return false;
        return mouseX >= gL - pad && mouseX <= gR + pad && mouseY >= gT && mouseY <= gB;
    }

    function ensurePanel() {
        if (!panel) {
            panel = document.createElement('div');
            panel.className = 'car-hover-preview';
            panel.style.display = 'none';
            document.body.appendChild(panel);
            panel.addEventListener('mouseenter', function () {
                overPanel = true;
                clearTimeout(hideTimer);
            });
            panel.addEventListener('mouseleave', function () {
                overPanel = false;
                scheduleHide();
            });
            panel.addEventListener('wheel', function (e) {
                var scroller = e.target.closest ? e.target.closest('.chp-showrooms, .chp-versions') : null;
                var d = e.deltaY;
                if (scroller) {
                    var dir = d > 0 ? 1 : -1;
                    var canScroll = dir < 0
                        ? scroller.scrollTop > 0
                        : scroller.scrollTop + scroller.clientHeight < scroller.scrollHeight - 1;
                    if (canScroll) return; // vùng còn cuộn được -> lăn mặc định trong panel
                    e.preventDefault(); // chạm biên -> khoá, không kéo trang theo
                    return;
                }
                e.preventDefault();
                var scrollable = document.scrollingElement || document.documentElement;
                window.scrollTo(0, scrollable.scrollTop + d);
            });
        }
        return panel;
    }

    function escapeHtml(s) {
        return String(s)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;');
    }

    function formatPrice(n) {
        var v = Number(n);
        if (!isFinite(v)) return 'Liên hệ';
        return v.toLocaleString('vi-VN') + 'đ';
    }

    function parseVersions(card) {
        try {
            var raw = card.getAttribute('data-car-versions');
            var arr = raw ? JSON.parse(raw) : [];
            return Array.isArray(arr) ? arr : [];
        } catch (e) { return []; }
    }

    function parseShowrooms(card) {
        try {
            var raw = card.getAttribute('data-car-showrooms');
            var arr = raw ? JSON.parse(raw) : [];
            return Array.isArray(arr) ? arr : [];
        } catch (e) { return []; }
    }

    function buildContent(card) {
        var name = card.getAttribute('data-car-name') || '';
        var brand = card.getAttribute('data-car-brand') || '';
        var style = card.getAttribute('data-car-style') || '';
        var country = card.getAttribute('data-car-country') || '';
        var price = card.getAttribute('data-car-price') || 'Liên hệ';
        var stock = card.getAttribute('data-car-stock') || 'in';
        var stockText = card.getAttribute('data-car-stock-text') || 'Còn hàng';
        var count = card.getAttribute('data-car-count') || '0';
        var detail = card.getAttribute('data-car-detail') || '#';
        var versions = parseVersions(card);
        var showrooms = parseShowrooms(card);
        var stockColor = stock === 'out' ? '#dc3545' : (stock === 'low' ? '#ffc107' : '#28a745');

        var verHtml = versions.length
            ? '<div class="chp-versions">' + versions.map(function (v) {
                var c = Number(v.SoLuongTrongKho) <= 0 ? '#dc3545' : (Number(v.SoLuongTrongKho) <= 5 ? '#ffc107' : '#28a745');
                return '<div class="chp-ver-row">'
                    + '<span class="chp-ver-name">' + escapeHtml(v.TenPhienBan || '') + '</span>'
                    + '<span class="chp-ver-price">' + formatPrice(v.GiaNiemYet) + '</span>'
                    + '<span class="chp-ver-stock" style="color:' + c + ';">' + escapeHtml(v.SoLuongTrongKho) + '</span>'
                    + '</div>';
            }).join('') + '</div>'
            : '';

        var srHtml = showrooms.length
            ? '<div class="chp-showrooms">' + showrooms.map(function (s) {
                var sc = Number(s.SoLuong) <= 0 ? '#dc3545' : (Number(s.SoLuong) <= 5 ? '#ffc107' : '#28a745');
                var versions = Array.isArray(s.PhienBans) ? s.PhienBans : [];
                var subHtml = versions.length > 0
                    ? '<div class="chp-sr-vers">' + versions.map(function (v) {
                        var vc = Number(v.SoLuong) <= 0 ? '#dc3545' : (Number(v.SoLuong) <= 2 ? '#ffc107' : '#28a745');
                        return '<div class="chp-sr-ver">'
                            + '<span class="chp-sr-ver-name">' + escapeHtml(v.TenPhienBan || '') + '</span>'
                            + '<span class="chp-sr-ver-stock" style="color:' + vc + ';">' + escapeHtml(v.SoLuong) + ' xe</span>'
                            + '</div>';
                    }).join('') + '</div>'
                    : '';
                return '<div class="chp-sr-row">'
                    + '<span class="chp-sr-name"><i class="fas fa-map-marker-alt"></i> ' + escapeHtml(s.Ten || s.Key || '') + '</span>'
                    + '<span class="chp-sr-stock" style="color:' + sc + ';">' + escapeHtml(s.SoLuong) + ' xe</span>'
                    + '</div>'
                    + subHtml;
            }).join('') + '</div>'
            : '';

        return '<div class="chp-header">'
            + '<span class="chp-brand"><i class="fas fa-tag"></i> ' + escapeHtml(brand) + '</span>'
            + '<span class="chp-stock" style="color:' + stockColor + ';border-color:' + stockColor + ';">' + escapeHtml(stockText) + '</span>'
            + '</div>'
            + '<div class="chp-name">' + escapeHtml(name) + '</div>'
            + '<div class="chp-meta">'
            + '<span><i class="fas fa-car-side"></i> ' + escapeHtml(style) + '</span>'
            + '<span><i class="fas fa-globe-asia"></i> ' + escapeHtml(country) + '</span>'
            + '<span><i class="fas fa-cubes"></i> ' + escapeHtml(count) + ' phiên bản</span>'
            + '</div>'
            + '<div class="chp-price">' + escapeHtml(price) + '</div>'
            + srHtml
            + verHtml
            + '<a class="chp-link" href="' + detail + '">Xem chi tiết <i class="fas fa-chevron-right"></i></a>';
    }

    function positionPanel(anchor) {
        var rect = anchor.getBoundingClientRect();
        var pw = 310, gap = 14, margin = 8;
        var vw = window.innerWidth, vh = window.innerHeight;
        var ph = panel.offsetHeight || 320;

        var left, side;
        if (rect.left + rect.width / 2 < vw / 2) {
            side = 'right';
            left = rect.right + gap;
        } else {
            side = 'left';
            left = rect.left - gap - pw;
        }

        if (side === 'right' && left + pw > vw - margin) {
            side = 'left';
            left = rect.left - gap - pw;
        }
        if (side === 'left' && left < margin) {
            side = 'right';
            left = rect.right + gap;
        }
        if (left + pw > vw - margin) left = vw - pw - margin;
        if (left < margin) left = margin;

        var top = rect.top + rect.height / 2 - ph / 2;
        if (top < margin) top = margin;
        if (top + ph > vh - margin) top = vh - ph - margin;
        if (top < margin) top = margin;

        panel.classList.toggle('chp-left', side === 'left');
        panel.classList.toggle('chp-right', side === 'right');
        panel.style.left = left + 'px';
        panel.style.top = top + 'px';
    }

    function show(img, card) {
        clearTimeout(hideTimer);
        clearTimeout(showTimer);
        activeCard = card;
        activeImg = img;
        overCard = true;
        var p = ensurePanel();
        p.innerHTML = buildContent(card);
        p.style.display = 'block';
        positionPanel(img);
    }

    function scheduleShow(img, card) {
        clearTimeout(showTimer);
        overCard = true;
        activeCard = card;
        activeImg = img;
        showTimer = setTimeout(function () {
            if (pointerActive) show(img, card);
        }, 220);
    }

    function cancelShow() {
        clearTimeout(showTimer);
        overCard = false;
        hideNow();
    }

    function hideNow() {
        clearTimeout(hideTimer);
        if (panel && !overPanel && !inHoverZone()) panel.style.display = 'none';
    }

    function scheduleHide() {
        clearTimeout(hideTimer);
        hideTimer = setTimeout(function () {
            if (overCard || overPanel || inHoverZone()) return;
            if (panel) panel.style.display = 'none';
        }, 80);
    }

    function init() {
        document.addEventListener('mousemove', function (e) {
            pointerActive = true;
            mouseX = e.clientX;
            mouseY = e.clientY;
            if (panel && panel.style.display !== 'none' && !overCard && !overPanel) {
                if (inHoverZone()) {
                    clearTimeout(hideTimer);
                } else {
                    clearTimeout(hideTimer);
                    hideTimer = setTimeout(function () {
                        if (overCard || overPanel || inHoverZone()) return;
                        if (panel) panel.style.display = 'none';
                    }, 80);
                }
            }
        });
        var images = document.querySelectorAll('.car-card-premium[data-car-name] .card-image');
        if (!images.length) return;
        [].forEach.call(images, function (img) {
            var card = img.closest ? img.closest('.car-card-premium[data-car-name]') : null;
            if (!card) return;
            img.addEventListener('mouseenter', function () { scheduleShow(img, card); });
            img.addEventListener('mouseleave', cancelShow);
        });
    }

    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', init);
    else init();
})();
