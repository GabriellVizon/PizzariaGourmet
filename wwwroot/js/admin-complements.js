async function loadAdminComplements() {
  const el = document.getElementById('complements-list');
  el.innerText = 'Carregando...';
  try {
    const res = await fetch('/api/complements');
    const list = await res.json();
    if (!list.length) { el.innerText = 'Nenhum complemento cadastrado.'; return; }
    el.innerHTML = `<table class="admin-table">
      <thead>
        <tr>
          <th>Id</th>
          <th>Nome</th>
          <th>Preço</th>
          <th>Disponível</th>
          <th>Ações</th>
        </tr>
      </thead>
      <tbody>${list.map(c => `
        <tr>
          <td>${c.id}</td>
          <td>${c.name}</td>
          <td>R$ ${c.price.toFixed(2)}</td>
          <td>${c.available ? '✅' : '❌'}</td>
          <td>
            <div class="admin-actions">
              <button data-id="${c.id}" class="btn-sm btn-edit">Editar</button>
              <button data-id="${c.id}" class="btn-sm btn-del">Excluir</button>
            </div>
          </td>
        </tr>`).join('')}
      </tbody>
    </table>`;

    document.querySelectorAll('.btn-edit').forEach(b => b.addEventListener('click', async (e) => {
      const id = Number(e.currentTarget.dataset.id);
      const r = await fetch('/api/complements/' + id);
      const c = await r.json();
      const form = document.getElementById('complement-form');
      form.elements['id'].value = c.id;
      form.elements['name'].value = c.name;
      form.elements['price'].value = c.price;
      form.elements['available'].checked = c.available;
    }));

    document.querySelectorAll('.btn-del').forEach(b => b.addEventListener('click', async (e) => {
      if (!confirm('Excluir complemento?')) return;
      const id = Number(e.currentTarget.dataset.id);
      const r = await fetch('/api/complements/' + id, { method: 'DELETE' });
      if (r.status === 401) { window.location.href = '/Admin/Login'; return; }
      loadAdminComplements();
    }));
  } catch (e) { el.innerText = 'Erro ao carregar complementos'; }
}

document.getElementById('complement-form').addEventListener('submit', async (e) => {
  e.preventDefault();
  const f = e.target;
  const id = f.elements['id'].value;
  const body = {
    name: f.elements['name'].value,
    price: Number(f.elements['price'].value),
    available: f.elements['available'].checked
  };

  const req = (url, opts) => fetch(url, opts).then(r => { if (r.status === 401) { window.location.href = '/Admin/Login'; throw new Error('Não autenticado'); } return r; });

  try {
    if (id) {
      await req('/api/complements/' + id, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body)
      });
    } else {
      await req('/api/complements', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body)
      });
    }
    f.reset();
    loadAdminComplements();
  } catch (e) {
    alert('Erro ao salvar complemento');
  }
});

document.getElementById('reset-complement-form')?.addEventListener('click', () => {
  document.getElementById('complement-form').reset();
});

// Check if we're on the complements page
if (document.getElementById('complements-list')) {
  loadAdminComplements();
}
