"use strict";
/* ==========================================================================
   Clinic Portal – plain JavaScript (no libraries, no build step).
   1 Settings | 2 Helpers | 3 API | 4 UI helpers | 5 Shared pieces
   6 Patient pages | 7 Doctor pages | 8 Staff & admin pages | 9 Session & start
   ========================================================================== */

/* ===== 1. SETTINGS ===== */
// Change this ONE line if your backend runs at a different address.
const API_BASE_URL = "http://localhost:5220";

const TOKEN_KEY = "clinic_token";

// Swagger does not describe the /api/Auth/me response. If your backend uses
// other property names, add them to these lists (dots mean nested objects).
const ME_FIELDS = {
  patientId: ["patientId", "patient.id"],
  doctorId: ["doctorId", "doctor.id"],
  userId: ["userId", "id"],
  email: ["email", "userName"],
  name: ["fullName", "name"],
  roles: ["roles", "role"]
};

const ROLE_ORDER = ["SuperAdmin", "Admin", "Doctor", "Receptionist", "Patient"];
const DAYS = ["Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"];
const DEAD_STATUSES = ["cancelled", "canceled", "rejected", "noshow", "completed"];
// The API permits NoShow after the 30-minute visit slot and 15-minute grace period.
const NO_SHOW_DELAY_MINUTES = 45;

const state = { token: sessionStorage.getItem(TOKEN_KEY), me: null, role: null, patientId: null, doctorId: null, nav: [], flash: {} };
const cache = { specialties: null, doctors: null };

