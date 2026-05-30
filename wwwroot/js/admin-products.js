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
  const body = {
    name: f.elements['name'].value,
    description: f.elements['description'].value,
    price: Number(f.elements['price'].value),
    image: f.elements['image'].value
  };
  if (f.elements['category']) body.category = f.elements['category'].value;

  if (id) {
    await fetch('/api/products/' + id, { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body) });
  } else {
    await fetch('/api/products', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body) });
  }
  f.reset();
  loadAdminProducts();
});

document.getElementById('reset-form')?.addEventListener('click', () => { document.getElementById('product-form').reset(); });

loadAdminProducts();
