(() => {
  const moeda = valor => Number(valor).toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
  const criar = (id, investido) => {
    const canvas = document.getElementById(id); if (!canvas) return;
    const dados = JSON.parse(canvas.dataset.pontos);
    new Chart(canvas, { type: 'bar', data: { labels: dados.rotulos, datasets: investido ? [
      { label: 'Novo', data: dados.novo, backgroundColor: '#0d6efd' },
      { label: 'Reinvestimento', data: dados.reinvestimento, backgroundColor: '#198754' }
    ] : [{ label: 'Retorno previsto', data: dados.valores, backgroundColor: '#6f42c1' }] }, options: {
      scales: { x: { stacked: investido }, y: { stacked: investido, beginAtZero: true, ticks: { callback: moeda } } },
      plugins: { legend: { display: investido }, tooltip: { callbacks: { label: contexto => moeda(contexto.raw) } } }
    } });
  };
  criar('grafico-retorno', false);
  criar('grafico-investido', true);
})();
