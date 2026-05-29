const productsEl = document.getElementById('products');
const cartItemsEl = document.getElementById('cart-items');
const gotoCheckout = document.getElementById('goto-checkout');
let cart = JSON.parse(localStorage.getItem('cart')||'[]');

function renderCart(){
  if(cart.length===0){ cartItemsEl.innerHTML='Sem itens no carrinho'; return }
  cartItemsEl.innerHTML = cart.map(i=>`<div>${i.name} x ${i.qty} — R$ ${i.price.toFixed(2)}</div>`).join('');
}

async function loadProducts(){
  productsEl.innerHTML='Carregando...';
  try{
    const res = await fetch('/api/products');
    const list = await res.json();
    productsEl.innerHTML = list.map(p=>`
      <div class="product">
        <img src="${p.image}" alt="${p.name}" />
        <h4>${p.name}</h4>
        <p>${p.description}</p>
        <div>R$ ${p.price.toFixed(2)}</div>
        <button onclick='addToCart(${p.id})'>Adicionar</button>
      </div>
    `).join('');
  }catch(e){ productsEl.innerHTML='Erro ao carregar produtos' }
}

window.addToCart = function(id){
  (async ()=>{
    const res = await fetch('/api/products');
    const list = await res.json();
    const p = list.find(x=>x.id===id);
    if(!p) return alert('Produto não encontrado');
    const existing = cart.find(i=>i.id===id);
    if(existing) existing.qty++;
    else cart.push({id:p.id,name:p.name,price:p.price,qty:1});
    localStorage.setItem('cart',JSON.stringify(cart));
    renderCart();
  })();
}

gotoCheckout?.addEventListener('click',()=>{
  location.href='/Checkout';
})

renderCart();
loadProducts();

// Nota: integração real de pagamento deve ocorrer no servidor.
// Exemplos:
// - Stripe: criar Checkout Session no servidor (/create-session) e redirecionar do cliente.
// - Pagar.me/PagSeguro: enviar detalhes do pedido e tokenizar cartão no cliente.
// Mantenha as chaves privadas no servidor e comente/endereço os trechos correspondentes.
