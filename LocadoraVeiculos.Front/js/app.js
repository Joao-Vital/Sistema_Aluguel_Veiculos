// Front-end da Locadora: consome a API do TP1 somente com fetch
const API = "http://localhost:5236/api";
const $ = s => document.querySelector(s);
const esc = t => { const d = document.createElement("div"); d.textContent = t ?? ""; return d.innerHTML.replace(/"/g, "&quot;"); };
const brl = v => Number(v).toLocaleString("pt-BR", { style: "currency", currency: "BRL" });
const dt = v => v ? new Date(v).toLocaleDateString("pt-BR") : "—";
const sn = v => (v === true || v === "true") ? "Sim" : "Não";

// ---------- Acesso à API ----------
async function http(metodo, url, corpo) {
  const r = await fetch(API + url, { method: metodo, headers: corpo ? { "Content-Type": "application/json" } : {}, body: corpo ? JSON.stringify(corpo) : undefined });
  const txt = await r.text(); let j = null;
  try { j = txt ? JSON.parse(txt) : null; } catch { }
  if (!r.ok) throw new Error(!j ? "Erro " + r.status : j.mensagem || (j.erros ? Object.values(j.erros).flat().join(" ") : j.titulo || j.title || "Erro " + r.status));
  return j;
}

// ---------- Configuração das entidades (telas geradas a partir daqui) ----------
// cols: [campo, rótulo, formatador]; filtros: [campo, rótulo, 'texto'|'select', formatador]; campos: formulário
const ENT = {
  veiculos: { nome: "Veículos", sing: "veículo", rota: "veiculos", id: "veiculoId",
    cols: [["modelo", "Modelo"], ["placa", "Placa"], ["fabricanteNome", "Fabricante"], ["categoriaVeiculoNome", "Categoria"], ["anoFabricacao", "Ano"], ["quilometragem", "Km"], ["disponivel", "Disponível", sn]],
    filtros: [["modelo", "Modelo", "texto"], ["placa", "Placa", "texto"], ["fabricanteNome", "Fabricante", "select"], ["disponivel", "Disponível", "select", sn]],
    campos: [{ n: "modelo", r: "Modelo", req: 1 }, { n: "placa", r: "Placa", req: 1, max: 8 }, { n: "anoFabricacao", r: "Ano de fabricação", t: "number", req: 1 },
      { n: "quilometragem", r: "Quilometragem", t: "number", req: 1 }, { n: "fabricanteId", r: "Fabricante", ref: ["fabricantes", x => x.nome] },
      { n: "categoriaVeiculoId", r: "Categoria", ref: ["categorias", x => x.nome] }, { n: "disponivel", r: "Disponível", t: "bool", soEdicao: 1 }] },
  fabricantes: { nome: "Fabricantes", sing: "fabricante", rota: "fabricantes", id: "fabricanteId",
    cols: [["nome", "Nome"], ["paisOrigem", "País de origem"], ["quantidadeVeiculos", "Veículos"]],
    filtros: [["nome", "Nome", "texto"], ["paisOrigem", "País", "select"]],
    campos: [{ n: "nome", r: "Nome", req: 1 }, { n: "paisOrigem", r: "País de origem" }] },
  categorias: { nome: "Categorias", sing: "categoria", rota: "categorias-veiculo", id: "categoriaVeiculoId",
    cols: [["nome", "Nome"], ["descricao", "Descrição"], ["valorDiariaBase", "Diária base", brl], ["quantidadeVeiculos", "Veículos"]],
    filtros: [["nome", "Nome", "texto"], ["descricao", "Descrição", "texto"]],
    campos: [{ n: "nome", r: "Nome", req: 1 }, { n: "descricao", r: "Descrição" }, { n: "valorDiariaBase", r: "Diária base (R$)", t: "number", step: "0.01", req: 1 }] },
  clientes: { nome: "Clientes", sing: "cliente", rota: "clientes", id: "clienteId",
    cols: [["nome", "Nome"], ["cpf", "CPF"], ["email", "E-mail"], ["telefone", "Telefone"], ["quantidadeAlugueis", "Aluguéis"]],
    filtros: [["nome", "Nome", "texto"], ["cpf", "CPF", "texto"], ["email", "E-mail", "texto"]],
    campos: [{ n: "nome", r: "Nome", req: 1 }, { n: "cpf", r: "CPF (11 dígitos)", req: 1, pat: "\\d{11}", max: 11 }, { n: "email", r: "E-mail", t: "email", req: 1 }, { n: "telefone", r: "Telefone" }] },
  alugueis: { nome: "Aluguéis", sing: "aluguel", rota: "alugueis", id: "aluguelId", semEdicao: 1,
    pre: l => ({ ...l, _sit: l.emAberto ? (l.emAtraso ? "Em atraso" : "Em aberto") : "Finalizado" }),
    cols: [["clienteNome", "Cliente"], ["veiculoModelo", "Veículo"], ["veiculoPlaca", "Placa"], ["dataInicio", "Início", dt], ["dataFimPrevista", "Prev. devolução", dt], ["dataDevolucao", "Devolução", dt], ["valorTotal", "Total", brl], ["_sit", "Situação"]],
    filtros: [["clienteNome", "Cliente", "texto"], ["veiculoPlaca", "Placa", "texto"], ["_sit", "Situação", "select"]],
    campos: [{ n: "clienteId", r: "Cliente", ref: ["clientes", x => `${x.nome} (${x.cpf})`] }, { n: "veiculoId", r: "Veículo (disponíveis)", ref: ["veiculos", x => `${x.modelo} - ${x.placa}`, x => x.disponivel] },
      { n: "dataInicio", r: "Data de início", t: "date", req: 1 }, { n: "dataFimPrevista", r: "Devolução prevista", t: "date", req: 1 }, { n: "valorDiaria", r: "Valor da diária (vazio = diária da categoria)", t: "number", step: "0.01" }] }
};
// As 5 rotas de filtro com JOIN da etapa 2: p = parâmetros [chave, rótulo, entidade|'date', rótulo da opção]
const CONS = [
  { t: "Veículos disponíveis por categoria", j: "JOIN por navegação (Include)", p: [["c", "Categoria", "categorias", x => x.nome]], u: p => `/veiculos/filtro/disponiveis-por-categoria/${p.c}` },
  { t: "Veículos de um fabricante (aluguéis e valor arrecadado)", j: "JOIN explícito (join do LINQ)", p: [["f", "Fabricante", "fabricantes", x => x.nome]], u: p => `/veiculos/filtro/fabricante/${p.f}` },
  { t: "Veículos que nunca foram alugados", j: "LEFT JOIN (GroupJoin + DefaultIfEmpty)", p: [], u: () => "/veiculos/filtro/sem-aluguel" },
  { t: "Histórico de aluguéis de um cliente", j: "3 JOINs encadeados (4 tabelas)", p: [["c", "Cliente", "clientes", x => `${x.nome} (${x.cpf})`]], u: p => `/alugueis/filtro/cliente/${p.c}` },
  { t: "Aluguéis iniciados em um período", j: "JOIN por navegação (Include)", p: [["i", "De", "date"], ["f", "Até", "date"]], u: p => `/alugueis/filtro/periodo?inicio=${p.i}&fim=${p.f}` }
];

// ---------- Utilitários de interface ----------
const main = $("#main"), mForm = new bootstrap.Modal("#mForm"), mConf = new bootstrap.Modal("#mConf");
function aviso(msg, tipo = "success") {
  const a = $("#aviso"); a.className = `alert alert-${tipo}`; a.textContent = msg;
  clearTimeout(aviso.t); aviso.t = setTimeout(() => a.classList.add("d-none"), 4000);
}
const falha = x => `<div class="alert alert-danger">${esc(x.message)} — a API está rodando?</div>`;

async function formulario({ titulo, campos, valores = {}, salvar }, depois) {
  const f = $("#form"); $("#fTitulo").textContent = titulo; $("#fErro").classList.add("d-none");
  f.classList.remove("was-validated"); $("#fCampos").innerHTML = "Carregando..."; mForm.show();
  try {
    $("#fCampos").innerHTML = (await Promise.all(campos.map(async c => {
      const v = valores[c.n] ?? ""; let ctl;
      if (c.ref) {
        const [ent, rot, filt] = c.ref, itens = (await http("GET", "/" + ENT[ent].rota)).filter(filt || (() => true));
        ctl = `<select class="form-select" name="${c.n}" required><option value="">Selecione...</option>${itens.map(i => `<option value="${i[ENT[ent].id]}" ${i[ENT[ent].id] == v ? "selected" : ""}>${esc(rot(i))}</option>`).join("")}</select>`;
      } else if (c.t === "bool") ctl = `<select class="form-select" name="${c.n}"><option value="true" ${v !== false ? "selected" : ""}>Sim</option><option value="false" ${v === false ? "selected" : ""}>Não</option></select>`;
      else ctl = `<input class="form-control" name="${c.n}" type="${c.t || "text"}" value="${esc(v)}" ${c.req ? "required" : ""} ${c.step ? `step="${c.step}"` : ""} ${c.pat ? `pattern="${c.pat}"` : ""} ${c.max ? `maxlength="${c.max}"` : ""}>`;
      return `<div class="mb-3"><label class="form-label">${c.r}</label>${ctl}</div>`;
    }))).join("");
  } catch (x) { $("#fCampos").innerHTML = ""; $("#fErro").textContent = x.message; $("#fErro").classList.remove("d-none"); }
  f.onsubmit = async ev => {
    ev.preventDefault();
    if (!f.checkValidity()) { f.classList.add("was-validated"); return; }
    const b = {};
    campos.forEach(c => { const r = f.elements[c.n].value.trim(); b[c.n] = c.t === "bool" ? r === "true" : (c.t === "number" || c.ref) ? (r === "" ? null : Number(r)) : (r === "" ? null : r); });
    try { await salvar(b); mForm.hide(); aviso("Operação realizada com sucesso!"); depois(); }
    catch (x) { $("#fErro").textContent = x.message; $("#fErro").classList.remove("d-none"); }
  };
}

function confirmar(msg, acao, depois) {
  $("#cMsg").textContent = msg; $("#cErro").classList.add("d-none"); mConf.show();
  $("#cOk").onclick = async () => {
    try { await acao(); mConf.hide(); aviso("Registro excluído com sucesso!"); depois(); }
    catch (x) { $("#cErro").textContent = x.message; $("#cErro").classList.remove("d-none"); }
  };
}

// ---------- Tela de listagem (CRUD + filtros) ----------
async function lista(chave) {
  const e = ENT[chave]; let dados = [];
  main.innerHTML = '<p class="text-muted">Carregando...</p>';
  try { dados = (await http("GET", "/" + e.rota)).map(e.pre || (x => x)); } catch (x) { main.innerHTML = falha(x); return; }
  const fil = e.filtros.map(([k, r, t, f], i) => `<div class="col-md-3"><label class="form-label small mb-0">${r}</label>` + (t === "texto"
    ? `<input class="form-control form-control-sm filtro" data-i="${i}" placeholder="Buscar...">`
    : `<select class="form-select form-select-sm filtro" data-i="${i}"><option value="">Todos</option>${[...new Set(dados.map(d => String(d[k] ?? "")))].filter(v => v).sort().map(v => `<option value="${esc(v)}">${esc(f ? f(v) : v)}</option>`).join("")}</select>`) + "</div>").join("");
  main.innerHTML = `<div class="d-flex justify-content-between align-items-center mb-3"><h3 class="mb-0">${e.nome}</h3><button class="btn btn-success" id="novo">+ ${e.sing === "categoria" ? "Nova" : "Novo"} ${e.sing}</button></div>
    <div class="card card-body mb-3"><div class="row g-2">${fil}</div></div>
    <div class="card"><div class="table-responsive"><table class="table table-hover mb-0"><thead class="table-light"><tr>${e.cols.map(c => `<th>${c[1]}</th>`).join("")}<th>Ações</th></tr></thead><tbody id="corpo"></tbody></table></div></div>
    <p class="small text-muted mt-2" id="total"></p>`;
  const linha = d => `<tr>${e.cols.map(([k, , f]) => `<td>${esc(f ? f(d[k], d) : d[k] ?? "—")}</td>`).join("")}<td class="text-end">
    ${chave === "alugueis" && d.emAberto ? `<button class="btn btn-sm btn-outline-success" data-a="devolver" data-id="${d[e.id]}">Devolver</button> ` : ""}
    ${e.semEdicao ? "" : `<button class="btn btn-sm btn-outline-primary" data-a="editar" data-id="${d[e.id]}">Editar</button> `}
    <button class="btn btn-sm btn-outline-danger" data-a="excluir" data-id="${d[e.id]}">Excluir</button></td></tr>`;
  const atualizar = () => {
    const fs = [...document.querySelectorAll(".filtro")];
    const vis = dados.filter(d => fs.every(el => { const v = el.value.trim().toLowerCase(); if (!v) return true; const x = String(d[e.filtros[el.dataset.i][0]] ?? "").toLowerCase(); return el.tagName === "SELECT" ? x === v : x.includes(v); }));
    $("#corpo").innerHTML = vis.length ? vis.map(linha).join("") : '<tr><td colspan="99" class="text-center text-muted py-3">Nenhum registro encontrado.</td></tr>';
    $("#total").textContent = `${vis.length} de ${dados.length} registro(s)`;
  };
  const recarregar = () => lista(chave);
  document.querySelectorAll(".filtro").forEach(el => el.addEventListener("input", atualizar));
  atualizar();
  $("#novo").onclick = () => formulario({ titulo: `${e.sing === "categoria" ? "Nova" : "Novo"} ${e.sing}`, campos: e.campos.filter(c => !c.soEdicao), salvar: b => http("POST", "/" + e.rota, b) }, recarregar);
  $("#corpo").onclick = ev => {
    const bt = ev.target.closest("button"); if (!bt) return;
    const id = bt.dataset.id, d = dados.find(x => x[e.id] == id);
    if (bt.dataset.a === "editar") formulario({ titulo: `Editar ${e.sing}`, campos: e.campos, valores: d, salvar: b => http("PUT", `/${e.rota}/${id}`, b) }, recarregar);
    if (bt.dataset.a === "excluir") confirmar(`Tem certeza de que deseja excluir ${e.sing === "categoria" ? "esta" : "este"} ${e.sing}? Esta ação não pode ser desfeita.`, () => http("DELETE", `/${e.rota}/${id}`), recarregar);
    if (bt.dataset.a === "devolver") formulario({ titulo: `Registrar devolução - ${d.veiculoModelo}`, campos: [{ n: "quilometragemFinal", r: "Quilometragem final", t: "number", req: 1 }, { n: "dataDevolucao", r: "Data da devolução (vazio = hoje)", t: "date" }], salvar: b => http("PATCH", `/alugueis/${id}/devolucao`, b) }, recarregar);
  };
}

// ---------- Tela de consultas (5 rotas de filtro com JOIN) ----------
function tabelaGenerica(r) {
  const arr = Array.isArray(r) ? r : [r];
  if (!arr.length) return '<p class="text-muted mb-0">Nenhum resultado para esta consulta.</p>';
  const ks = Object.keys(arr[0]), h = k => k.replace(/([A-Z])/g, " $1").replace(/^./, c => c.toUpperCase());
  const fmt = (k, v) => v == null ? "—" : typeof v === "boolean" ? sn(v) : typeof v === "string" && /^\d{4}-\d\d-\d\dT/.test(v) ? dt(v) : typeof v === "number" && /valor|total|arrecad/i.test(k) ? brl(v) : typeof v === "object" ? JSON.stringify(v) : v;
  return `<div class="table-responsive"><table class="table table-sm table-striped mb-1"><thead><tr>${ks.map(k => `<th>${h(k)}</th>`).join("")}</tr></thead><tbody>${arr.map(l => `<tr>${ks.map(k => `<td>${esc(fmt(k, l[k]))}</td>`).join("")}</tr>`).join("")}</tbody></table></div><div class="small text-muted">${arr.length} resultado(s)</div>`;
}

async function consultas() {
  main.innerHTML = '<p class="text-muted">Carregando...</p>'; const L = {};
  try { for (const ent of ["categorias", "fabricantes", "clientes"]) L[ent] = await http("GET", "/" + ENT[ent].rota); } catch (x) { main.innerHTML = falha(x); return; }
  main.innerHTML = '<h3>Consultas</h3><p class="text-muted">Cada consulta chama uma rota de filtro da API (com JOIN no banco).</p>' + CONS.map((c, i) => `<div class="card card-consulta mb-3"><div class="card-body">
    <h6 class="mb-0">${i + 1}. ${c.t}</h6><div class="small text-muted mb-2">${c.j}</div>
    <div class="row g-2 align-items-end">${c.p.map(([k, r, ent, rot]) => `<div class="col-md-4"><label class="form-label small mb-0">${r}</label>` + (ent === "date" ? `<input type="date" class="form-control form-control-sm" data-p="${k}">`
      : `<select class="form-select form-select-sm" data-p="${k}"><option value="">Selecione...</option>${L[ent].map(x => `<option value="${x[ENT[ent].id]}">${esc(rot(x))}</option>`).join("")}</select>`) + "</div>").join("")}
    <div class="col-auto"><button class="btn btn-sm btn-primary" data-i="${i}">Consultar</button></div></div><div class="mt-3" id="res${i}"></div></div></div>`).join("");
  main.onclick = async ev => {
    const bt = ev.target.closest("button[data-i]"); if (!bt) return;
    const i = bt.dataset.i, c = CONS[i], p = {}; let ok = true;
    bt.closest(".card").querySelectorAll("[data-p]").forEach(el => { p[el.dataset.p] = el.value; if (!el.value) ok = false; });
    if (!ok) return aviso("Preencha os parâmetros da consulta.", "warning");
    try { $("#res" + i).innerHTML = `<code>GET /api${c.u(p)}</code><div class="mt-2">${tabelaGenerica(await http("GET", c.u(p)))}</div>`; }
    catch (x) { $("#res" + i).innerHTML = `<div class="alert alert-danger mb-0">${esc(x.message)}</div>`; }
  };
}

// ---------- Navegação ----------
const ROTAS = { ...Object.fromEntries(Object.entries(ENT).map(([k, e]) => [k, e.nome])), consultas: "Consultas" };
$("#menu").innerHTML = Object.entries(ROTAS).map(([k, n]) => `<li class="nav-item"><a class="nav-link" href="#${k}" data-k="${k}">${n}</a></li>`).join("");
function ir() {
  const k = ROTAS[location.hash.slice(1)] ? location.hash.slice(1) : "veiculos";
  document.querySelectorAll("#menu .nav-link").forEach(a => a.classList.toggle("active", a.dataset.k === k));
  main.onclick = null;
  k === "consultas" ? consultas() : lista(k);
}
addEventListener("hashchange", ir); ir();
