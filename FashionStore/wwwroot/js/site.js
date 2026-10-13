(function () {
    const authed = document.body.dataset.auth === 'true';
    const token = () => document.querySelector('#afForm input[name="__RequestVerificationToken"]')?.value;
    const buttons = () => [...document.querySelectorAll('.wish-btn')];

    function mark(btn, liked) {
        btn.classList.toggle('active', liked);
        btn.querySelector('i').className = liked ? 'bi bi-heart-fill' : 'bi bi-heart';
    }

    // Tô đỏ các sản phẩm đã thích (chỉ 1 request cho cả trang)
    if (authed && buttons().length) {
        fetch('/Wishlist/Ids').then(r => r.json()).then(ids => {
            const set = new Set(ids);
            buttons().forEach(b => mark(b, set.has(+b.dataset.id)));
        });
    }

    document.addEventListener('click', async e => {
        const b = e.target.closest('.wish-btn');
        if (!b) return;
        e.preventDefault();
        if (!authed) {
            location.href = '/Account/Login?returnUrl=' + encodeURIComponent(location.pathname + location.search);
            return;
        }
        const res = await fetch('/Wishlist/Toggle', {
            method: 'POST',
            headers: { 'Content-Type': 'application/x-www-form-urlencoded', 'RequestVerificationToken': token() },
            body: 'productId=' + b.dataset.id
        });
        if (!res.ok) return;
        const d = await res.json();
        mark(b, d.liked);
        const item = b.closest('[data-wish-item]');       // ở trang Yêu thích: bỏ thích thì xóa thẻ
        if (!d.liked && item) item.remove();
    });
})();