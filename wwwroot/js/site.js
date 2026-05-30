let cart = JSON.parse(localStorage.getItem('cart') || '[]');
let DELIVERY_FEE = 5.00;
let FREE_DELIVERY_MIN = 50.00;
const MAX_COMPLEMENTS = 3;

const productsEl = document.getElementById('products');
const cartItemsEl = document.getElementById('cart-items');
const cartCount = document.getElementById('cart-count');
const cartTotal = document.getElementById('cart-total');
const cartTotalAmount = document.getElementById('cart-total-amount');
const cartSubtotal = document.getElementById('cart-subtotal');
const cartDelivery = document.getElementById('cart-delivery');
const cartSidebar = document.getElementById('cart-sidebar');
const cartOverlay = document.getElementById('cart-overlay');
const gotoCheckout = document.getElementById('goto-checkout');
const cartFloating = document.getElementById('cart-floating');
const floatingCount = document.getElementById('floating-count');
const floatingTotal = document.getElementById('floating-total');

let currentCategory = 'all';
let allProducts = [];
let allComplements = [];

function getScrollbarWidth() {
  return window.innerWidth - document.documentElement.clientWidth;
}

function openCart() {
  const sw = getScrollbarWidth();
  document.body.style.paddingRight = sw + 'px';
  cartSidebar?.classList.add('open');
  cartOverlay?.classList.add('open');
  document.body.style.overflow = 'hidden';
}

function closeCart() {
  cartSidebar?.classList.remove('open');
  cartOverlay?.classList.remove('open');
  document.body.style.overflow = '';
  document.body.style.paddingRight = '';
}

