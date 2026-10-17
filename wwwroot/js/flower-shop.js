// flower-shop.js - Quản lý giỏ hàng và tương tác giao diện Lá & Nụ
const currency = value => new Intl.NumberFormat('vi-VN').format(value) + ' ₫';
const normalize = value => String(value || '').toLocaleLowerCase('vi')
  .normalize('NFD').replace(/[\u0300-\u036f]/g, '').replace(/đ/g, 'd');
const escapeHtml = value => String(value ?? '').replace(/[&<>"']/g, char =>
  ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' })[char]);

const cards = [...document.querySelectorAll('#product-grid .product-card')];
const products = new Map(cards.map(card => {
  const button = card.querySelector('[data-add]');
  if (!button) return ['', null];
  return [String(button.dataset.add), {
    id: String(button.dataset.add),
    name: button.dataset.name,
    price: Number(button.dataset.price),
    image: button.dataset.image
  }];
}).filter(([k, v]) => k && v));

let filter = 'all';
let searchText = '';
let cart = readCart();
let opener = null;
let toastTimer;

function readCart() {
  try {
    const value = JSON.parse(localStorage.getItem('laNuCart') || '{}');
    if (!value || typeof value !== 'object' || Array.isArray(value)) return {};
    return Object.fromEntries(Object.entries(value).map(([id, saved]) => {
      const product = products.get(String(id));
      const quantity = Number(typeof saved === 'object' ? saved?.quantity : saved) || 0;
      const display = typeof saved === 'object' && saved ? saved : (product || { id: String(id), name: 'Hoa tươi', price: 0, image: '/assets/tulips.jpg' });
      return [String(id), { ...product, ...display, quantity }];
    }));
  } catch { return {}; }
}

function saveCart() {
  try { localStorage.setItem('laNuCart', JSON.stringify(cart)); }
  catch { notify('Không thể lưu giỏ trong trình duyệt này.'); }
  renderCart();
}

function applyFilters() {
  if (!cards.length) return;
  let visible = 0;
  cards.forEach(card => {
    const nameAndDescription = normalize(card.dataset.name || card.textContent);
    const matchesCategory = filter === 'all' || card.dataset.category === filter;
    const matchesSearch = nameAndDescription.includes(normalize(searchText));
    card.hidden = !(matchesCategory && matchesSearch);
    if (!card.hidden) visible++;
  });
  let empty = document.querySelector('#product-grid .no-results');
  if (!empty) {
    empty = document.createElement('p');
    empty.className = 'no-results';
    empty.setAttribute('role', 'status');
    empty.textContent = 'Chưa tìm thấy mẫu hoa phù hợp. Thử từ khóa khác nhé.';
    document.querySelector('#product-grid')?.append(empty);
  }
  empty.hidden = visible !== 0;
}

function addToCart(id, name, price, image, quantity = 1) {
  let product = products.get(String(id));
  if (name) {
    product = { id: String(id), name, price: Number(price) || 0, image: image || '/assets/tulips.jpg' };
    products.set(String(id), product);
  }
  if (!product) return;
  const current = cart[id] || product;
  cart[id] = { ...current, ...product, quantity: Math.min(100, (Number(current.quantity) || 0) + Math.max(1, Math.min(100, Number(quantity) || 1))) };
  saveCart();
  notify(`Đã thêm "${escapeHtml(product.name)}" vào giỏ hàng 🌸`);
}

function renderCart() {
  const lines = Object.entries(cart)
    .map(([id, saved]) => ({
      product: products.get(String(id)) || saved,
      quantity: Number(typeof saved === 'object' ? saved?.quantity : saved)
    }))
    .filter(line => line.product?.name && line.quantity > 0);

  const count = lines.reduce((sum, line) => sum + line.quantity, 0);
  const countElement = document.querySelector('#cart-count');
  if (countElement) countElement.textContent = count;

  const empty = document.querySelector('#cart-empty');
  if (empty) empty.hidden = lines.length > 0;

  const items = document.querySelector('#cart-items');
  if (items) items.innerHTML = lines.map(({ product, quantity }) => `
    <div class="cart-line">
      <img src="${escapeHtml(product.image)}" alt="" />
      <div>
        <h3>${escapeHtml(product.name)}</h3>
        <p>${currency(product.price)}</p>
        <div class="qty-control" aria-label="Số lượng ${escapeHtml(product.name)}">
          <button type="button" data-qty="${product.id}" data-delta="-1" aria-label="Giảm ${escapeHtml(product.name)}">−</button>
          <span aria-live="polite">${quantity}</span>
          <button type="button" data-qty="${product.id}" data-delta="1" aria-label="Tăng ${escapeHtml(product.name)}">+</button>
        </div>
      </div>
      <strong>${currency(product.price * quantity)}</strong>
    </div>`).join('');

  const total = lines.reduce((sum, line) => sum + line.product.price * line.quantity, 0);
  const totalElement = document.querySelector('#cart-total');
  if (totalElement) totalElement.textContent = currency(total);
}

function setDrawer(open) {
  const drawer = document.querySelector('#cart-drawer');
  const backdrop = document.querySelector('#cart-backdrop');
  if (!drawer || !backdrop) return;
  drawer.classList.toggle('open', open);
  drawer.setAttribute('aria-hidden', String(!open));
  backdrop.hidden = !open;
  document.body.style.overflow = open ? 'hidden' : '';
  if (open) {
    opener = document.activeElement;
    drawer.querySelector('#close-cart')?.focus();
  } else if (opener instanceof HTMLElement) opener.focus();
}

function notify(message) {
  const element = document.querySelector('#toast-message');
  if (!element) return;
  element.textContent = message;
  element.classList.add('show');
  clearTimeout(toastTimer);
  toastTimer = setTimeout(() => element.classList.remove('show'), 2600);
}

document.addEventListener('DOMContentLoaded', () => {
  const year = document.querySelector('#current-year');
  if (year) year.textContent = new Date().getFullYear();

  cart = readCart();
  renderCart();
  applyFilters();

  // Lọc nhanh cho filter-chip kiểu button
  document.querySelectorAll('button.filter-chip').forEach(button => button.addEventListener('click', () => {
    document.querySelector('button.filter-chip.active')?.classList.remove('active');
    document.querySelectorAll('button.filter-chip').forEach(item => item.setAttribute('aria-pressed', 'false'));
    button.classList.add('active');
    button.setAttribute('aria-pressed', 'true');
    filter = button.dataset.filter || 'all';
    applyFilters();
  }));

  // Tìm kiếm tức thì trong trang
  document.querySelector('#product-search')?.addEventListener('input', event => {
    searchText = event.currentTarget.value.trim();
    applyFilters();
  });

  // Sự kiện thêm nhanh vào giỏ hàng từ bất kỳ đâu trên website
  document.addEventListener('click', event => {
    const button = event.target.closest('[data-add]');
    if (button) {
      event.preventDefault();
      addToCart(button.dataset.add, button.dataset.name, button.dataset.price, button.dataset.image);
    }
  });

  // Tăng giảm số lượng trong cart drawer
  document.querySelector('#cart-items')?.addEventListener('click', event => {
    const button = event.target.closest('[data-qty]');
    if (!button) return;
    const id = button.dataset.qty;
    const current = cart[id] || products.get(String(id));
    const currentQty = Number(typeof current === 'object' ? current?.quantity : current) || 0;
    const quantity = currentQty + Number(button.dataset.delta);
    if (quantity <= 0) delete cart[id];
    else cart[id] = { ...products.get(String(id)), ...current, quantity };
    saveCart();
  });

  document.querySelector('#open-cart')?.addEventListener('click', () => { cart = readCart(); renderCart(); setDrawer(true); });
  document.querySelector('#close-cart')?.addEventListener('click', () => setDrawer(false));
  document.querySelector('#cart-backdrop')?.addEventListener('click', () => setDrawer(false));

  document.querySelector('#checkout-button')?.addEventListener('click', () => {
    if (!Object.values(cart).some(item => Number(typeof item === 'object' ? item?.quantity : item) > 0)) {
      notify('Giỏ hàng của bạn đang trống.');
      return;
    }
    window.location.href = '/Orders/Checkout';
  });

  document.addEventListener('keydown', event => {
    const drawer = document.querySelector('#cart-drawer');
    if (!drawer?.classList.contains('open')) return;
    if (event.key === 'Escape') { setDrawer(false); return; }
  });
});

// API dùng chung cho bảng chi tiết sản phẩm. Checkout vẫn tính giá/tồn từ server.
window.LaNuCart = { add: addToCart, quantityFor: id => Number(cart[String(id)]?.quantity) || 0 };
