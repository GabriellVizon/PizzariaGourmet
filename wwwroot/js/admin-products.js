let editingSizes = [];

function renderSizeEditor(sizes) {
  const container = document.getElementById('size-editor');
  editingSizes = sizes.length > 0 ? [...sizes] : [{ name: 'P', diameter: 30, price: 0 }, { name: 'M', diameter: 40, price: 0 }, { name: 'G', diameter: 50, price: 0 }];
  renderSizeRows();
}

function renderSizeRows() {
  const container = document.getElementById('size-editor');
  container.innerHTML = `
    <div style="margin-bottom:8px"><strong>Tamanhos</strong> <small style="color:var(--text-light)">(deixe vazio se não tiver tamanhos)</small></div>
    <table style="width:100%;border-collapse:collapse;margin-bottom:8px">
      <thead>
        <tr>
          <th style="text-align:left;font-size:0.75rem;padding:4px 8px">Nome</th>
          <th style="text-align:left;font-size:0.75rem;padding:4px 8px">Diâmetro (cm)</th>
          <th style="text-align:left;font-size:0.75rem;padding:4px 8px">Preço (R$)</th>
          <th style="width:36px"></th>
        </tr>
      </thead>
      <tbody>
        ${editingSizes.map((s, i) => `
          <tr>
            <td><input name="size_name_${i}" value="${s.name}" style="width:60px" placeholder="P" /></td>
            <td><input name="size_diameter_${i}" value="${s.diameter || ''}" type="number" style="width:70px" placeholder="30" /></td>
            <td><input name="size_price_${i}" value="${s.price}" type="number" step="0.01" style="width:80px" placeholder="0" /></td>
            <td><button type="button" onclick="removeSize(${i})" style="background:#e74c3c;padding:4px 8px;font-size:0.8rem">✕</button></td>
          </tr>
        `).join('')}
      </tbody>
    </table>
    <button type="button" onclick="addSize()" style="background:var(--accent);padding:4px 14px;font-size:0.8rem">+ Tamanho</button>
  `;
}

window.addSize = function() {
  editingSizes.push({ name: '', diameter: null, price: 0 });
  renderSizeRows();
};

window.removeSize = function(idx) {
  editingSizes.splice(idx, 1);
  renderSizeRows();
};

function collectSizes() {
  return editingSizes.map((s, i) => ({
    name: document.querySelector(`[name="size_name_${i}"]`)?.value || s.name,
    diameter: parseInt(document.querySelector(`[name="size_diameter_${i}"]`)?.value) || null,
    price: parseFloat(document.querySelector(`[name="size_price_${i}"]`)?.value) || 0
  })).filter(s => s.name && s.price > 0);
}

async function loadAdminProducts() {
  const el = document.getElementById('products-list');
  el.innerText = 'Carregando...';
  try {
    const res = await fetch('/api/products');
    const list = await res.json();
    if (!list.length) { el.innerText = 'Nenhum produto cadastrado.'; return; }
    el.innerHTML = `<table class="admin-table">
      <thead>
        <tr>
          <th>Id</th>
          <th>Nome</th>
          <th>Categoria</th>
          <th>Preço</th>
          <th>Disponível</th>
          <th>Ações</th>
        </tr>
      </thead>
      <tbody>${list.map(p => `
        <tr>
          <td>${p.id}</td>
          <td class="td-name">${p.name}</td>
          <td>${p.category || '-'}</td>
          <td>R$ ${p.price.toFixed(2)}</td>
          <td>${p.available ? '✅' : '❌'}</td>
          <td>
            <div class="admin-actions">
              <button data-id="${p.id}" class="btn-sm btn-edit">Editar</button>
              <button data-id="${p.id}" class="btn-sm btn-del">Excluir</button>
            </div>
          </td>
        </tr>`).join('')}
      </tbody>
    </table>`;

    document.querySelectorAll('.btn-edit').forEach(b => b.addEventListener('click', async (e) => {
      const id = Number(e.currentTarget.dataset.id);
      const r = await fetch('/api/products/' + id);
      const p = await r.json();
      const form = document.getElementById('product-form');
      form.elements['id'].value = p.id;
      form.elements['name'].value = p.name;
      form.elements['description'].value = p.description;
      form.elements['price'].value = p.price;
      form.elements['image'].value = p.image;
      if (form.elements['category']) form.elements['category'].value = p.category || '';
      const sizes = p.sizesJson ? JSON.parse(p.sizesJson) : [];
      renderSizeEditor(sizes);
    }));

    document.querySelectorAll('.btn-del').forEach(b => b.addEventListener('click', async (e) => {
      if (!confirm('Excluir produto?')) return;
      const id = Number(e.currentTarget.dataset.id);
      await fetch('/api/products/' + id, { method: 'DELETE' });
      loadAdminProducts();
    }));
  } catch (e) { el.innerText = 'Erro ao carregar produtos'; }
}

document.getElementById('product-form').addEventListener('submit', async (e) => {
  e.preventDefault();
  const f = e.target;
  const id = f.elements['id'].value;
  const sizes = collectSizes();
  const body = {
    name: f.elements['name'].value,
    description: f.elements['description'].value,
    price: Number(f.elements['price'].value),
    image: f.elements['image'].value,
    sizesJson: sizes.length > 0 ? JSON.stringify(sizes) : null
  };
  if (f.elements['category']) body.category = f.elements['category'].value;

  if (id) {
    await fetch('/api/products/' + id, { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body) });
  } else {
    await fetch('/api/products', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body) });
  }
  f.reset();
  renderSizeEditor([]);
  loadAdminProducts();
});

document.getElementById('reset-form')?.addEventListener('click', () => {
  document.getElementById('product-form').reset();
  renderSizeEditor([]);
});

loadAdminProducts();