function formatPrice(price) {
  return price.toLocaleString('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}

function getItemTotal(item) {
  const base = item.price * item.qty;
  const comps = item.complements ? item.complements.reduce((s, c) => s + c.price, 0) * item.qty : 0;
  return base + comps;
}

function getCartSubtotal() {
  return cart.reduce((sum, i) => sum + getItemTotal(i), 0);
}

function getBaseSubtotal() {
  return cart.reduce((sum, i) => sum + (i.price * i.qty), 0);
}

function getDeliveryFee() {
  return getBaseSubtotal() >= FREE_DELIVERY_MIN ? 0 : DELIVERY_FEE;
}

function renderCart() {
  if (!cartItemsEl) { updateCartBadge(); return; }

  const subtotal = getCartSubtotal();
  const delivery = getDeliveryFee();
  const total = subtotal + delivery;

  if (cart.length === 0) {
    cartItemsEl.innerHTML = `
      <div class="cart-empty">
        <div class="cart-empty-icon">🛒</div>
        <div>Seu carrinho está vazio</div>
        <div style="font-size:0.8rem;margin-top:4px">Adicione produtos do cardápio</div>
      </div>`;
  } else {
    cartItemsEl.innerHTML = cart.map((item, idx) => {
      const sizeText = item.size ? `<div class="cart-item-size">${item.size.name}</div>` : '';
      const compsText = item.complements && item.complements.length > 0
        ? `<div class="cart-item-comps">+ ${item.complements.map(c => c.name).join(', ')}</div>`
        : '';
      const itemTotal = getItemTotal(item);
      return `
      <div class="cart-item">
        <div class="cart-item-info">
          <div class="cart-item-name">${item.name}${sizeText}</div>
          ${compsText}
          <div class="cart-item-price">R$ ${formatPrice(item.price)}</div>
        </div>
        <div class="cart-item-qty">
          <button onclick="decrementCart(${idx})">−</button>
          <span>${item.qty}</span>
          <button onclick="incrementCart(${idx})">+</button>
        </div>
        <button class="cart-item-remove" onclick="removeFromCart(${idx})" title="Remover">✕</button>
      </div>`;
    }).join('');
  }

  const deliveryText = delivery === 0 ? 'Grátis' : `R$ ${formatPrice(delivery)}`;
  const deliveryClass = delivery === 0 ? 'free' : '';

  if (cartSubtotal) cartSubtotal.textContent = `R$ ${formatPrice(subtotal)}`;
  if (cartDelivery) {
    cartDelivery.innerHTML = `<span>Taxa de entrega</span><span class="${deliveryClass}">${deliveryText}</span>`;
  }
  if (cartTotal) cartTotal.textContent = `R$ ${formatPrice(total)}`;
  if (cartTotalAmount) cartTotalAmount.textContent = `R$ ${formatPrice(total)}`;

  if (gotoCheckout) {
    gotoCheckout.disabled = cart.length === 0;
  }

  updateCartBadge();
  updateCartFloating();
  saveCart();
}

function updateCartBadge() {
  const count = cart.reduce((s, i) => s + i.qty, 0);
  if (cartCount) cartCount.textContent = count;
  if (cartCount) cartCount.style.display = count > 0 ? 'inline-flex' : 'none';
}

function updateCartFloating() {
  const count = cart.reduce((s, i) => s + i.qty, 0);
  const subtotal = getCartSubtotal();
  const total = subtotal + getDeliveryFee();

  if (floatingCount) floatingCount.textContent = `${count} ${count === 1 ? 'item' : 'itens'}`;
  if (floatingTotal) floatingTotal.textContent = `R$ ${formatPrice(total)}`;
  if (cartFloating) cartFloating.style.display = count > 0 ? 'block' : 'none';
}

function incrementCart(idx) {
  cart[idx].qty++;
  renderCart();
}

function decrementCart(idx) {
  if (cart[idx].qty > 1) {
    cart[idx].qty--;
  } else {
    cart.splice(idx, 1);
  }
  renderCart();
}

function removeFromCart(idx) {
  cart.splice(idx, 1);
  renderCart();
}

function saveCart() {
  localStorage.setItem('cart', JSON.stringify(cart));
}

// Complement modal
let complementModalProduct = null;
let complementModalQty = 1;
let selectedComplements = [];
let selectedSize = null;

function parseSizes(sizesJson) {
  if (!sizesJson) return [];
  try { return JSON.parse(sizesJson); } catch { return []; }
}

function openSizeModal(product) {
  sizeModalProduct = product;
  selectedSize = null;
  document.getElementById('size-modal-title').textContent = product.name;
  renderSizeList();
  document.getElementById('size-modal-confirm').disabled = true;
  document.getElementById('size-overlay').classList.add('open');
  document.getElementById('size-modal').classList.add('open');
  document.body.style.overflow = 'hidden';
}

function closeSizeModal() {
  document.getElementById('size-overlay').classList.remove('open');
  document.getElementById('size-modal').classList.remove('open');
  document.body.style.overflow = '';
  sizeModalProduct = null;
  selectedSize = null;
}

function renderSizeList() {
  const list = document.getElementById('size-list');
  const sizes = parseSizes(sizeModalProduct.sizesJson);
  list.innerHTML = sizes.map((s, i) => `
    <button class="size-option ${selectedSize?.name === s.name ? 'selected' : ''}"
      onclick="selectSize(${i})">
      <span class="size-name">${s.name}</span>
      <span class="size-desc">${s.diameter ? s.diameter + 'cm' : ''}</span>
      <span class="size-price">R$ ${formatPrice(s.price)}</span>
    </button>
  `).join('');
}

function selectSize(index) {
  const sizes = parseSizes(sizeModalProduct.sizesJson);
  selectedSize = sizes[index];
  renderSizeList();
  document.getElementById('size-modal-confirm').disabled = false;
}

function confirmSize() {
  if (!sizeModalProduct || !selectedSize) return;
  if (sizeModalProduct.category === 'Bebida') {
    const existing = cart.find(i => i.id === sizeModalProduct.id && i.size?.name === selectedSize.name);
    if (existing) {
      existing.qty++;
    } else {
      cart.push({
        id: sizeModalProduct.id, name: sizeModalProduct.name,
        price: selectedSize.price, qty: 1, image: sizeModalProduct.image,
        size: { name: selectedSize.name, diameter: selectedSize.diameter, price: selectedSize.price },
        complements: []
      });
    }
    closeSizeModal();
    renderCart();
    showAddedFeedback(sizeModalProduct.id);
  } else {
    const sizedProduct = { ...sizeModalProduct, price: selectedSize.price };
    closeSizeModal();
    openComplementModal(sizedProduct, selectedSize);
  }
}

function showAddedFeedback(id) {
  const btn = document.querySelector(`[data-id="${id}"]`);
  if (btn) {
    btn.textContent = '✓ Adicionado';
    btn.style.background = '#27ae60';
    setTimeout(() => {
      btn.textContent = 'Adicionar';
      btn.style.background = '';
    }, 1200);
  }
}

function openComplementModal(product, size) {
  complementModalProduct = product;
  complementModalQty = 1;
  selectedComplements = [];
  selectedSize = size || null;

  const overlay = document.getElementById('complement-overlay');
  const modal = document.getElementById('complement-modal');
  const title = document.getElementById('complement-modal-title');
  const list = document.getElementById('complement-list');
  const qtyEl = document.getElementById('complement-modal-qty');

  title.textContent = product.name + (size ? ` (${size.name})` : '');
  qtyEl.textContent = '1';
  renderComplementList();
  updateComplementTotal();

  overlay.classList.add('open');
  modal.classList.add('open');
  document.body.style.overflow = 'hidden';
}

function closeComplementModal() {
  const overlay = document.getElementById('complement-overlay');
  const modal = document.getElementById('complement-modal');
  overlay.classList.remove('open');
  modal.classList.remove('open');
  document.body.style.overflow = '';
  complementModalProduct = null;
}

function renderComplementList() {
  const list = document.getElementById('complement-list');
  list.innerHTML = allComplements.map(c => {
    const checked = selectedComplements.find(sc => sc.id === c.id) ? 'checked' : '';
    const disabled = !checked && selectedComplements.length >= MAX_COMPLEMENTS;
    return `
      <label class="complement-item ${checked ? 'selected' : ''} ${disabled ? 'disabled' : ''}">
        <input type="checkbox" value="${c.id}" ${checked} ${disabled ? 'disabled' : ''}
          onchange="toggleComplement(${c.id}, this.checked)" />
        <span class="complement-name">${c.name}</span>
        <span class="complement-price">+R$ ${formatPrice(c.price)}</span>
      </label>`;
  }).join('');
}

function toggleComplement(id, checked) {
  const comp = allComplements.find(c => c.id === id);
  if (!comp) return;

  if (checked) {
    if (selectedComplements.length >= MAX_COMPLEMENTS) return;
    selectedComplements.push({ id: comp.id, name: comp.name, price: comp.price });
  } else {
    selectedComplements = selectedComplements.filter(sc => sc.id !== id);
  }
  renderComplementList();
  updateComplementTotal();
}

function updateComplementTotal() {
  const base = selectedSize ? selectedSize.price : (complementModalProduct ? complementModalProduct.price : 0);
  const total = base + selectedComplements.reduce((s, c) => s + c.price, 0);
  document.getElementById('complement-modal-total').textContent = `R$ ${formatPrice(total)} cada`;
}

function complementModalDecQty() {
  if (complementModalQty > 1) {
    complementModalQty--;
    document.getElementById('complement-modal-qty').textContent = complementModalQty;
  }
}

function complementModalIncQty() {
  complementModalQty++;
  document.getElementById('complement-modal-qty').textContent = complementModalQty;
}

function confirmComplementModal() {
  if (!complementModalProduct) return;

  const price = selectedSize ? selectedSize.price : complementModalProduct.price;
  const sizeData = selectedSize ? { name: selectedSize.name, diameter: selectedSize.diameter, price: selectedSize.price } : null;

  const existing = cart.find(i => i.id === complementModalProduct.id
    && JSON.stringify(i.complements || []) === JSON.stringify(selectedComplements)
    && (i.size?.name || null) === (sizeData?.name || null));

  if (existing) {
    existing.qty += complementModalQty;
  } else {
    cart.push({
      id: complementModalProduct.id,
      name: complementModalProduct.name,
      price: price,
      qty: complementModalQty,
      image: complementModalProduct.image,
      size: sizeData,
      complements: [...selectedComplements]
    });
  }

  closeComplementModal();
  renderCart();
  showAddedFeedback(complementModalProduct.id);
}

window.addToCart = async function(id) {
  try {
    const res = await fetch('/api/products');
    const list = await res.json();
    const p = list.find(x => x.id === id);
    if (!p) return;

    if (p.sizesJson) {
      openSizeModal(p);
    } else if (p.category === 'Bebida') {
      const existing = cart.find(i => i.id === id);
      if (existing) {
        existing.qty++;
      } else {
        cart.push({ id: p.id, name: p.name, price: p.price, qty: 1, image: p.image, complements: [] });
      }
      renderCart();
      showAddedFeedback(id);
    } else {
      openComplementModal(p);
    }
  } catch (e) {
    console.error('Erro ao adicionar:', e);
  }
};

function filterCategory(category) {
  currentCategory = category;
  document.querySelectorAll('.category-tab').forEach(tab => {
    tab.classList.toggle('active', tab.dataset.cat === category);
  });
  renderProducts(allProducts);
}

function renderProducts(products) {
  if (!productsEl) return;
  const filtered = currentCategory === 'all'
    ? products
    : products.filter(p => p.category === currentCategory);

  if (filtered.length === 0) {
    productsEl.innerHTML = '<div style="text-align:center;padding:40px;color:var(--text-light)">Nenhum produto encontrado nesta categoria.</div>';
    return;
  }

  productsEl.innerHTML = filtered.map(p => {
    const badge = p.category ? `<span class="product-badge">${p.category}</span>` : '';
    const sizes = parseSizes(p.sizesJson);
    let priceHtml;
    if (sizes.length > 0) {
      const minP = Math.min(...sizes.map(s => s.price));
      const maxP = Math.max(...sizes.map(s => s.price));
      if (minP === maxP) {
        const pp = minP.toFixed(2).split('.');
        priceHtml = `R$ ${pp[0]}<span class="cents">,${pp[1]}</span>`;
      } else {
        const p1 = minP.toFixed(2).split('.');
        const p2 = maxP.toFixed(2).split('.');
        priceHtml = `R$ ${p1[0]}<span class="cents">,${p1[1]}</span> – R$ ${p2[0]}<span class="cents">,${p2[1]}</span>`;
      }
    } else {
      const pp = p.price.toFixed(2).split('.');
      priceHtml = `R$ ${pp[0]}<span class="cents">,${pp[1]}</span>`;
    }
    return `
      <div class="product">
        <div class="product-img-wrap">
          <img src="${p.image}" alt="${p.name}" loading="lazy" />
          ${badge}
        </div>
        <div class="product-body">
          <h4>${p.name}</h4>
          <p class="product-desc">${p.description}</p>
          <div class="product-footer">
            <div class="product-price">${priceHtml}</div>
            <button class="btn-add" data-id="${p.id}" onclick="addToCart(${p.id})">+ Adicionar</button>
          </div>
        </div>
      </div>
    `;
  }).join('');
}

async function loadProducts() {
  if (!productsEl) return;
  productsEl.innerHTML = '<div style="text-align:center;padding:40px;color:var(--text-light)">Carregando...</div>';
  try {
    const res = await fetch('/api/products');
    allProducts = await res.json();
    renderProducts(allProducts);
  } catch (e) {
    productsEl.innerHTML = '<div style="text-align:center;padding:40px;color:var(--brand)">Erro ao carregar produtos. Tente novamente.</div>';
  }
}

async function loadSettings() {
  try {
    const res = await fetch('/api/settings');
    const data = await res.json();
    DELIVERY_FEE = data.deliveryFee;
    FREE_DELIVERY_MIN = data.freeDeliveryMin;
    return data;
  } catch (e) {
    console.error('Erro ao carregar configurações:', e);
    return null;
  }
}

async function loadComplements() {
  try {
    const res = await fetch('/api/complements/available');
    allComplements = await res.json();
  } catch (e) {
    console.error('Erro ao carregar complementos:', e);
  }
}

document.addEventListener('DOMContentLoaded', () => {
  renderCart();
  loadProducts();
  loadComplements();
  loadSettings().then(() => renderCart());

  document.querySelectorAll('[data-action="open-cart"]').forEach(el => {
    el.addEventListener('click', (e) => { e.preventDefault(); openCart(); });
  });
  document.querySelectorAll('[data-action="close-cart"], #cart-overlay').forEach(el => {
    el.addEventListener('click', (e) => { e.preventDefault(); closeCart(); });
  });
  document.querySelectorAll('#complement-overlay, [data-action="close-complement"]').forEach(el => {
    el.addEventListener('click', closeComplementModal);
  });
  document.getElementById('complement-modal-dec')?.addEventListener('click', complementModalDecQty);
  document.getElementById('complement-modal-inc')?.addEventListener('click', complementModalIncQty);
  document.getElementById('complement-modal-confirm')?.addEventListener('click', confirmComplementModal);

  document.querySelectorAll('#size-overlay, [data-action="close-size"]').forEach(el => {
    el.addEventListener('click', closeSizeModal);
  });
  document.getElementById('size-modal-confirm')?.addEventListener('click', confirmSize);

  gotoCheckout?.addEventListener('click', () => {
    location.href = '/Carrinho';
  });

  document.querySelectorAll('.category-tab').forEach(tab => {
    tab.addEventListener('click', () => filterCategory(tab.dataset.cat));
  });

  document.querySelector('.mobile-menu-btn')?.addEventListener('click', () => {
    document.querySelector('.top-nav')?.classList.toggle('open');
  });


});