/* ===== 2. HELPERS ===== */
const $ = (sel, root = document) => root.querySelector(sel);
const esc = v => String(v ?? "").replace(/[&<>"']/g, c => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[c]));
const deep = (o, path) => path.split(".").reduce((x, k) => (x == null ? undefined : x[k]), o);
const pick = (o, ...paths) => { for (const p of paths) { const v = deep(o, p); if (v !== undefined && v !== null && v !== "") return v; } return undefined; };
const asList = d => { if (Array.isArray(d)) return d; for (const k of ["items", "data", "results", "$values"]) if (d && Array.isArray(d[k])) return d[k]; return []; };
const pad = n => String(n).padStart(2, "0");
const toInt = v => (v === "" || v == null ? null : Number(v));

function toDate(v) {
  if (!v) return null;
  if (/^\d{4}-\d{2}-\d{2}$/.test(v)) { const [y, m, d] = v.split("-").map(Number); return new Date(y, m - 1, d); }
  const d = new Date(v);
  return isNaN(d) ? null : d;
}
const fmtDateTime = v => { const d = toDate(v); return d ? d.toLocaleString([], { dateStyle: "medium", timeStyle: "short" }) : "—"; };
const fmtDate = v => { const d = toDate(v); return d ? d.toLocaleDateString([], { dateStyle: "medium" }) : "—"; };
const fmtTime = v => { const d = toDate(v); return d ? d.toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" }) : String(v); };
const todayStr = () => { const d = new Date(); return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`; };
const hasStarted = v => { const d = toDate(v); return !!d && d <= new Date(); };

const hasRole = (...names) => names.includes(state.role);
const isAdmin = () => hasRole("Admin", "SuperAdmin");

function personName(o, fallback) {
  if (!o) return fallback;
  const n = pick(o, "fullName", "name");
  if (n) return n;
  return `${o.firstName || ""} ${o.lastName || ""}`.trim() || fallback;
}
function activeFlag(o) {
  const a = pick(o, "isActive", "active", "isEnabled");
  if (a !== undefined) return !!a;
  const l = pick(o, "isLocked", "isDeactivated", "isDisabled", "lockedOut");
  return l === undefined ? undefined : !l;
}
const dayName = v => (typeof v === "number" ? DAYS[v] : String(v ?? ""));

/* ===== 3. API ===== */
class ApiError extends Error {
  constructor(status, message, data) { super(message); this.status = status; this.data = data; }
}

function errorMessage(status, data) {
  let detail = "";
  if (typeof data === "string") detail = data.length < 300 ? data.trim() : "";
  else if (data && typeof data === "object") {
    if (data.errors && typeof data.errors === "object") detail = Object.values(data.errors).flat().join(" ");
    detail = detail || String(pick(data, "detail", "message", "error", "title") ?? "");
  }
  const generic = {
    400: "The request was not valid.", 401: "You are not signed in or your session has expired.",
    403: "You don't have permission to do this.", 404: "Not found.",
    409: "This conflicts with existing data.", 422: "The data was not accepted."
  }[status] || (status >= 500 ? "The server had a problem. Please try again later." : `Request failed (${status}).`);
  const useless = /^(forbidden|unauthorized|bad request|not found|one or more validation errors occurred\.?)$/i;
  if (!detail || useless.test(detail)) return generic;
  return status === 403 ? `${generic} ${detail}` : detail;
}

async function api(path, { method = "GET", body, query, auth = true } = {}) {
  let url = API_BASE_URL.replace(/\/+$/, "") + path;
  const params = new URLSearchParams();
  for (const [k, v] of Object.entries(query || {})) if (v !== undefined && v !== null && v !== "") params.set(k, v);
  if ([...params].length) url += "?" + params.toString();

  const headers = { Accept: "application/json" };
  if (body !== undefined) headers["Content-Type"] = "application/json";
  if (auth && state.token) headers.Authorization = `Bearer ${state.token}`;

  let res;
  try {
    res = await fetch(url, { method, headers, body: body !== undefined ? JSON.stringify(body) : undefined });
  } catch (e) {
    throw new ApiError(0, `Cannot reach the API at ${API_BASE_URL}. Check that the backend is running, that the address in app.js is right, and see the notes about CORS / the HTTPS certificate.`);
  }
  const text = await res.text();
  let data = null;
  if (text) { try { data = JSON.parse(text); } catch { data = text; } }

  if (!res.ok) {
    if (res.status === 401 && auth && state.token) { handleExpired(); throw new ApiError(401, "Your session has expired. Please sign in again.", data); }
    throw new ApiError(res.status, errorMessage(res.status, data), data);
  }
  // Unwrap successful ApiResponse<T> responses once so page code receives the DTO/list.
  if (data && typeof data === "object"
      && Object.prototype.hasOwnProperty.call(data, "success")
      && Object.prototype.hasOwnProperty.call(data, "data")) {
    return data.data;
  }
  return data;
}

/* ===== 4. UI HELPERS ===== */
const loading = (t = "Loading…") => `<div class="state"><span class="spinner"></span> ${esc(t)}</div>`;
const empty = t => `<div class="state empty">${esc(t)}</div>`;
const alertBox = (t, type = "info") => `<div class="alert ${type}">${esc(t)}</div>`;
const errBox = e => alertBox(e && e.message ? e.message : String(e), "error");
const head = (title, actions = "", sub = "") => `<div class="page-head"><div><h1>${esc(title)}</h1>${sub ? `<div class="muted">${esc(sub)}</div>` : ""}</div><div class="actions">${actions}</div></div>`;

function dl(pairs) {
  return `<dl class="dl">${pairs.map(([k, v]) => `<dt>${esc(k)}</dt><dd>${esc(v === undefined || v === null || v === "" ? "—" : v)}</dd>`).join("")}</dl>`;
}
function table(cols, rows) {
  return `<div class="table-wrap"><table><thead><tr>${cols.map(c => `<th>${esc(c[0])}</th>`).join("")}</tr></thead><tbody>${
    rows.map(r => `<tr>${cols.map(c => `<td>${c[1](r)}</td>`).join("")}</tr>`).join("")}</tbody></table></div>`;
}
function toast(msg, type = "ok") {
  const t = document.createElement("div");
  t.className = `toast ${type}`;
  t.textContent = msg;
  $("#toasts").append(t);
  setTimeout(() => t.remove(), 5000);
}

// Click handling: onClick(root, { actionName: el => ... }) for elements with data-act="actionName"
function onClick(root, handlers) {
  root.addEventListener("click", e => {
    const el = e.target.closest("[data-act]");
    if (!el || !root.contains(el) || el.disabled) return;
    const fn = handlers[el.dataset.act];
    if (fn) { e.preventDefault(); fn(el); }
  });
}
async function busy(btn, fn) {
  const old = btn.textContent;
  btn.disabled = true; btn.textContent = "Working…";
  try { return await fn(); } finally { btn.disabled = false; btn.textContent = old; }
}
// Runs an API action; the success message is shown ONLY if the request worked.
async function runAction(btn, fn, okMsg) {
  try { const r = await busy(btn, fn); toast(okMsg); return { ok: true, result: r }; }
  catch (e) { toast(e.message, "error"); return { ok: false }; }
}

// Modal
const dlg = $("#modal");
function openModal(title, wide = false) {
  $("#modal-title").textContent = title;
  dlg.classList.toggle("wide", wide);
  const c = document.createElement("div");
  $("#modal-body").replaceChildren(c);
  if (!dlg.open) dlg.showModal();
  return c;
}
const closeModal = () => { if (dlg.open) dlg.close(); };

function field(label, name, o = {}) {
  const { type = "text", value = "", required = false, options = [], hint = "", rows = 3, placeholder = "" } = o;
  const req = required ? "required" : "";
  if (type === "checkbox") return `<label class="check"><input type="checkbox" name="${name}" ${value ? "checked" : ""}> ${esc(label)}</label>`;
  let input;
  if (type === "select") {
    input = `<select name="${name}" ${req}>${options.map(([v, t]) => `<option value="${esc(v)}" ${String(v) === String(value) ? "selected" : ""}>${esc(t)}</option>`).join("")}</select>`;
  } else if (type === "textarea") {
    input = `<textarea name="${name}" rows="${rows}" ${req} placeholder="${esc(placeholder)}">${esc(value)}</textarea>`;
  } else {
    input = `<input type="${type}" name="${name}" value="${esc(value)}" ${req} placeholder="${esc(placeholder)}" ${type === "password" ? 'autocomplete="new-password"' : ""}>`;
  }
  return `<label class="field"><span>${esc(label)}${required ? " *" : ""}</span>${input}${hint ? `<small>${esc(hint)}</small>` : ""}</label>`;
}
function readForm(form) {
  const o = {};
  for (const el of form.elements) {
    if (!el.name) continue;
    o[el.name] = el.type === "checkbox" ? el.checked : (el.type === "password" ? el.value : el.value.trim());
  }
  return o;
}
// Opens a form in the pop-up. onSubmit(values) must throw on failure; the pop-up stays open and shows the error.
function openForm({ title, fields, submitLabel = "Save", onSubmit, back, afterSave, wide = false }) {
  const c = openModal(title, wide);
  c.innerHTML = `<form class="form" novalidate>${fields}<div class="alert error form-error" hidden></div>
    <div class="actions"><button class="btn" type="submit">${esc(submitLabel)}</button><button class="btn ghost" type="button" data-cancel>Cancel</button></div></form>`;
  const form = c.firstElementChild;
  $("[data-cancel]", form).addEventListener("click", () => (back || closeModal)());
  form.addEventListener("submit", async e => {
    e.preventDefault();
    const box = $(".form-error", form);
    box.hidden = true;
    if (!form.reportValidity()) return;
    const btn = $("button[type=submit]", form);
    try {
      await busy(btn, () => onSubmit(readForm(form)));
      (afterSave || back || closeModal)();
    } catch (err) { box.textContent = err.message; box.hidden = false; }
  });
  return form;
}

/* ===== 5. SHARED PIECES ===== */
async function loadLookups() {
  const jobs = [];
  if (!cache.specialties) jobs.push(api("/api/Specialties").then(d => { cache.specialties = asList(d); }).catch(() => {}));
  if (!cache.doctors) {
    const inactive = hasRole("Admin", "SuperAdmin", "Receptionist") ? true : undefined;
    jobs.push(api("/api/Doctors", { query: { includeInactive: inactive } }).then(d => { cache.doctors = asList(d); }).catch(() => {}));
  }
  await Promise.all(jobs);
}
const specialties = () => cache.specialties || [];
const doctors = () => cache.doctors || [];
const findSpecialty = id => specialties().find(s => String(s.id) === String(id));
const findDoctor = id => doctors().find(d => String(d.id) === String(id));
const isFM = s => !!(s && s.isFamilyMedicine);
const needsReferral = s => !!(s && !s.isFamilyMedicine && s.requiresReferral); // Family Medicine never needs one
const doctorName = d => personName(d, `Doctor #${d.id}`);
const doctorNameById = id => { const d = findDoctor(id); return d ? doctorName(d) : `Doctor #${id}`; };
const specialtyOfDoctor = d => pick(d, "specialtyName", "specialty.name") ?? findSpecialty(d.specialtyId)?.name ?? "—";
const isActiveDoctor = d => activeFlag(d) !== false;

function needPatientId() {
  if (state.patientId == null) throw new Error("Could not read your patient ID from /api/Auth/me. Adjust ME_FIELDS at the top of app.js to match the response.");
  return state.patientId;
}
function needDoctorId() {
  if (state.doctorId == null) throw new Error("Could not find your doctor ID. Adjust ME_FIELDS at the top of app.js to match the /api/Auth/me response.");
  return state.doctorId;
}

/* --- appointments --- */
const apptId = a => a.id ?? a.appointmentId;
const apptStart = a => pick(a, "startTime", "start", "dateTime");
const statusText = a => String(pick(a, "status", "appointmentStatus") ?? "Unknown");
const statusKey = a => statusText(a).toLowerCase().replace(/[\s_-]/g, "");
const badgeClass = k => ({ completed: "ok", approved: "ok", confirmed: "ok", pending: "warn", cancelled: "bad", canceled: "bad", rejected: "bad", noshow: "bad" }[k] || "");
const apptDoctorName = a => pick(a, "doctorName", "doctorFullName") || (a.doctor ? personName(a.doctor, "") : "") || doctorNameById(a.doctorId);
const apptPatientName = a => pick(a, "patientName", "patientFullName") || (a.patient ? personName(a.patient, "") : "") || `Patient #${a.patientId}`;
const sortByStart = (list, dir = 1) => [...list].sort((a, b) => dir * (new Date(apptStart(a)) - new Date(apptStart(b))));
const canCancel = a => !DEAD_STATUSES.includes(statusKey(a)) && !hasStarted(apptStart(a));
const noShowAllowedAt = a => {
  const start = toDate(apptStart(a));
  return start ? new Date(start.getTime() + NO_SHOW_DELAY_MINUTES * 60_000) : null;
};

function apptTable(list, mode) { // mode: patient | doctor | staff | plain
  if (!list.length) return empty("No appointments found.");
  const cols = [["When", a => esc(fmtDateTime(apptStart(a)))]];
  if (mode !== "patient") cols.push(["Patient", a => esc(apptPatientName(a))]);
  if (mode !== "doctor") cols.push(["Doctor", a => esc(apptDoctorName(a))]);
  cols.push(["Status", a => `<span class="badge ${badgeClass(statusKey(a))}">${esc(statusText(a))}</span>`]);
  if (mode !== "plain") cols.push(["", a => apptActions(a, mode)]);
  return table(cols, list);
}
function apptActions(a, mode) {
  const d = `data-id="${esc(apptId(a))}" data-start="${esc(apptStart(a))}" data-pid="${esc(a.patientId)}"`;
  const key = statusKey(a), started = hasStarted(apptStart(a)), done = key === "completed";
  const out = [];
  if (mode === "patient") {
    if (canCancel(a)) out.push(`<button class="btn small danger ghost" data-act="cancel" ${d}>Cancel</button>`);
    if (done) out.push(`<button class="btn small ghost" data-act="consult" ${d}>View consultation</button>`);
  } else {
    out.push(`<button class="btn small ghost" data-act="patient" ${d}>Patient</button>`);
    if (mode === "doctor") {
      if (done) out.push(`<button class="btn small" data-act="consult" ${d}>View / edit consultation</button>`);
      else if (!DEAD_STATUSES.includes(key)) {
        out.push(started
          ? `<button class="btn small" data-act="consult" ${d}>Record consultation</button>`
          : `<button class="btn small" disabled title="Available from the scheduled start time">Record consultation</button>`);
        if (started) {
          const allowedAt = noShowAllowedAt(a);
          if (allowedAt && new Date() >= allowedAt) {
            out.push(`<button class="btn small danger ghost" data-act="noshow" ${d}>No-show</button>`);
          } else {
            const label = allowedAt ? "No-show available at " + fmtDateTime(allowedAt) : "No-show unavailable";
            out.push('<button class="btn small danger ghost" disabled>' + esc(label) + '</button>');
          }
        }
      }
    }
  }
  return out.join("") || "—";
}
// Shared click handlers for appointment tables
function apptHandlers(reload, mode) {
  return {
    cancel: async el => {
      if (!confirm("Cancel this appointment?")) return;
      const r = await runAction(el, () => api(`/api/appointments/${el.dataset.id}/cancel`, { method: "POST" }), "Appointment cancelled.");
      if (r.ok) reload();
    },
    noshow: async el => {
      if (!confirm("Mark this patient as a no-show?")) return;
      const r = await runAction(el, () => api(`/api/appointments/${el.dataset.id}/no-show`, { method: "POST" }), "Marked as no-show.");
      if (r.ok) reload();
    },
    consult: el => openConsultation(el.dataset.id, el.dataset.start, { canWrite: mode === "doctor", afterSave: () => { closeModal(); reload(); } }),
    patient: el => openPatientDetail(el.dataset.pid, reload)
  };
}

/* --- consultation pop-up --- */
async function openConsultation(appointmentId, start, { canWrite, afterSave, back }) {
  const c = openModal("Consultation");
  c.innerHTML = loading();
  let existing = null;
  try { existing = await api(`/api/appointments/${appointmentId}/consultation`); }
  catch (e) {
    if (e.status !== 404) { c.innerHTML = errBox(e); return; }
  }
  if (!existing) {
    if (!canWrite) { c.innerHTML = empty("No consultation has been recorded for this visit."); return; }
    if (!hasStarted(start)) { c.innerHTML = alertBox(`A consultation can be recorded only after the scheduled start time (${fmtDateTime(start)}).`, "warn"); return; }
  }
  const diag = existing ? (pick(existing, "diagnosis") ?? "") : "", presc = existing ? (pick(existing, "prescription") ?? "") : "";
  if (!canWrite) { c.innerHTML = dl([["Diagnosis", diag], ["Prescription", presc]]); return; }
  openForm({
    title: existing ? "Edit consultation" : "Record consultation",
    fields: field("Diagnosis", "diagnosis", { type: "textarea", value: diag, required: true, rows: 4 }) + field("Prescription", "prescription", { type: "textarea", value: presc, rows: 4 }),
    submitLabel: existing ? "Save changes" : "Record consultation",
    back, afterSave,
    onSubmit: async v => {
      if (!existing && !hasStarted(start)) throw new Error("This appointment has not started yet, so a consultation cannot be recorded.");
      await api(`/api/appointments/${appointmentId}/consultation`, { method: existing ? "PUT" : "POST", body: { diagnosis: v.diagnosis, prescription: v.prescription } });
      toast(existing ? "Consultation updated." : "Consultation recorded.");
    }
  });
}

/* --- referrals & consultation lists --- */
function referralCards(list) {
  if (!list.length) return empty("No referrals found.");
  return list.map(r => {
    const spec = pick(r, "toSpecialtyName", "toSpecialty.name") ?? findSpecialty(pick(r, "toSpecialtyId", "specialtyId"))?.name ?? "Specialist";
    const from = pick(r, "fromDoctorName") ?? (r.fromDoctorId ? doctorNameById(r.fromDoctorId) : "—");
    const until = pick(r, "expiresAt", "validUntil", "expirationDate", "expiryDate");
    const used = pick(r, "isUsed", "used") === true || !!pick(r, "usedAt", "usedOn");
    const expired = until && toDate(until) && toDate(until) < new Date();
    const [label, cls] = used ? ["Used", ""] : expired ? ["Expired", "bad"] : ["Active", "ok"];
    return `<div class="card"><div class="page-head"><h3>Referral to ${esc(spec)}</h3><span class="badge ${cls}">${label}</span></div>${
      dl([["Reason", pick(r, "reason")], ["Issued by", from], ["Issued", fmtDate(pick(r, "createdAt", "issuedAt", "issueDate", "date"))], ["Valid until", until ? fmtDate(until) : "—"]])}</div>`;
  }).join("");
}
function consultationCards(list) {
  if (!list.length) return empty("No consultations recorded yet.");
  return list.map(c => {
    const when = pick(c, "createdAt", "date", "consultationDate", "appointment.startTime", "startTime");
    const doc = pick(c, "doctorName") ?? (c.doctorId ? doctorNameById(c.doctorId) : "—");
    return `<div class="card"><h3>${esc(fmtDateTime(when))}</h3>${dl([["Doctor", doc], ["Diagnosis", pick(c, "diagnosis")], ["Prescription", pick(c, "prescription")]])}</div>`;
  }).join("");
}

/* --- medical profile: read-only for patients, editable for doctor/admin --- */
async function loadMedicalProfile(el, pid, canEdit, reopen) {
  el.innerHTML = loading();
  let p = null;
  try { p = await api(`/api/patients/${pid}/medical-profile`); }
  catch (e) { if (e.status !== 404) { el.innerHTML = errBox(e); return; } }
  let html = p
    ? dl([["Blood type", pick(p, "bloodType")], ["Allergies", pick(p, "allergies")], ["Chronic conditions", pick(p, "chronicConditions")]])
    : empty("No medical profile has been created yet.");
  if (canEdit) html += `<button class="btn small" data-act="editprofile">${p ? "Edit" : "Create"} medical profile</button>`;
  el.innerHTML = html;
  if (canEdit) onClick(el, { editprofile: () => openForm({
    title: p ? "Edit medical profile" : "Create medical profile",
    fields: field("Blood type", "bloodType", { value: p ? pick(p, "bloodType") ?? "" : "", placeholder: "e.g. O+" }) +
      field("Allergies", "allergies", { type: "textarea", value: p ? pick(p, "allergies") ?? "" : "" }) +
      field("Chronic conditions", "chronicConditions", { type: "textarea", value: p ? pick(p, "chronicConditions") ?? "" : "" }),
    back: reopen,
    onSubmit: async v => {
      await api(`/api/patients/${pid}/medical-profile`, { method: p ? "PUT" : "POST", body: { bloodType: v.bloodType, allergies: v.allergies, chronicConditions: v.chronicConditions } });
      toast("Medical profile saved.");
    }
  }) });
}

/* --- patient detail pop-up (used by doctor, receptionist, admin) --- */
async function openPatientDetail(pid, onChange) {
  const c = openModal("Patient details", true);
  const front = hasRole("Receptionist", "Admin", "SuperAdmin");
  const clinical = hasRole("Doctor", "Admin", "SuperAdmin");
  const reopen = () => { openPatientDetail(pid, onChange); if (onChange) onChange(); };
  c.innerHTML = `<section><div id="pd-info">${loading()}</div></section>
    <section><h3>Appointments</h3><div id="pd-appts">${loading()}</div></section>
    <section><h3>Referrals</h3><div id="pd-refs">${loading()}</div></section>
    ${clinical ? `<section><h3>Consultation history</h3><div id="pd-cons">${loading()}</div></section>
    <section><h3>Medical profile</h3><div id="pd-prof"></div></section>` : ""}`;
  const patientP = api(`/api/Patients/${pid}`);
  patientP.catch(() => {});
  await loadLookups();

  // info + family doctor + account
  (async () => {
    const el = $("#pd-info", c);
    try {
      const p = await patientP;
      const fd = pick(p, "familyDoctorId", "familyDoctor.id");
      let html = `<h2>${esc(personName(p, "Patient #" + pid))}</h2>` + dl([
        ["Personal number", pick(p, "personalNumber")], ["Phone", pick(p, "phone")], ["Birth date", fmtDate(pick(p, "birthDate"))],
        ["Contact email", pick(p, "contactEmail", "email")], ["Family doctor", fd ? doctorNameById(fd) : "None assigned"]]);
      if (front) {
        const fams = doctors().filter(d => isActiveDoctor(d) && isFM(findSpecialty(d.specialtyId)));
        const list = fams;
        html += `<div class="toolbar"><label class="field"><span>Family doctor</span><select id="fd-select"><option value="">— choose a doctor —</option>${
          list.map(d => `<option value="${esc(d.id)}" ${String(d.id) === String(fd) ? "selected" : ""}>${esc(doctorName(d))} (${esc(specialtyOfDoctor(d))})</option>`).join("")}</select></label>
          <button class="btn small" data-act="assignfd">Assign</button>
          <button class="btn small danger ghost" data-act="removefd" ${fd ? "" : "disabled"}>Remove</button></div>
          <div class="actions"><button class="btn small ghost" data-act="editpatient">Edit details</button>
          ${p.hasAccount ? '<button class="btn small ghost" disabled>Login account exists</button>' : '<button class="btn small ghost" data-act="account">Create login account</button>'}</div>`;
        if (!list.length) html += '<p class="muted">No active Family Medicine doctors are available to assign.</p>';
      }
      el.innerHTML = html;
      onClick(el, {
        assignfd: async b => {
          if (!list.length) { toast("There are no active Family Medicine doctors to assign.", "warn"); return; }
          const v = toInt($("#fd-select", el).value);
          if (v === null) { toast("Choose a doctor first.", "warn"); return; }
          const r = await runAction(b, () => api(`/api/Patients/${pid}/family-doctor`, { method: "PUT", body: { doctorId: v } }), "Family doctor assigned.");
          if (r.ok) reopen();
        },
        removefd: async b => {
          if (!confirm("Remove this patient's family doctor?")) return;
          const r = await runAction(b, () => api(`/api/Patients/${pid}/family-doctor`, { method: "PUT", body: { doctorId: null } }), "Family doctor removed.");
          if (r.ok) reopen();
        },
        editpatient: () => openForm({
          title: "Edit patient", back: reopen,
          fields: field("First name", "firstName", { value: p.firstName ?? "", required: true }) + field("Last name", "lastName", { value: p.lastName ?? "", required: true }) +
            field("Phone", "phone", { value: p.phone ?? "", required: true }) + field("Contact email", "contactEmail", { type: "email", value: p.contactEmail ?? "" }) + field("Birth date", "birthDate", { type: "date", value: String(p.birthDate ?? "").slice(0, 10), required: true }),
          onSubmit: async v => { await api(`/api/Patients/${pid}`, { method: "PUT", body: { firstName: v.firstName, lastName: v.lastName, phone: v.phone, contactEmail: v.contactEmail, birthDate: v.birthDate } }); toast("Patient updated."); }
        }),
        account: () => openForm({
          title: "Create login account", back: reopen, submitLabel: "Create account",
          fields: `<p class="muted">Only for patients who do not have an account yet.</p>` + field("Email", "email", { type: "email", value: p.contactEmail ?? p.email ?? "", required: true }) + field("Password", "password", { type: "password", required: true }),
          onSubmit: async v => { await api(`/api/Patients/${pid}/account`, { method: "POST", body: { email: v.email, password: v.password } }); toast("Account created."); }
        })
      });
    } catch (e) { el.innerHTML = errBox(e); }
  })();

  // appointments
  api(`/api/patients/${pid}/appointments`).then(d => { $("#pd-appts", c).innerHTML = apptTable(sortByStart(asList(d), -1), "plain"); })
    .catch(e => { $("#pd-appts", c).innerHTML = errBox(e); });

  // Referrals may be issued by the family doctor or by a doctor who completed
  // a consultation with this patient. Patients themselves cannot issue them.
  (async () => {
    const el = $("#pd-refs", c);
    try {
      const list = asList(await api(`/api/patients/${pid}/referrals`));
      let canIssue = false;
      if (hasRole("Doctor")) {
        const p = await patientP.catch(() => null);
        const fd = p ? pick(p, "familyDoctorId", "familyDoctor.id") : undefined;
        const isFamilyDoctor = !!p && fd !== undefined && fd !== null && state.doctorId !== null
          && String(fd) === String(state.doctorId);
        let hasCompletedConsultation = false;
        if (p && state.doctorId !== null && !isFamilyDoctor) {
          try {
            const consultations = asList(await api(`/api/patients/${pid}/consultations`));
            hasCompletedConsultation = consultations.some(x => String(x.doctorId) === String(state.doctorId));
          } catch { /* the API remains the final authority for issuing a referral */ }
        }
        canIssue = isFamilyDoctor || hasCompletedConsultation;
      }
      el.innerHTML = referralCards(list) + (canIssue
        ? `<button class="btn small" data-act="issue">Issue referral</button>`
        : (hasRole("Doctor")
          ? `<p class="muted">The family doctor, or a doctor who has completed a consultation with this patient, can issue a referral.</p>`
          : ""));
      onClick(el, { issue: () => {
        const targets = specialties().filter(s => !isFM(s));
        openForm({
          title: "Issue referral", back: reopen, submitLabel: "Issue referral",
          fields: field("Specialty", "toSpecialtyId", { type: "select", required: true, options: [["", "— choose —"], ...targets.map(s => [s.id, s.name])] }) +
            field("Reason", "reason", { type: "textarea", required: true }) + field("Valid for (days)", "validForDays", { type: "number", hint: "Optional" }),
          onSubmit: async v => {
            await api(`/api/patients/${pid}/referrals`, { method: "POST", body: { fromDoctorId: needDoctorId(), toSpecialtyId: toInt(v.toSpecialtyId), reason: v.reason, validForDays: toInt(v.validForDays) } });
            toast("Referral issued.");
          }
        });
      } });
    } catch (e) { el.innerHTML = errBox(e); }
  })();

  if (clinical) {
    api(`/api/patients/${pid}/consultations`).then(d => { $("#pd-cons", c).innerHTML = consultationCards(asList(d)); })
      .catch(e => { $("#pd-cons", c).innerHTML = errBox(e); });
    loadMedicalProfile($("#pd-prof", c), pid, true, reopen);
  }
}

/* --- simple dashboard shell shared by all roles --- */
function dashboardShell(box, intro, extraHtml = "") {
  const links = state.nav.filter(n => !["dashboard", "account"].includes(n.id));
  box.innerHTML = head(`Welcome, ${userDisplayName()}`, "", intro) + extraHtml +
    `<div class="cards">${links.map(n => `<a class="card link-card" href="#${n.id}"><strong>${esc(n.label)}</strong><span class="muted">${esc(n.hint || "Open")}</span></a>`).join("")}</div><div id="live"></div>`;
}
function userDisplayName() {
  const m = state.me || {};
  return pick(m, ...ME_FIELDS.name) ?? (`${m.firstName || ""} ${m.lastName || ""}`.trim() || pick(m, ...ME_FIELDS.email) || "user");
}

async function accountPage(box) {
  const m = state.me || {};
  box.innerHTML = head("Account") + `<div class="card">${dl([["Name", userDisplayName()], ["Email", pick(m, ...ME_FIELDS.email)], ["Role", state.role]])}</div>
    <div class="card"><h3>Change password</h3><form class="form" id="pw-form" style="max-width:420px">${
      field("Current password", "currentPassword", { type: "password", required: true }) + field("New password", "newPassword", { type: "password", required: true }) +
      field("Repeat new password", "repeat", { type: "password", required: true })}<div id="pw-msg"></div><button class="btn" type="submit">Change password</button></form></div>`;
  const form = $("#pw-form", box), msg = $("#pw-msg", box);
  form.addEventListener("submit", async e => {
    e.preventDefault(); msg.innerHTML = "";
    const v = readForm(form);
    if (v.newPassword !== v.repeat) { msg.innerHTML = alertBox("The new passwords do not match.", "error"); return; }
    try {
      await busy($("button", form), () => api("/api/Auth/change-password", { method: "PUT", body: { currentPassword: v.currentPassword, newPassword: v.newPassword } }));
      msg.innerHTML = alertBox("Password changed.", "ok"); form.reset();
    } catch (err) { msg.innerHTML = errBox(err); }
  });
}

/* ===== 6. PATIENT PAGES ===== */
async function patientDashboard(box) {
  dashboardShell(box, "Book visits and see your health records.");
  const live = $("#live", box);
  live.innerHTML = `<h3>Upcoming appointments</h3>` + loading();
  try {
    const list = sortByStart(asList(await api(`/api/patients/${needPatientId()}/appointments`))).filter(a => hasStarted(apptStart(a)) === false && !DEAD_STATUSES.includes(statusKey(a))).slice(0, 5);
    await loadLookups();
    live.innerHTML = `<h3>Upcoming appointments</h3>` + (list.length ? apptTable(list, "plain") : empty("You have no upcoming appointments."));
  } catch (e) { live.innerHTML = errBox(e); }
}

async function browseDoctors(box) {
  box.innerHTML = head("Doctors & specialties") + loading();
  await loadLookups();
  if (!cache.doctors || !cache.specialties) { box.innerHTML = head("Doctors & specialties") + alertBox("Could not load doctors. Please try again.", "error"); return; }
  const draw = filter => {
    const list = doctors().filter(d => isActiveDoctor(d) && (!filter || String(d.specialtyId) === String(filter)));
    $("#doc-list", box).innerHTML = list.length ? list.map(d => `<div class="card"><h3>${esc(doctorName(d))}</h3><p class="muted">${esc(specialtyOfDoctor(d))}</p>
      <button class="btn small" data-act="book" data-id="${esc(d.id)}">Book a visit</button></div>`).join("") : empty("No doctors found.");
  };
  box.innerHTML = head("Doctors & specialties") + `<div class="card"><h3>Specialties</h3><div class="actions">${
    specialties().map(s => `<span class="badge">${esc(s.name)}${isFM(s) ? " · no referral needed" : needsReferral(s) ? " · referral required" : ""}</span>`).join("") || "<span class='muted'>None</span>"}</div></div>
    <div class="toolbar"><label class="field"><span>Filter by specialty</span><select id="sp-filter"><option value="">All specialties</option>${specialties().map(s => `<option value="${esc(s.id)}">${esc(s.name)}</option>`).join("")}</select></label></div>
    <div id="doc-list" class="cards"></div>`;
  draw("");
  $("#sp-filter", box).addEventListener("change", e => draw(e.target.value));
  onClick(box, { book: el => goTo("book", { bookDoctorId: el.dataset.id }) });
}

async function bookVisit(box) {
  box.innerHTML = head("Book a visit") + loading();
  const pid = needPatientId();
  const flash = state.flash; state.flash = {};
  await loadLookups();
  let patient = null, refs = [], refsErr = null;
  const results = await Promise.allSettled([api(`/api/Patients/${pid}`), api(`/api/patients/${pid}/referrals`, { query: { activeOnly: true } })]);
  if (results[0].status === "fulfilled") patient = results[0].value;
  if (results[1].status === "fulfilled") refs = asList(results[1].value); else refsErr = results[1].reason;
  if (!cache.specialties || !cache.doctors) { box.innerHTML = head("Book a visit") + alertBox("Could not load doctors and specialties. Please try again.", "error"); return; }

  const hasFamily = patient ? !!pick(patient, "familyDoctorId", "familyDoctor.id") : null;
  let selSpec = "", selDoc = "", selDate = todayStr(), selSlot = "";
  const pre = flash.bookDoctorId ? findDoctor(flash.bookDoctorId) : null;
  if (pre) { selSpec = String(pre.specialtyId); selDoc = String(pre.id); }

  box.innerHTML = head("Book a visit", "", "Your booking is confirmed immediately.") + `
    ${hasFamily === false ? alertBox("You don't have a family doctor yet. You can book a Family Medicine doctor to ask for a referral. This does not make that doctor your family doctor.", "info") : ""}
    <div class="card"><div class="grid2">
      <label class="field"><span>Specialty</span><select id="b-spec"><option value="">— choose —</option>${specialties().map(s => `<option value="${esc(s.id)}">${esc(s.name)}</option>`).join("")}</select></label>
      <label class="field"><span>Doctor</span><select id="b-doc"></select></label>
      <label class="field"><span>Date</span><input type="date" id="b-date" min="${todayStr()}" value="${selDate}"></label>
      <div class="field"><span>&nbsp;</span><button class="btn" data-act="times">Show available times</button></div>
    </div><div id="b-ref"></div></div>
    <div id="b-result"></div><div id="b-slots"></div>`;
  const specEl = $("#b-spec", box), docEl = $("#b-doc", box), dateEl = $("#b-date", box);
  specEl.value = selSpec;

  const clearSlots = () => { selSlot = ""; $("#b-slots", box).innerHTML = ""; };
  const matchingRefs = () => refs.filter(r => String(pick(r, "toSpecialtyId", "specialtyId", "toSpecialty.id")) === String(selSpec));
  const blocked = () => { const s = findSpecialty(selSpec); return needsReferral(s) && (!!refsErr || !matchingRefs().length); };

  function drawDoctors() {
    const list = doctors().filter(d => isActiveDoctor(d) && String(d.specialtyId) === String(selSpec));
    docEl.innerHTML = `<option value="">${selSpec ? "— choose —" : "Choose a specialty first"}</option>` + list.map(d => `<option value="${esc(d.id)}">${esc(doctorName(d))}</option>`).join("");
    docEl.value = list.some(d => String(d.id) === selDoc) ? selDoc : "";
    selDoc = docEl.value;
  }
  function drawReferral() {
    const s = findSpecialty(selSpec), el = $("#b-ref", box);
    if (!s) { el.innerHTML = ""; return; }
    if (isFM(s)) { el.innerHTML = alertBox("Family Medicine visits do not need a referral.", "info"); return; }
    if (!needsReferral(s)) { el.innerHTML = ""; return; }
    if (refsErr) { el.innerHTML = alertBox(`This specialty needs a referral, but your referrals could not be loaded: ${refsErr.message}`, "error"); return; }
    const m = matchingRefs();
    if (!m.length) { el.innerHTML = alertBox("This specialty requires a referral and you have no active referral for it. Book a Family Medicine visit first to request one.", "warn"); return; }
    el.innerHTML = `<label class="field"><span>Referral to use</span><select id="b-refsel">${m.map(r => {
      const until = pick(r, "expiresAt", "validUntil", "expirationDate", "expiryDate");
      return `<option value="${esc(r.id)}">Referral #${esc(r.id)}${pick(r, "reason") ? " – " + esc(pick(r, "reason")) : ""}${until ? " (valid until " + esc(fmtDate(until)) + ")" : ""}</option>`;
    }).join("")}</select></label>`;
  }
  async function loadSlots() {
    const out = $("#b-slots", box);
    clearSlots();
    if (!selSpec || !selDoc || !selDate) { out.innerHTML = alertBox("Choose a specialty, a doctor and a date.", "warn"); return; }
    if (blocked()) { out.innerHTML = alertBox("You need an active referral for this specialty before you can book.", "warn"); return; }
    out.innerHTML = loading("Loading free times…");
    try {
      const slots = asList(await api(`/api/doctors/${selDoc}/available-slots`, { query: { date: selDate } }))
        .map(s => { let v = typeof s === "string" ? s : pick(s, "startTime", "start", "dateTime", "time"); if (v && /^\d{2}:\d{2}/.test(v)) v = `${selDate}T${v}`; return v; }).filter(Boolean);
      out.innerHTML = slots.length
        ? `<div class="card"><h3>Free times on ${esc(fmtDate(selDate))}</h3><div class="slots">${slots.map(v => `<button class="slot" data-act="slot" data-v="${esc(v)}">${esc(fmtTime(v))}</button>`).join("")}</div><div id="b-confirm"></div></div>`
        : empty("No free times on this date. Try another day.");
    } catch (e) { out.innerHTML = errBox(e); }
  }

  drawDoctors(); drawReferral();
  specEl.addEventListener("change", () => { selSpec = specEl.value; selDoc = ""; drawDoctors(); drawReferral(); clearSlots(); });
  docEl.addEventListener("change", () => { selDoc = docEl.value; clearSlots(); });
  dateEl.addEventListener("change", () => { selDate = dateEl.value; clearSlots(); });

  onClick(box, {
    times: () => loadSlots(),
    slot: el => {
      selSlot = el.dataset.v;
      box.querySelectorAll(".slot").forEach(b => b.classList.toggle("selected", b === el));
      $("#b-confirm", box).innerHTML = `<p>Book <strong>${esc(doctorNameById(selDoc))}</strong> on <strong>${esc(fmtDateTime(selSlot))}</strong>?</p><button class="btn" data-act="confirm">Confirm booking</button>`;
    },
    confirm: async el => {
      const refSel = $("#b-refsel", box);
      const body = { patientId: pid, doctorId: Number(selDoc), startTime: selSlot, referralId: needsReferral(findSpecialty(selSpec)) && refSel ? Number(refSel.value) : null };
      try {
        const res = await busy(el, () => api("/api/appointments", { method: "POST", body }));
        const st = res && typeof res === "object" ? pick(res, "status", "appointmentStatus") : undefined;
        toast("Appointment booked.");
        $("#b-result", box).innerHTML = alertBox(`Booked for ${fmtDateTime(body.startTime)}.${st !== undefined ? " Status from the clinic system: " + st + "." : ""} You can see it under "My appointments".`, "ok");
        try { refs = asList(await api(`/api/patients/${pid}/referrals`, { query: { activeOnly: true } })); refsErr = null; } catch (e) { refsErr = e; }
        drawReferral(); loadSlots();
      } catch (e) { toast(e.message, "error"); $("#b-result", box).innerHTML = errBox(e); }
    }
  });
  if (pre) loadSlots();
}

async function patientAppointments(box) {
  const pid = needPatientId();
  const load = async () => {
    box.innerHTML = head("My appointments") + loading();
    try {
      await loadLookups();
      const list = sortByStart(asList(await api(`/api/patients/${pid}/appointments`)), -1);
      box.innerHTML = head("My appointments", `<button class="btn small" data-act="reload">Refresh</button>`) + apptTable(list, "patient");
    } catch (e) { box.innerHTML = head("My appointments") + errBox(e); }
  };
  onClick(box, { ...apptHandlers(load, "patient"), reload: load });
  await load();
}

async function patientReferrals(box) {
  const pid = needPatientId();
  let activeOnly = false;
  const load = async () => {
    box.innerHTML = head("My referrals", `<label class="check"><input type="checkbox" id="act-only" ${activeOnly ? "checked" : ""}> Active only</label>`) + loading();
    $("#act-only", box).addEventListener("change", e => { activeOnly = e.target.checked; load(); });
    try {
      await loadLookups();
      const list = asList(await api(`/api/patients/${pid}/referrals`, { query: { activeOnly: activeOnly ? true : undefined } }));
      $(".state", box)?.remove();
      box.insertAdjacentHTML("beforeend", referralCards(list));
    } catch (e) { $(".state", box)?.remove(); box.insertAdjacentHTML("beforeend", errBox(e)); }
  };
  await load();
}

async function patientHistory(box) {
  box.innerHTML = head("Consultation history") + loading();
  try {
    await loadLookups();
    box.innerHTML = head("Consultation history") + consultationCards(asList(await api(`/api/patients/${needPatientId()}/consultations`)));
  } catch (e) { box.innerHTML = head("Consultation history") + errBox(e); }
}

async function patientProfile(box) {
  box.innerHTML = head("My medical profile", "", "You can view this information. Only your doctor can change it.") + `<div class="card" id="prof"></div>`;
  await loadMedicalProfile($("#prof", box), needPatientId(), false);
}

/* ===== 7. DOCTOR PAGES ===== */
async function doctorDashboard(box) {
  dashboardShell(box, "Your day at a glance.");
  const live = $("#live", box);
  live.innerHTML = `<h3>Today's appointments</h3>` + loading();
  try {
    await loadLookups();
    const list = sortByStart(asList(await api(`/api/doctors/${needDoctorId()}/appointments`, { query: { date: todayStr() } })));
    live.innerHTML = `<h3>Today's appointments</h3>` + apptTable(list, "plain");
  } catch (e) { live.innerHTML = errBox(e); }
}

async function doctorAppointments(box) {
  const did = needDoctorId();
  let date = todayStr();
  box.innerHTML = head("Appointments") + `<div class="toolbar"><label class="field"><span>Date</span><input type="date" id="d-date" value="${date}"></label>
    <button class="btn small ghost" data-act="all">Show all dates</button><button class="btn small" data-act="reload">Refresh</button></div>
    <p class="muted">A consultation can be recorded only after the appointment's start time.</p><div id="d-list"></div>`;
  const load = async () => {
    const out = $("#d-list", box);
    out.innerHTML = loading();
    try {
      await loadLookups();
      const list = sortByStart(asList(await api(`/api/doctors/${did}/appointments`, { query: { date: date || undefined } })));
      out.innerHTML = apptTable(list, "doctor");
    } catch (e) { out.innerHTML = errBox(e); }
  };
  $("#d-date", box).addEventListener("change", e => { date = e.target.value; load(); });
  onClick(box, { ...apptHandlers(load, "doctor"), reload: load, all: () => { date = ""; $("#d-date", box).value = ""; load(); } });
  await load();
}

function scheduleTable(list, manage) {
  if (!list.length) return empty("No working hours have been set.");
  const sorted = [...list].sort((a, b) => DAYS.indexOf(dayName(a.dayOfWeek)) - DAYS.indexOf(dayName(b.dayOfWeek)) || String(a.startTime).localeCompare(String(b.startTime)));
  const cols = [["Day", s => esc(dayName(s.dayOfWeek))], ["From", s => esc(String(s.startTime).slice(0, 5))], ["To", s => esc(String(s.endTime).slice(0, 5))]];
  if (manage) cols.push(["", s => `<button class="btn small ghost" data-act="editsch" data-id="${esc(s.id)}">Edit</button><button class="btn small danger ghost" data-act="delsch" data-id="${esc(s.id)}">Delete</button>`]);
  return table(cols, sorted);
}

async function doctorSchedule(box) {
  box.innerHTML = head("My schedule") + loading();
  try { box.innerHTML = head("My schedule", "", "Your weekly working hours. Ask an administrator to change them.") + scheduleTable(asList(await api(`/api/doctors/${needDoctorId()}/schedules`)), false); }
  catch (e) { box.innerHTML = head("My schedule") + errBox(e); }
}

async function patientsPage(box) { // used by doctor (own patients), receptionist and admin
  const doctorMode = hasRole("Doctor");
  const canRegister = hasRole("Receptionist", "Admin", "SuperAdmin");
  let all = [], famFilter = "";
  await loadLookups();
  const famOptions = doctors().filter(d => isActiveDoctor(d) && isFM(findSpecialty(d.specialtyId)));
  box.innerHTML = head(doctorMode ? "My patients" : "Patients", canRegister ? `<button class="btn" data-act="register">Register patient</button>` : "",
    doctorMode ? "Patients who have you as their family doctor." : "") +
    `<div class="toolbar"><label class="field"><span>Search</span><input id="p-search" placeholder="Name, personal number or phone"></label>
    ${doctorMode ? "" : `<label class="field"><span>Family doctor</span><select id="p-fam"><option value="">All patients</option>${famOptions.map(d => `<option value="${esc(d.id)}">${esc(doctorName(d))}</option>`).join("")}</select></label>`}</div><div id="p-list"></div>`;
  const draw = () => {
    const q = $("#p-search", box).value.trim().toLowerCase();
    const list = all.filter(p => !q || [personName(p, ""), p.personalNumber, p.phone, p.contactEmail, p.email].join(" ").toLowerCase().includes(q));
    $("#p-list", box).innerHTML = list.length ? table([
      ["Name", p => esc(personName(p, "Patient #" + p.id))], ["Personal no.", p => esc(p.personalNumber ?? "—")], ["Phone", p => esc(p.phone ?? "—")],
      ["Contact email", p => esc(p.contactEmail ?? p.email ?? "—")],
      ["Birth date", p => esc(fmtDate(p.birthDate))],
      ["Family doctor", p => { const f = pick(p, "familyDoctorId", "familyDoctor.id"); return esc(f ? doctorNameById(f) : "—"); }],
      ["", p => `<button class="btn small" data-act="open" data-id="${esc(p.id)}">Open</button>`]], list) : empty(all.length ? "No patients match your search." : "No patients found.");
  };
  const load = async () => {
    $("#p-list", box).innerHTML = loading();
    try {
      all = asList(await api("/api/Patients", { query: { familyDoctorId: doctorMode ? needDoctorId() : (famFilter || undefined) } }));
      draw();
    } catch (e) { $("#p-list", box).innerHTML = errBox(e); }
  };
  $("#p-search", box).addEventListener("input", draw);
  $("#p-fam", box)?.addEventListener("change", e => { famFilter = e.target.value; load(); });
  onClick(box, { open: el => openPatientDetail(el.dataset.id, load), register: () => openRegisterPatient(load) });
  await load();
}

function openRegisterPatient(onDone) {
  openForm({
    title: "Register patient", submitLabel: "Register patient",
    fields: `<div class="grid2">${field("First name", "firstName", { required: true }) + field("Last name", "lastName", { required: true }) +
      field("Personal number", "personalNumber", { required: true }) + field("Phone", "phone", { type: "tel", required: true }) + field("Contact email", "contactEmail", { type: "email" }) +
      field("Birth date", "birthDate", { type: "date", required: true })}</div>
      <h3>Login account (optional)</h3><p class="muted">Fill in both fields to also create a login account. You can also do this later from the patient's page.</p>
      <div class="grid2">${field("Login email", "email", { type: "email" }) + field("Password", "password", { type: "password" })}</div>`,
    onSubmit: async v => {
      if (!!v.email !== !!v.password) throw new Error("Fill in both email and password, or leave both empty.");
      const body = { firstName: v.firstName, lastName: v.lastName, personalNumber: v.personalNumber, phone: v.phone, contactEmail: v.contactEmail, birthDate: v.birthDate, familyDoctorId: null };
      if (v.email) { body.email = v.email; body.password = v.password; }
      await api("/api/Patients", { method: "POST", body });
      toast("Patient registered.");
    },
    afterSave: () => { closeModal(); if (onDone) onDone(); }
  });
}

/* ===== 8. RECEPTIONIST & ADMIN PAGES ===== */
async function staffDashboard(box) {
  const adminRole = isAdmin();
  dashboardShell(box, adminRole ? "Manage the clinic." : "Register patients and manage family doctors.",
    hasRole("Receptionist") ? `<div class="actions" style="margin-bottom:1rem"><button class="btn" data-act="register">Register a new patient</button></div>` : "");
  onClick(box, { register: () => openRegisterPatient(() => toast("Open “Patients” to see the new patient.", "ok")), reminders: async el => {
    if (!confirm("Send appointment reminders now?")) return;
    await runAction(el, () => api("/api/Reminders/run", { method: "POST" }), "Reminder check completed.");
  } });
  const live = $("#live", box);
  const jobs = [["Doctors", "/api/Doctors"], ["Specialties", "/api/Specialties"], ["Patients", "/api/Patients"]];
  if (adminRole) jobs.push(["Receptionists", "/api/staff/receptionists"]);
  if (hasRole("SuperAdmin")) jobs.push(["Administrators", "/api/staff/admins"]);
  live.innerHTML = loading();
  const res = await Promise.allSettled(jobs.map(j => api(j[1], { query: j[0] === "Doctors" ? { includeInactive: true } : undefined })));
  const currentUserId = String(pick(state.me, ...ME_FIELDS.userId) ?? "");
  const currentEmail = String(pick(state.me, ...ME_FIELDS.email) ?? "").toLowerCase();
  live.innerHTML = `<div class="cards">${jobs.map((j, i) => {
    let count = res[i].status === "fulfilled" ? asList(res[i].value).length : "—";
    if (j[0] === "Administrators" && state.role === "SuperAdmin" && count !== "—") {
      count = asList(res[i].value).filter(u =>
        String(u.userId ?? u.id) !== currentUserId && String(u.email ?? "").toLowerCase() !== currentEmail).length;
    }
    return `<div class="card"><div class="muted">${esc(j[0])}</div><div class="stat">${count}</div></div>`;
  }).join("")}</div>` +
    (adminRole ? `<button class="btn ghost" data-act="reminders">Send reminders now</button>` : "");
}

async function doctorsPage(box) {
  const manage = isAdmin();
  const load = async () => {
    cache.doctors = null;
    box.innerHTML = head("Doctors", manage ? `<button class="btn" data-act="add">Add doctor</button>` : "") + loading();
    await loadLookups();
    const hdr = head("Doctors", manage ? `<button class="btn" data-act="add">Add doctor</button>` : "");
    if (!cache.doctors) { box.innerHTML = hdr + alertBox("Could not load doctors.", "error"); return; }
    const cols = [["Name", d => esc(doctorName(d))], ["Specialty", d => esc(specialtyOfDoctor(d))], ["Phone", d => esc(d.phone ?? "—")], ["Email", d => esc(d.email ?? "—")],
      ["Status", d => { const a = activeFlag(d); return a === undefined ? "—" : `<span class="badge ${a ? "ok" : "bad"}">${a ? "Active" : "Inactive"}</span>`; }]];
    if (manage) cols.push(["", d => { const a = activeFlag(d);
      return `<button class="btn small ghost" data-act="edit" data-id="${esc(d.id)}">Edit</button><button class="btn small ghost" data-act="sched" data-id="${esc(d.id)}">Schedule</button>` +
        (a !== true ? `<button class="btn small ghost" data-act="activate" data-id="${esc(d.id)}">Activate</button>` : "") +
        (a !== false ? `<button class="btn small danger ghost" data-act="deactivate" data-id="${esc(d.id)}">Deactivate</button>` : ""); }]);
    box.innerHTML = hdr + (doctors().length ? table(cols, doctors()) : empty("No doctors yet."));
  };
  const specOptions = () => [["", "— choose —"], ...specialties().map(s => [s.id, s.name])];
  onClick(box, {
    add: () => {
      if (!specialties().length) { toast("Create a specialty first.", "warn"); return; }
      openForm({
        title: "Add doctor", submitLabel: "Create doctor",
        fields: `<div class="grid2">${field("First name", "firstName", { required: true }) + field("Last name", "lastName", { required: true }) + field("Personal number", "personalNumber", { required: true }) +
          field("Phone", "phone", { type: "tel", required: true }) + field("Email", "email", { type: "email", required: true }) + field("Password", "password", { type: "password", required: true })}</div>` +
          field("Specialty", "specialtyId", { type: "select", options: specOptions(), required: true }),
        onSubmit: async v => { await api("/api/Doctors", { method: "POST", body: { ...v, specialtyId: toInt(v.specialtyId) } }); toast("Doctor created."); },
        afterSave: () => { closeModal(); load(); }
      });
    },
    edit: el => {
      const d = findDoctor(el.dataset.id);
      openForm({
        title: "Edit doctor",
        fields: `<div class="grid2">${field("First name", "firstName", { value: d.firstName ?? "", required: true }) + field("Last name", "lastName", { value: d.lastName ?? "", required: true }) +
          field("Phone", "phone", { value: d.phone ?? "", required: true })}</div>` + field("Specialty", "specialtyId", { type: "select", options: specOptions(), value: d.specialtyId, required: true }),
        onSubmit: async v => { await api(`/api/Doctors/${d.id}`, { method: "PUT", body: { firstName: v.firstName, lastName: v.lastName, phone: v.phone, specialtyId: toInt(v.specialtyId) } }); toast("Doctor updated."); },
        afterSave: () => { closeModal(); load(); }
      });
    },
    sched: el => goTo("schedules", { scheduleDoctorId: el.dataset.id }),
    activate: async el => { const r = await runAction(el, () => api(`/api/Doctors/${el.dataset.id}/activate`, { method: "POST" }), "Doctor activated."); if (r.ok) load(); },
    deactivate: async el => {
      if (!confirm("Deactivate this doctor and their account?")) return;
      const r = await runAction(el, () => api(`/api/Doctors/${el.dataset.id}/deactivate`, { method: "POST" }), "Doctor deactivated."); if (r.ok) load();
    }
  });
  await load();
}

async function schedulesPage(box) {
  const flash = state.flash; state.flash = {};
  box.innerHTML = head("Doctor schedules") + loading();
  cache.doctors = null; await loadLookups();
  if (!cache.doctors) { box.innerHTML = head("Doctor schedules") + alertBox("Could not load doctors.", "error"); return; }
  let did = flash.scheduleDoctorId ? String(flash.scheduleDoctorId) : "";
  box.innerHTML = head("Doctor schedules", `<button class="btn" data-act="add" id="add-sch">Add working hours</button>`) +
    `<div class="toolbar"><label class="field"><span>Doctor</span><select id="s-doc"><option value="">— choose a doctor —</option>${doctors().map(d => `<option value="${esc(d.id)}" ${String(d.id) === did ? "selected" : ""}>${esc(doctorName(d))} (${esc(specialtyOfDoctor(d))})</option>`).join("")}</select></label></div><div id="s-list"></div>`;
  let items = [];
  const load = async () => {
    const out = $("#s-list", box);
    $("#add-sch", box).disabled = !did;
    if (!did) { out.innerHTML = empty("Choose a doctor to see their working hours."); return; }
    out.innerHTML = loading();
    try { items = asList(await api(`/api/doctors/${did}/schedules`)); out.innerHTML = scheduleTable(items, true); }
    catch (e) { out.innerHTML = errBox(e); }
  };
  const form = (title, s, onSubmit) => openForm({
    title,
    fields: field("Day of week", "dayOfWeek", { type: "select", required: true, value: s ? dayName(s.dayOfWeek) : "Monday", options: DAYS.map(d => [d, d]) }) +
      field("Start time", "startTime", { type: "time", required: true, value: s ? String(s.startTime).slice(0, 5) : "09:00" }) +
      field("End time", "endTime", { type: "time", required: true, value: s ? String(s.endTime).slice(0, 5) : "17:00" }),
    onSubmit: async v => {
      if (v.endTime <= v.startTime) throw new Error("The end time must be after the start time.");
      await onSubmit({ dayOfWeek: v.dayOfWeek, startTime: v.startTime + ":00", endTime: v.endTime + ":00" });
    },
    afterSave: () => { closeModal(); load(); }
  });
  $("#s-doc", box).addEventListener("change", e => { did = e.target.value; load(); });
  onClick(box, {
    add: () => form("Add working hours", null, async b => { await api(`/api/doctors/${did}/schedules`, { method: "POST", body: b }); toast("Working hours added."); }),
    editsch: el => { const s = items.find(x => String(x.id) === el.dataset.id); form("Edit working hours", s, async b => { await api(`/api/schedules/${s.id}`, { method: "PUT", body: b }); toast("Working hours updated."); }); },
    delsch: async el => {
      if (!confirm("Delete these working hours?")) return;
      const r = await runAction(el, () => api(`/api/schedules/${el.dataset.id}`, { method: "DELETE" }), "Working hours deleted."); if (r.ok) load();
    }
  });
  await load();
}

async function specialtiesPage(box) {
  const form = (title, s) => {
    const formEl = openForm({
      title,
      fields: field("Name", "name", { value: s ? s.name : "", required: true }) +
        field("This is Family Medicine (visits never need a referral)", "isFamilyMedicine", { type: "checkbox", value: s ? s.isFamilyMedicine : false }) +
        field("Visits require a referral", "requiresReferral", { type: "checkbox", value: s ? s.requiresReferral : false }),
      onSubmit: async v => {
        const body = { name: v.name, isFamilyMedicine: v.isFamilyMedicine, requiresReferral: v.isFamilyMedicine ? false : v.requiresReferral };
        await api(s ? `/api/Specialties/${s.id}` : "/api/Specialties", { method: s ? "PUT" : "POST", body });
        toast(s ? "Specialty updated." : "Specialty created.");
      },
      afterSave: () => { closeModal(); load(); }
    });

    const familyMedicine = $('[name="isFamilyMedicine"]', formEl);
    const requiresReferral = $('[name="requiresReferral"]', formEl);
    const syncReferralRules = changed => {
      // If old data already has both flags checked, keep the Family Medicine rule.
      if (!changed && familyMedicine.checked && requiresReferral.checked)
        requiresReferral.checked = false;
      if (changed?.checked) {
        const other = changed === familyMedicine ? requiresReferral : familyMedicine;
        other.checked = false;
      }
      familyMedicine.disabled = requiresReferral.checked;
      requiresReferral.disabled = familyMedicine.checked;
    };
    familyMedicine.addEventListener("change", e => syncReferralRules(e.currentTarget));
    requiresReferral.addEventListener("change", e => syncReferralRules(e.currentTarget));
    syncReferralRules();
    return formEl;
  };
  const load = async () => {
    cache.specialties = null;
    cache.doctors = null;
    box.innerHTML = head("Specialties", `<button class="btn" data-act="add">Add specialty</button>`) + loading();
    await loadLookups();
    const hdr = head("Specialties", `<button class="btn" data-act="add">Add specialty</button>`);
    if (!cache.specialties || !cache.doctors) { box.innerHTML = hdr + alertBox("Could not load doctor assignments. Try again before deleting a specialty.", "error"); return; }
    box.innerHTML = hdr + (specialties().length ? table([["Name", s => esc(s.name)], ["Family Medicine", s => isFM(s) ? "Yes" : "No"], ["Needs referral", s => needsReferral(s) ? "Yes" : "No"],
      ["", s => {
        const assignedDoctors = doctors().filter(d => String(d.specialtyId) === String(s.id)).length;
        const deleteAction = assignedDoctors
          ? `<span class="badge warn" title="Remove or reassign these doctors before deleting this specialty">In use by ${assignedDoctors} doctor${assignedDoctors === 1 ? "" : "s"} · cannot delete</span>`
          : `<button class="btn small danger ghost" data-act="del" data-id="${esc(s.id)}">Delete</button>`;
        return `<button class="btn small ghost" data-act="edit" data-id="${esc(s.id)}">Edit</button>${deleteAction}`;
      }]], specialties()) : empty("No specialties yet."));
  };
  onClick(box, {
    add: () => form("Add specialty", null),
    edit: el => form("Edit specialty", findSpecialty(el.dataset.id)),
    del: async el => {
      if (doctors().some(d => String(d.specialtyId) === String(el.dataset.id))) {
        toast("This specialty is assigned to a doctor and cannot be deleted.", "warn");
        return;
      }
      if (!confirm("Delete this specialty? This only works if no doctors use it.")) return;
      const r = await runAction(el, () => api(`/api/Specialties/${el.dataset.id}`, { method: "DELETE" }), "Specialty deleted."); if (r.ok) load();
    }
  });
  await load();
}

// kind: "receptionists" | "admins"
async function staffPage(box, kind) {
  const title = kind === "admins" ? "Administrator accounts" : "Receptionist accounts";
  const one = kind === "admins" ? "administrator" : "receptionist";
  const add = `<button class="btn" data-act="add">Add ${one}</button>`;
  const load = async () => {
    box.innerHTML = head(title, add) + loading();
    try {
      const allAccounts = asList(await api(`/api/staff/${kind}`));
      const activeAdminCount = kind === "admins" ? allAccounts.filter(u => activeFlag(u) === true).length : 0;
      const currentUserId = String(pick(state.me, ...ME_FIELDS.userId) ?? "");
      const currentEmail = String(pick(state.me, ...ME_FIELDS.email) ?? "").toLowerCase();
      // The seeded SuperAdmin also has the Admin role for admin-level access.
      // Keep that protected account out of the regular administrator list.
      const list = kind === "admins" && state.role === "SuperAdmin"
        ? allAccounts.filter(u => String(u.userId ?? u.id) !== currentUserId && String(u.email ?? "").toLowerCase() !== currentEmail)
        : allAccounts;
      box.innerHTML = head(title, add) + (list.length ? table([["Name", u => esc(personName(u, "—"))], ["Email", u => esc(u.email ?? "—")], ["Phone", u => esc(u.phone ?? "—")],
        ["Status", u => { const a = activeFlag(u); return a === undefined ? "—" : `<span class="badge ${a ? "ok" : "bad"}">${a ? "Active" : "Inactive"}</span>`; }],
        ["", u => { const a = activeFlag(u), id = esc(u.userId ?? u.id);
          const activate = a !== true ? `<button class="btn small ghost" data-act="activate" data-id="${id}">Activate</button>` : "";
          if (a !== true) return activate;
          if (kind === "admins" && activeAdminCount <= 1) return `${activate}<span class="muted" title="The last active administrator cannot be deactivated">Last active admin · protected</span>`;
          return `${activate}<button class="btn small danger ghost" data-act="deactivate" data-id="${id}">Deactivate</button>`; }]], list) : empty(`No ${one} accounts yet.`));
    } catch (e) { box.innerHTML = head(title, add) + errBox(e); }
  };
  onClick(box, {
    add: () => openForm({
      title: `Add ${one}`, submitLabel: "Create account",
      fields: `<div class="grid2">${field("First name", "firstName", { required: true }) + field("Last name", "lastName", { required: true }) + field("Personal number", "personalNumber", { required: true }) +
        field("Phone", "phone", { type: "tel", required: true }) + field("Email", "email", { type: "email", required: true }) + field("Password", "password", { type: "password", required: true })}</div>`,
      onSubmit: async v => { await api(`/api/staff/${kind}`, { method: "POST", body: v }); toast("Account created."); },
      afterSave: () => { closeModal(); load(); }
    }),
    activate: async el => { const r = await runAction(el, () => api(`/api/staff/${encodeURIComponent(el.dataset.id)}/activate`, { method: "POST" }), "Account activated."); if (r.ok) load(); },
    deactivate: async el => {
      if (!confirm("Deactivate this account?")) return;
      const r = await runAction(el, () => api(`/api/staff/${encodeURIComponent(el.dataset.id)}/deactivate`, { method: "POST" }), "Account deactivated."); if (r.ok) load();
    }
  });
  await load();
}

/* ===== 9. NAVIGATION, SESSION & START ===== */
const NAV = {
  Patient: [
    { id: "dashboard", label: "Dashboard", render: patientDashboard },
    { id: "doctors", label: "Doctors", hint: "Browse doctors and specialties", render: browseDoctors },
    { id: "book", label: "Book a visit", hint: "Pick a free time", render: bookVisit },
    { id: "appointments", label: "My appointments", hint: "View or cancel visits", render: patientAppointments },
    { id: "referrals", label: "Referrals", hint: "Referrals from your doctor", render: patientReferrals },
    { id: "history", label: "Consultations", hint: "Your consultation history", render: patientHistory },
    { id: "profile", label: "Medical profile", hint: "View only", render: patientProfile }
  ],
  Doctor: [
    { id: "dashboard", label: "Dashboard", render: doctorDashboard },
    { id: "appointments", label: "Appointments", hint: "Visits and consultations", render: doctorAppointments },
    { id: "schedule", label: "My schedule", hint: "Working hours", render: doctorSchedule },
    { id: "patients", label: "My patients", hint: "Patients and referrals", render: patientsPage }
  ],
  Receptionist: [
    { id: "dashboard", label: "Dashboard", render: staffDashboard },
    { id: "patients", label: "Patients", hint: "Find, register, family doctor", render: patientsPage },
    { id: "doctors", label: "Doctors", hint: "See the doctors", render: doctorsPage }
  ],
  Admin: [
    { id: "dashboard", label: "Dashboard", render: staffDashboard },
    { id: "doctors", label: "Doctors", hint: "Add, edit, activate", render: doctorsPage },
    { id: "schedules", label: "Schedules", hint: "Working hours", render: schedulesPage },
    { id: "specialties", label: "Specialties", hint: "Manage specialties", render: specialtiesPage },
    { id: "patients", label: "Patients", hint: "Find and manage patients", render: patientsPage },
    { id: "receptionists", label: "Receptionists", hint: "Receptionist accounts", render: b => staffPage(b, "receptionists") }
  ]
};
function buildNav(role) {
  const list = role === "SuperAdmin"
    ? [...NAV.Admin, { id: "admins", label: "Administrators", hint: "Administrator accounts (SuperAdmin only)", render: b => staffPage(b, "admins") }]
    : NAV[role];
  return [...list, { id: "account", label: "Account", render: accountPage }];
}

function goTo(id, flash) {
  state.flash = flash || {};
  if (location.hash === "#" + id) route(); else location.hash = id;
}
function route() {
  if (!state.role) return;
  const id = location.hash.slice(1) || "dashboard";
  const item = state.nav.find(n => n.id === id) || state.nav[0];
  document.querySelectorAll("#nav a").forEach(a => a.classList.toggle("active", a.dataset.id === item.id));
  const box = document.createElement("div");
  $("#view").replaceChildren(box);
  $("#app-view").classList.remove("menu-open");
  window.scrollTo(0, 0);
  Promise.resolve().then(() => item.render(box)).catch(e => { box.innerHTML = head(item.label) + errBox(e); });
}

function showAuth(msg, type = "info") {
  state.role = null;
  delete document.body.dataset.role;
  $("#app-view").hidden = true; $("#userbox").hidden = true; $("#menu-btn").hidden = true;
  $("#auth-view").hidden = false;
  const m = $("#auth-msg");
  m.hidden = !msg; m.className = `alert ${type}`; m.textContent = msg || "";
}
function clearSession() {
  sessionStorage.removeItem(TOKEN_KEY);
  Object.assign(state, { token: null, me: null, role: null, patientId: null, doctorId: null, nav: [], flash: {} });
  cache.specialties = null; cache.doctors = null;
  closeModal();
}
function handleExpired() { clearSession(); showAuth("Your session has expired. Please sign in again.", "warn"); }
function logout() { clearSession(); history.replaceState(null, "", location.pathname); showAuth("You have been signed out.", "info"); }

// Collects every string stored under a key that contains "role" (any nesting, any casing).
function collectRoles(value, out = [], underRoleKey = false) {
  if (value == null) return out;
  if (typeof value === "string") { if (underRoleKey) out.push(value); return out; }
  if (Array.isArray(value)) { value.forEach(v => collectRoles(v, out, underRoleKey)); return out; }
  if (typeof value === "object") {
    // claim-style items: { type: "...role", value: "Patient" }
    const t = String(value.type ?? value.claimType ?? value.key ?? "");
    if (/role/i.test(t) && typeof value.value === "string") out.push(value.value);
    for (const [k, v] of Object.entries(value)) collectRoles(v, out, underRoleKey || /role/i.test(k));
  }
  return out;
}
function jwtPayload(token) {
  try {
    const part = token.split(".")[1].replace(/-/g, "+").replace(/_/g, "/");
    return JSON.parse(decodeURIComponent(escape(atob(part))));
  } catch { return null; }
}

async function startSession(loginRes) {
  const me = await api("/api/Auth/me");
  const claims = jwtPayload(state.token);
  const found = [...collectRoles(me), ...collectRoles(loginRes), ...collectRoles(claims)].map(r => String(r).toLowerCase());
  const role = ROLE_ORDER.find(r => found.includes(r.toLowerCase()));
  if (!role) {
    const seen = JSON.stringify({ me, loginResponse: loginRes && typeof loginRes === "object" ? { ...loginRes, token: "…" } : undefined, tokenClaims: claims }).slice(0, 600);
    throw new ApiError(0, `No known role found (Patient, Doctor, Receptionist, Admin, SuperAdmin). The server sent: ${seen}`);
  }
  state.me = me; state.role = role;
  document.body.dataset.role = role;
  state.patientId = pick(me, ...ME_FIELDS.patientId) ?? null;
  state.doctorId = pick(me, ...ME_FIELDS.doctorId) ?? null;
  if (role === "Doctor" && state.doctorId == null) { // fallback: find myself in the doctors list
    try {
      const uid = pick(me, ...ME_FIELDS.userId), email = String(pick(me, ...ME_FIELDS.email) ?? "").toLowerCase();
      const mine = asList(await api("/api/Doctors")).find(d => (uid && String(d.userId) === String(uid)) || (email && String(d.email ?? "").toLowerCase() === email));
      if (mine) state.doctorId = mine.id;
    } catch { /* the doctor pages will explain what is missing */ }
  }
  state.nav = buildNav(role);
  $("#nav").innerHTML = state.nav.map(n => `<a href="#${n.id}" data-id="${n.id}">${esc(n.label)}</a>`).join("");
  $("#user-name").textContent = userDisplayName();
  $("#user-role").textContent = role;
  $("#auth-view").hidden = true; $("#app-view").hidden = false; $("#userbox").hidden = false; $("#menu-btn").hidden = false;
  route();
}

function bindAuthForms() {
  document.querySelectorAll(".tab").forEach(t => t.addEventListener("click", () => {
    document.querySelectorAll(".tab").forEach(x => x.classList.toggle("active", x === t));
    $("#login-form").hidden = t.dataset.tab !== "login";
    $("#register-form").hidden = t.dataset.tab !== "register";
    $("#auth-msg").hidden = true;
  }));

  $("#login-form").addEventListener("submit", async e => {
    e.preventDefault();
    const f = e.target, v = readForm(f);
    if (!v.email || !v.password) { showAuth("Enter your email and password.", "error"); return; }
    try {
      await busy($("button", f), async () => {
        const res = await api("/api/Auth/login", { method: "POST", auth: false, body: { email: v.email, password: v.password } });
        const token = typeof res === "string" ? res : pick(res, "token", "accessToken", "jwt", "data.token");
        if (!token) throw new Error("The server did not return a token.");
        state.token = token; sessionStorage.setItem(TOKEN_KEY, token);
        f.reset();
        await startSession(res);
      });
    } catch (err) {
      const wasSignedIn = !!state.token;
      if (wasSignedIn) { sessionStorage.removeItem(TOKEN_KEY); state.token = null; }
      showAuth(err.status === 401 ? "Invalid email or password. After 5 wrong attempts the account is locked for 15 minutes." : err.message, "error");
    }
  });

  $("#register-form").addEventListener("submit", async e => {
    e.preventDefault();
    const f = e.target;
    if (!f.reportValidity()) return;
    const v = readForm(f);
    try {
      await busy($("button", f), () => api("/api/Auth/register", { method: "POST", auth: false, body: v }));
      f.reset();
      document.querySelector('.tab[data-tab="login"]').click();
      $("#login-form [name=email]").value = v.email;
      showAuth("Account created. You can sign in now.", "ok");
    } catch (err) { showAuth(err.message, "error"); }
  });
}

function bindGlobal() {
  bindAuthForms();
  $("#logout-btn").addEventListener("click", logout);
  $("#menu-btn").addEventListener("click", () => {
    const isOpen = $("#app-view").classList.toggle("menu-open");
    $("#menu-btn").setAttribute("aria-expanded", String(isOpen));
  });
  $("#nav").addEventListener("click", e => { const a = e.target.closest("a"); if (a && a.getAttribute("href") === location.hash) route(); });
  window.addEventListener("hashchange", route);
  dlg.addEventListener("click", e => { if (e.target === dlg || e.target.closest("[data-close]")) closeModal(); });
}

(async function init() {
  bindGlobal();
  if (!state.token) { showAuth(); return; }
  try { await startSession(); }
  catch (e) { if (state.token) { sessionStorage.removeItem(TOKEN_KEY); state.token = null; showAuth(e.message, "error"); } }
})();
