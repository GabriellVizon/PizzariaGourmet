async function loadAdminCoupons() {
  const el = document.getElementById('coupons-list');
  el.innerText = 'Carregando...';
  try {
    const res = await fetch('/api/coupons');
    const list = await res.json();
    if (!list.length) { el.innerText = 'Nenhum cupom cadastrado.'; return; }
    el.innerHTML = `<table class="admin-table">
      <thead>
        <tr>
          <th>Código</th>
          <th>Tipo</th>
          <th>Valor</th>
          <th>Mínimo</th>
          <th>Usos</th>
          <th>Validade</th>
          <th>Ativo</th>
          <th>Ações</th>
        </tr>
      </thead>
      <tbody>${list.map(c => `
        <tr>
          <td><strong>${c.code}</strong></td>
          <td>${c.discountType === 'percentage' ? '%' : 'R$'}</td>
          <td>${c.discountType === 'percentage' ? c.discountValue + '%' : 'R$ ' + c.discountValue.toFixed(2)}</td>
          <td>R$ ${c.minOrder.toFixed(2)}</td>
          <td>${c.usedCount}/${c.maxUses || '∞'}</td>
          <td style="font-size:0.8rem">${c.expiresAt ? new Date(c.expiresAt).toLocaleString('pt-BR') : '-'}</td>
          <td>${c.isActive ? '✅' : '❌'}</td>
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
      const r = await fetch('/api/coupons/' + id);
      const c = await r.json();
      const form = document.getElementById('coupon-form');
      form.elements['id'].value = c.id;
      form.elements['code'].value = c.code;
      form.elements['discountType'].value = c.discountType;
      form.elements['discountValue'].value = c.discountValue;
      form.elements['minOrder'].value = c.minOrder;
      form.elements['maxUses'].value = c.maxUses;
      form.elements['isActive'].checked = c.isActive;
      if (c.expiresAt) {
        const d = new Date(c.expiresAt);
        form.elements['expiresAt'].value = d.toISOString().slice(0, 16);
      }
    }));

    document.querySelectorAll('.btn-del').forEach(b => b.addEventListener('click', async (e) => {
      if (!confirm('Excluir cupom?')) return;
      const id = Number(e.currentTarget.dataset.id);
      const r = await fetch('/api/coupons/' + id, { method: 'DELETE' });
      if (r.status === 401) { window.location.href = '/Admin/Login'; return; }
      loadAdminCoupons();
    }));
  } catch (e) { el.innerText = 'Erro ao carregar cupons'; }
}

document.getElementById('coupon-form').addEventListener('submit', async (e) => {
  e.preventDefault();
  const f = e.target;
  const id = f.elements['id'].value;
  const body = {
    code: f.elements['code'].value.toUpperCase().trim(),
    discountType: f.elements['discountType'].value,
    discountValue: Number(f.elements['discountValue'].value),
    minOrder: Number(f.elements['minOrder'].value) || 0,
    maxUses: Number(f.elements['maxUses'].value) || 0,
    isActive: f.elements['isActive'].checked,
    expiresAt: f.elements['expiresAt'].value ? f.elements['expiresAt'].value + ':00' : null
  };

  const req = (url, opts) => fetch(url, opts).then(r => { if (r.status === 401) { window.location.href = '/Admin/Login'; throw new Error('Não autenticado'); } return r; });

  try {
    if (id) {
      await req('/api/coupons/' + id, { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body) });
    } else {
      await req('/api/coupons', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body) });
    }
    f.reset();
    loadAdminCoupons();
  } catch (e) { alert('Erro ao salvar cupom'); }
});

document.getElementById('reset-coupon-form')?.addEventListener('click', () => {
  document.getElementById('coupon-form').reset();
});

if (document.getElementById('coupons-list')) {
  loadAdminCoupons();
}
