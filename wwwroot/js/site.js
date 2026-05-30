let cart = JSON.parse(localStorage.getItem('cart') || '[]');
const DELIVERY_FEE = 5.00;
const FREE_DELIVERY_MIN = 50.00;

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

// Category filtering
let currentCategory = 'all';
let allProducts = [];

function openCart() {
  cartSidebar?.classList.add('open');
  cartOverlay?.classList.add('open');
  document.body.style.overflow = 'hidden';
}

function closeCart() {
  cartSidebar?.classList.remove('open');
  cartOverlay?.classList.remove('open');
  document.body.style.overflow = '';
}

function formatPrice(price) {
  return price.toLocaleString('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}

function getDeliveryFee(subtotal) {
  return subtotal >= FREE_DELIVERY_MIN ? 0 : DELIVERY_FEE;
}

function renderCart() {
  if (!cartItemsEl) { updateCartBadge(); return; }

  const subtotal = cart.reduce((sum, i) => sum + i.price * i.qty, 0);
  const delivery = getDeliveryFee(subtotal);
  const total = subtotal + delivery;

  if (cart.length === 0) {
    cartItemsEl.innerHTML = `
      <div class="cart-empty">
        <div class="cart-empty-icon">🛒</div>
        <div>Seu carrinho está vazio</div>
        <div style="font-size:0.8rem;margin-top:4px">Adicione produtos do cardápio</div>
      </div>`;
  } else {
    cartItemsEl.innerHTML = cart.map((item, idx) => `
      <div class="cart-item">
        <div class="cart-item-info">
          <div class="cart-item-name">${item.name}</div>
          <div class="cart-item-price">R$ ${formatPrice(item.price)}</div>
        </div>
        <div class="cart-item-qty">
          <button onclick="decrementCart(${idx})">−</button>
          <span>${item.qty}</span>
          <button onclick="incrementCart(${idx})">+</button>
        </div>
        <button class="cart-item-remove" onclick="removeFromCart(${idx})" title="Remover">✕</button>
      </div>
    `).join('');
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
  const subtotal = cart.reduce((sum, i) => sum + i.price * i.qty, 0);
  const total = subtotal + getDeliveryFee(subtotal);

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

window.addToCart = async function(id) {
  try {
    const res = await fetch('/api/products');
    const list = await res.json();
    const p = list.find(x => x.id === id);
    if (!p) return;
    const existing = cart.find(i => i.id === id);
    if (existing) {
      existing.qty++;
    } else {
      cart.push({ id: p.id, name: p.name, price: p.price, qty: 1, image: p.image });
    }
    renderCart();
    openCart();
    // Brief animation feedback
    const btn = document.querySelector(`[data-id="${id}"]`);
    if (btn) {
      btn.textContent = '✓ Adicionado';
      btn.style.background = '#27ae60';
      setTimeout(() => {
        btn.textContent = 'Adicionar';
        btn.style.background = '';
      }, 1200);
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
    const priceParts = p.price.toFixed(2).split('.');
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
            <div class="product-price">R$ <span class="cents">${priceParts[0]}</span>,${priceParts[1]}</div>
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

// DOM ready
document.addEventListener('DOMContentLoaded', () => {
  renderCart();
  loadProducts();

  // Cart sidebar controls
  document.querySelectorAll('[data-action="open-cart"]').forEach(el => {
    el.addEventListener('click', openCart);
  });
  document.querySelectorAll('[data-action="close-cart"], #cart-overlay').forEach(el => {
    el.addEventListener('click', closeCart);
  });

  // Checkout button
  gotoCheckout?.addEventListener('click', () => {
    location.href = '/Checkout';
  });

  // Category tabs
  document.querySelectorAll('.category-tab').forEach(tab => {
    tab.addEventListener('click', () => filterCategory(tab.dataset.cat));
  });

  // Mobile menu toggle
  document.querySelector('.mobile-menu-btn')?.addEventListener('click', () => {
    document.querySelector('.top-nav')?.classList.toggle('open');
  });

  // Payment method selection
  document.querySelectorAll('.payment-option')?.forEach(opt => {
    opt.addEventListener('click', () => {
      document.querySelectorAll('.payment-option').forEach(o => o.classList.remove('selected'));
      opt.classList.add('selected');
      document.getElementById('payment-method').value = opt.dataset.method;
    });
  });
});
