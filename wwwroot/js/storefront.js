// Các tương tác dùng chung: banner, chi tiết sản phẩm, đánh giá và yêu thích.
(() => {
  'use strict';
  const byId = id => document.getElementById(id);
  const money = value => new Intl.NumberFormat('vi-VN').format(Number(value) || 0) + ' ₫';
  const esc = value => String(value ?? '').replace(/[&<>"']/g, c => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
  const reducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
  const requestJson = async (url, options = {}) => {
    const response = await fetch(url, { credentials: 'same-origin', ...options });
    if (response.status === 404) throw new Error('Mẫu hoa này không còn được bán.');
    if (response.redirected || response.status === 401 || response.status === 403)
      throw new Error('Phiên đăng nhập đã thay đổi. Vui lòng tải lại trang và đăng nhập.');
    if (!response.headers.get('content-type')?.includes('application/json'))
      throw new Error('Chưa tải được dữ liệu. Vui lòng thử lại hoặc tải lại trang.');
    const data = await response.json();
    if (!response.ok) throw new Error(data.message || (response.status === 404 ? 'Mẫu hoa này không còn được bán.' : 'Chưa thể tải dữ liệu. Vui lòng thử lại.'));
    return data;
  };

  // Yêu thích được lưu trên trình duyệt, độc lập với trạng thái giỏ hàng.
  const favoriteKey = 'laNuFavorites';
  let favorites = [];
  try {
    const saved = JSON.parse(localStorage.getItem(favoriteKey) || '[]');
    if (Array.isArray(saved)) favorites = [...new Set(saved.map(Number).filter(id => Number.isInteger(id) && id > 0))].slice(0, 100);
  } catch { /* Trình duyệt không cho truy cập storage: vẫn dùng được trong phiên này. */ }
  let product = null;
  const updateFavorites = () => {
    byId('favorites-count').textContent = String(favorites.length);
    document.querySelectorAll('.product-favorite[data-favorite]').forEach(button => {
      const active = favorites.includes(Number(button.dataset.favorite));
      button.setAttribute('aria-pressed', String(active)); button.textContent = active ? '♥' : '♡';
    });
    if (product) {
      const active = favorites.includes(product.productId);
      byId('detail-favorite').setAttribute('aria-pressed', String(active));
      byId('detail-favorite').textContent = active ? '♥ Đã yêu thích' : '♡ Yêu thích';
    }
  };
  const toggleFavorite = id => {
    id = Number(id);
    if (!Number.isInteger(id) || id < 1) return;
    if (favorites.includes(id)) favorites = favorites.filter(x => x !== id);
    else if (favorites.length < 100) favorites.push(id);
    else { window.alert('Bạn đã lưu 100 mẫu hoa. Hãy bỏ bớt một mẫu để thêm mẫu mới.'); return; }
    try { localStorage.setItem(favoriteKey, JSON.stringify(favorites)); } catch { /* Giữ danh sách trong phiên hiện tại. */ }
    updateFavorites();
  };
  const detailDialog = byId('product-dialog');
  const favoritesDialog = byId('favorites-dialog');
  let favoriteRequest = 0;
  async function showFavorites() {
    if (!favoritesDialog.open) favoritesDialog.showModal();
    const version = ++favoriteRequest;
    const list = byId('favorites-list');
    if (!favorites.length) { list.innerHTML = '<p class="review-empty">Chưa có mẫu yêu thích. Nhấn trái tim trên thẻ hoa để lưu lại nhé.</p>'; return; }
    list.textContent = 'Đang tải danh sách…';
    try {
      const items = await requestJson('/Products/Favorites?ids=' + encodeURIComponent(favorites.join(',')));
      if (version !== favoriteRequest || !favoritesDialog.open) return;
      list.innerHTML = items.length ? items.map(p => `<div class="favorite-line">
        <img src="${esc(p.imageUrl)}" alt="${esc(p.name)}" loading="lazy" />
        <div><button type="button" data-product-open="${p.productId}">${esc(p.name)}</button><p>${money(p.price)} · ${p.stockQuantity > 0 ? 'Còn hàng' : 'Tạm hết hàng'}</p></div>
        <button type="button" class="remove-favorite" data-favorite="${p.productId}" aria-label="Bỏ yêu thích ${esc(p.name)}">×</button></div>`).join('')
        : '<p class="review-empty">Những mẫu đã lưu hiện không còn được bán. Bạn có thể chọn mẫu hoa khác trong bộ sưu tập.</p>';
      if (items.length < favorites.length) list.insertAdjacentHTML('beforeend', '<button type="button" class="text-link" id="clear-unavailable-favorites">Dọn mẫu đã ngừng bán</button>');
      byId('clear-unavailable-favorites')?.addEventListener('click', () => {
        favorites = items.map(p => p.productId);
        try { localStorage.setItem(favoriteKey, JSON.stringify(favorites)); } catch { /* Phiên hiện tại */ }
        updateFavorites(); showFavorites();
      });
    } catch (error) { if (version === favoriteRequest) list.textContent = error.message; }
  }

  let productId = 0, starFilter = 0, reviewPage = 1, requestVersion = 0, detailAbort;
  let opener = null;
  const drawRating = data => {
    const rating = data.rating;
    byId('detail-rating-link').textContent = rating.count
      ? `★ ${Number(rating.average).toFixed(1)} / 5 · ${rating.count} đánh giá · Xem chi tiết`
      : 'Chưa có đánh giá · Xem đánh giá';
    byId('review-summary').textContent = rating.count
      ? `${Number(rating.average).toFixed(1)} / 5 · ${rating.count} đánh giá` : 'Chưa có đánh giá';
    const choices = [{ stars: 0, count: rating.count }, ...rating.counts];
    byId('review-filters').innerHTML = choices.map(c => `<button type="button" data-review-stars="${c.stars}" aria-pressed="${starFilter === c.stars}">${c.stars ? c.stars + ' sao' : 'Tất cả'} (${c.count})</button>`).join('');
  };
  const drawReviews = (data, append) => {
    const entries = data.reviews.map(r => {
      const date = new Date(r.createdAt.endsWith('Z') ? r.createdAt : r.createdAt + 'Z');
      const formatted = Number.isNaN(date.getTime()) ? '' : date.toLocaleDateString('vi-VN');
      const comment = r.comment.length > 220
        ? `<p>${esc(r.comment.slice(0,220))}…</p><details><summary>Xem chi tiết đánh giá</summary><p>${esc(r.comment)}</p></details>`
        : `<p>${esc(r.comment)}</p>`;
      return `<article class="review-entry"><div class="review-entry-head"><strong>${esc(r.displayName)}</strong><time>${esc(formatted)}</time></div>
        <span class="review-stars" aria-label="${r.stars} trên 5 sao">${'★'.repeat(r.stars)}${'☆'.repeat(5-r.stars)}</span>${comment}</article>`;
    }).join('');
    if (append) byId('review-list').insertAdjacentHTML('beforeend', entries);
    else byId('review-list').innerHTML = entries || `<p class="review-empty">${starFilter ? `Chưa có đánh giá ${starFilter} sao cho mẫu hoa này.` : 'Hãy là người đầu tiên chia sẻ cảm nhận về mẫu hoa này.'}</p>`;
    byId('review-more').hidden = !data.hasMore;
  };
  const drawProduct = data => {
    product = data.product;
    byId('detail-title').textContent = product.name;
    byId('detail-category').textContent = product.category || 'HOA TƯƠI LÁ & NỤ';
    byId('detail-description').textContent = product.description || 'Mẫu hoa tươi được chuẩn bị theo đơn tại Lá & Nụ.';
    byId('detail-image').src = product.imageUrl; byId('detail-image').alt = product.name;
    byId('detail-price').textContent = money(product.price);
    byId('detail-stock').textContent = product.stockQuantity > 0 ? `Còn ${product.stockQuantity} sản phẩm` : 'Tạm hết hàng';
    byId('detail-quantity').max = String(Math.min(100, Math.max(1, product.stockQuantity)));
    byId('detail-quantity').value = '1';
    byId('detail-quantity').disabled = product.stockQuantity <= 0;
    byId('detail-add').disabled = byId('detail-buy').disabled = product.stockQuantity <= 0;
    byId('detail-feedback').textContent = '';
    byId('review-form').hidden = !data.canReview; byId('review-login').hidden = data.canReview;
    const form = byId('review-form');
    form.elements.namedItem('DisplayName').value = data.myReview?.displayName || '';
    form.elements.namedItem('Comment').value = data.myReview?.comment || '';
    form.elements.namedItem('Stars').value = String(data.myReview?.stars || 5);
    byId('review-feedback').textContent = '';
    byId('detail-content').hidden = false; byId('detail-loading').hidden = true;
    updateFavorites();
  };
  async function loadDetails({ initial = false, append = false } = {}) {
    detailAbort?.abort(); detailAbort = new AbortController();
    const version = ++requestVersion;
    byId('review-more').disabled = true;
    try {
      const data = await requestJson(`/Products/Details/${productId}?page=${reviewPage}${starFilter ? '&stars=' + starFilter : ''}`, { signal: detailAbort.signal });
      if (version !== requestVersion || !detailDialog.open) return false;
      if (initial) drawProduct(data);
      drawRating(data); drawReviews(data, append);
      return true;
    } catch (error) {
      if (error.name === 'AbortError' || version !== requestVersion) return false;
      if (initial) { byId('detail-loading').textContent = error.message; }
      else { byId('review-feedback').textContent = error.message; }
      return false;
    } finally { if (version === requestVersion) byId('review-more').disabled = false; }
  }
  async function openProduct(id) {
    id = Number(id); if (!Number.isInteger(id) || id < 1) return;
    opener = document.activeElement;
    if (favoritesDialog.open) favoritesDialog.close();
    productId = id; product = null; starFilter = 0; reviewPage = 1;
    byId('detail-content').hidden = true; byId('detail-loading').hidden = false;
    byId('detail-loading').textContent = 'Đang tải mẫu hoa…';
    if (!detailDialog.open) detailDialog.showModal();
    detailDialog.scrollTop = 0;
    if (await loadDetails({ initial: true })) byId('detail-title').focus({ preventScroll: true });
  }
  detailDialog.addEventListener('close', () => {
    detailAbort?.abort(); requestVersion++;
    if (opener?.isConnected) opener.focus({ preventScroll: true });
  });
  [detailDialog, favoritesDialog].forEach(dialog => {
    dialog.addEventListener('click', event => {
      if (event.target !== dialog) return;
      const box = dialog.getBoundingClientRect();
      if (event.clientX < box.left || event.clientX > box.right || event.clientY < box.top || event.clientY > box.bottom) dialog.close();
    });
  });
  document.addEventListener('click', event => {
    const target = event.target.closest('button, a');
    if (target?.matches('[data-close-dialog]')) { target.closest('dialog').close(); return; }
    if (target?.matches('[data-favorite]')) {
      toggleFavorite(target.dataset.favorite);
      if (favoritesDialog.open) showFavorites();
      return;
    }
    if (target?.matches('[data-product-open]')) { openProduct(target.dataset.productOpen); return; }
    if (target?.matches('[data-review-stars]')) {
      starFilter = Number(target.dataset.reviewStars); reviewPage = 1; loadDetails(); return;
    }
    const card = event.target.closest('[data-product-card]');
    if (card && !event.target.closest('button, a, input, select')) openProduct(card.dataset.productCard);
  });
  byId('open-favorites').addEventListener('click', showFavorites);
  byId('detail-favorite').addEventListener('click', () => { if (product) toggleFavorite(product.productId); });
  byId('detail-rating-link').addEventListener('click', () => byId('detail-reviews').scrollIntoView({ behavior: reducedMotion ? 'auto' : 'smooth', block: 'start' }));
  byId('review-more').addEventListener('click', async () => {
    const id = productId, requestedPage = ++reviewPage;
    if (!await loadDetails({ append: true }) && id === productId && reviewPage === requestedPage) reviewPage--;
  });
  const addProduct = buy => {
    if (!product) return;
    const quantity = Number(byId('detail-quantity').value);
    if (!Number.isInteger(quantity) || quantity < 1 || quantity > Math.min(100, product.stockQuantity)) {
      byId('detail-feedback').textContent = 'Vui lòng chọn số lượng hợp lệ theo tồn kho.'; return;
    }
    if (!window.LaNuCart?.add) { byId('detail-feedback').textContent = 'Chưa tải được giỏ hàng. Vui lòng tải lại trang.'; return; }
    const inCart = window.LaNuCart.quantityFor(product.productId);
    if (inCart + quantity > Math.min(100, product.stockQuantity)) {
      byId('detail-feedback').textContent = `Giỏ đã có ${inCart} sản phẩm. Bạn có thể thêm tối đa ${Math.max(0, Math.min(100, product.stockQuantity) - inCart)} sản phẩm nữa.`;
      return;
    }
    window.LaNuCart.add(product.productId, product.name, product.price, product.imageUrl, quantity);
    byId('detail-feedback').textContent = `Đã thêm ${quantity} sản phẩm vào giỏ.`;
    if (buy) window.location.href = '/Orders/Checkout';
  };
  byId('detail-add').addEventListener('click', () => addProduct(false));
  byId('detail-buy').addEventListener('click', () => addProduct(true));
  byId('review-form').addEventListener('submit', async event => {
    event.preventDefault();
    const form = event.currentTarget;
    if (!form.reportValidity()) return;
    const id = productId, submit = form.querySelector('[type=submit]');
    submit.disabled = true; byId('review-feedback').textContent = 'Đang lưu đánh giá…';
    try {
      const result = await requestJson(`/Products/Review/${id}`, { method: 'POST', body: new FormData(form), headers: { 'Accept': 'application/json' } });
      if (id !== productId || !detailDialog.open) return;
      starFilter = 0; reviewPage = 1; await loadDetails();
      if (id === productId && detailDialog.open) byId('review-feedback').textContent = result.message;
    } catch (error) { if (id === productId) byId('review-feedback').textContent = error.message; }
    finally { submit.disabled = false; }
  });
  updateFavorites();
  window.addEventListener('storage', event => {
    if (event.key !== favoriteKey) return;
    try {
      const values = JSON.parse(event.newValue || '[]');
      favorites = Array.isArray(values) ? [...new Set(values.map(Number).filter(id => Number.isInteger(id) && id > 0))].slice(0,100) : [];
      updateFavorites(); if (favoritesDialog.open) showFavorites();
    } catch { /* Bỏ qua dữ liệu storage không hợp lệ */ }
  });

  // Banner chỉ lấy hoa đang bán, còn hàng và có nhãn Bán chạy do quản trị cập nhật.
  const banner = byId('bestseller-banner');
  if (banner) {
    const track = byId('banner-track'), dots = byId('banner-dots');
    let slides = [...track.children], index = 0, direction = 1, timer, paused = reducedMotion;
    let hovering = false, focused = false, touchStart;
    const pauseButton = banner.querySelector('[data-banner-pause]');
    const stop = () => { if (timer) clearInterval(timer); timer = null; };
    const restart = () => {
      stop();
      if (!paused && !hovering && !focused && !document.hidden && slides.length > 1)
        timer = setInterval(() => {
          if (detailDialog.open || favoritesDialog.open) return;
          if (index >= slides.length - 1) direction = -1;
          else if (index <= 0) direction = 1;
          show(index + direction);
        }, 4500);
    };
    const show = (next, manual = false) => {
      if (!slides.length) return;
      index = (next + slides.length) % slides.length;
      track.style.transform = `translateX(-${index * 100}%)`;
      slides.forEach((slide, i) => { slide.inert = i !== index; slide.setAttribute('aria-hidden', String(i !== index)); });
      [...dots.children].forEach((dot, i) => dot.setAttribute('aria-current', String(i === index)));
      if (manual) { byId('banner-status').textContent = `Mẫu hoa ${index + 1} trên ${slides.length}`; restart(); }
    };
    const rebuild = () => {
      slides = [...track.children];
      byId('banner-empty').hidden = slides.length > 0;
      banner.querySelector('.banner-controls').hidden = slides.length < 2;
      dots.innerHTML = slides.map((_, i) => `<button type="button" data-banner-index="${i}" aria-label="Xem mẫu hoa ${i+1}"></button>`).join('');
      index = Math.min(index, Math.max(0, slides.length-1));
      show(index); restart();
    };
    pauseButton.textContent = paused ? 'Tự động chạy' : 'Tạm dừng';
    pauseButton.setAttribute('aria-pressed', String(paused));
    pauseButton.addEventListener('click', () => {
      paused = !paused; pauseButton.textContent = paused ? 'Tự động chạy' : 'Tạm dừng';
      pauseButton.setAttribute('aria-pressed', String(paused)); restart();
    });
    banner.querySelector('[data-banner-prev]').addEventListener('click', () => show(index-1, true));
    banner.querySelector('[data-banner-next]').addEventListener('click', () => show(index+1, true));
    dots.addEventListener('click', event => { const dot = event.target.closest('[data-banner-index]'); if (dot) show(Number(dot.dataset.bannerIndex), true); });
    banner.addEventListener('pointerenter', () => { hovering = true; stop(); });
    banner.addEventListener('pointerleave', () => { hovering = false; restart(); });
    banner.addEventListener('focusin', () => { focused = true; stop(); });
    banner.addEventListener('focusout', event => { if (!banner.contains(event.relatedTarget)) { focused = false; restart(); } });
    banner.addEventListener('keydown', event => {
      if (event.key === 'ArrowLeft') { event.preventDefault(); show(index-1, true); }
      if (event.key === 'ArrowRight') { event.preventDefault(); show(index+1, true); }
    });
    track.addEventListener('touchstart', event => { touchStart = event.touches[0].clientX; stop(); }, { passive:true });
    track.addEventListener('touchend', event => {
      if (touchStart !== undefined) {
        const delta = event.changedTouches[0].clientX - touchStart;
        if (Math.abs(delta) > 45) show(index + (delta < 0 ? 1 : -1), true);
      }
      touchStart = undefined; restart();
    }, { passive:true });
    document.addEventListener('visibilitychange', restart);
    const refresh = async () => {
      if (document.hidden || banner.contains(document.activeElement) || hovering) return;
      try {
        const items = await requestJson('/Products/BestSellers');
        const signature = JSON.stringify(items);
        if (signature === banner.dataset.signature) return;
        banner.dataset.signature = signature;
        const selectedId = slides[index]?.dataset.bannerProduct;
        track.innerHTML = items.map((p,i) => `<article class="banner-slide" data-banner-product="${p.productId}" aria-label="${i+1} / ${items.length}">
          <div class="banner-copy"><p class="section-label">BỘ SƯU TẬP · HOA BÁN CHẠY</p><h1>${esc(p.name)}</h1><p class="banner-description">${esc(p.description)}</p>
          <p class="banner-price">${money(p.price)}</p><button type="button" class="btn btn-petal" data-product-open="${p.productId}">Khám phá mẫu hoa →</button></div>
          <div class="banner-photo"><img src="${esc(p.imageUrl)}" alt="${esc(p.name)}" /></div></article>`).join('');
        const selectedIndex = items.findIndex(p => String(p.productId) === selectedId);
        index = selectedIndex >= 0 ? selectedIndex : 0; rebuild();
      } catch { /* Giữ banner từ server nếu việc cập nhật tạm thời thất bại. */ }
    };
    rebuild(); setInterval(refresh, 60000);
  }
  const header = document.querySelector('.site-header');
  if (header && window.ResizeObserver) new ResizeObserver(() => document.documentElement.style.setProperty('--header-height', header.offsetHeight + 'px')).observe(header);
})();
