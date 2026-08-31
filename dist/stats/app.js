const DATA_PATHS = Object.freeze({
  matches: "./data/matches.json",
  stats: "./data/stats.json",
  briefing: "./data/briefing.json"
});

const elements = {
  loading: document.querySelector("#loading-state"),
  error: document.querySelector("#error-state"),
  errorMessage: document.querySelector("#error-message"),
  empty: document.querySelector("#empty-state"),
  content: document.querySelector("#content"),
  formSequence: document.querySelector("#form-sequence"),
  formSummary: document.querySelector("#form-summary"),
  headToHead: document.querySelector("#head-to-head-list"),
  matches: document.querySelector("#matches-list"),
  briefingSection: document.querySelector("#briefing-section"),
  briefingContent: document.querySelector("#briefing-content")
};

function isRecord(value) {
  return value !== null && typeof value === "object" && !Array.isArray(value);
}

function validateData(matches, stats) {
  if (!Array.isArray(matches) || !isRecord(stats) || !isRecord(stats.recentForm) || !Array.isArray(stats.headToHead)) {
    throw new Error("Dataformatet er ikke gyldigt.");
  }

  if (!Array.isArray(stats.recentForm.outcomes) ||
      !Number.isInteger(stats.recentForm.wins) ||
      !Number.isInteger(stats.recentForm.losses) ||
      !Number.isInteger(stats.recentForm.unknown)) {
    throw new Error("Formdata er ikke gyldig.");
  }
}

function text(value, fallback = "Ikke oplyst") {
  return value === null || value === undefined || value === "" ? fallback : String(value);
}

function formatDate(value) {
  const date = new Date(value);
  return Number.isNaN(date.valueOf())
    ? "Dato ikke oplyst"
    : new Intl.DateTimeFormat("da-DK", { dateStyle: "medium" }).format(date);
}

function formatPercentage(value) {
  return value === null || value === undefined ? "Ikke oplyst" : `${value.toLocaleString("da-DK")} %`;
}

function outcomeClass(outcome) {
  const normalized = String(outcome || "unknown").toLowerCase();
  return ["win", "loss"].includes(normalized) ? normalized : "unknown";
}

function createElement(tag, className, content) {
  const element = document.createElement(tag);
  if (className) element.className = className;
  if (content !== undefined) element.textContent = content;
  return element;
}

function renderForm(form) {
  elements.formSequence.replaceChildren();
  form.outcomes.forEach(outcome => {
    const normalized = outcomeClass(outcome);
    const label = normalized === "win" ? "W" : normalized === "loss" ? "L" : "?";
    const pill = createElement("span", `form-pill form-pill--${normalized}`, label);
    pill.title = normalized === "win" ? "Sejr" : normalized === "loss" ? "Nederlag" : "Ukendt resultat";
    elements.formSequence.append(pill);
  });

  const values = [
    ["Sejre", form.wins],
    ["Nederlag", form.losses],
    ["Ukendt", form.unknown],
    ["Sejrsprocent", formatPercentage(form.winPercentage)]
  ];
  elements.formSummary.replaceChildren();
  values.forEach(([label, value]) => {
    const item = createElement("div", "stat-item");
    item.append(createElement("dt", null, label), createElement("dd", null, text(value)));
    elements.formSummary.append(item);
  });
}

function competitionName(match) {
  return match.tournament?.name || match.serie?.name || match.league?.name || "Konkurrence ikke oplyst";
}

function renderHeadToHead(summaries) {
  elements.headToHead.replaceChildren();
  summaries.forEach(summary => {
    const card = createElement("article", "summary-card");
    card.append(createElement("h3", null, text(summary.opponentName, `Hold ${summary.opponentTeamId}`)));
    const record = createElement("p", "summary-card__record");
    record.append(document.createTextNode(`${summary.astralisWins} – ${summary.opponentWins}`));
    record.append(createElement("span", null, `af ${summary.meetings} møder`));
    card.append(record, createElement("p", "summary-card__meta", `${summary.unknown} med ukendt resultat`));
    elements.headToHead.append(card);
  });
}

function renderMatches(matches) {
  elements.matches.replaceChildren();
  matches.slice(0, 10).forEach(match => {
    const outcome = outcomeClass(match.outcome);
    const row = createElement("article", "match-row");
    row.append(createElement("time", "match-row__date", formatDate(match.relevantAt)));
    const opponent = createElement("div", "match-row__opponent");
    opponent.append(createElement("h3", `outcome--${outcome}`, `${outcome === "win" ? "Sejr" : outcome === "loss" ? "Nederlag" : "Ukendt"} mod ${text(match.opponentName, `hold ${match.opponentTeamId}`)}`));
    opponent.append(createElement("p", "match-row__meta", competitionName(match)));
    row.append(opponent);
    const score = match.astralisScore === null || match.opponentScore === null || match.astralisScore === undefined || match.opponentScore === undefined
      ? "Score ikke oplyst"
      : `${match.astralisScore} – ${match.opponentScore}`;
    row.append(createElement("strong", "match-row__score", score));
    elements.matches.append(row);
  });
}

function showState(state, message) {
  elements.loading.hidden = state !== "loading";
  elements.error.hidden = state !== "error";
  elements.empty.hidden = state !== "empty";
  elements.content.hidden = state !== "content";
  if (message) elements.errorMessage.textContent = message;
}

async function loadData() {
  const responses = await Promise.all([DATA_PATHS.matches, DATA_PATHS.stats].map(path => fetch(path, { cache: "no-cache" })));
  if (responses.some(response => !response.ok)) throw new Error("En eller flere datafiler kunne ikke læses.");
  const [matches, stats] = await Promise.all(responses.map(response => response.json()));
  validateData(matches, stats);
  return { matches, stats };
}

async function loadBriefing() {
  try {
    const response = await fetch(DATA_PATHS.briefing, { cache: "no-cache" });
    if (!response.ok) return null;
    return await response.json();
  } catch {
    return null;
  }
}

function renderBriefing(asset) {
  elements.briefingSection.hidden = false;
  elements.briefingContent.replaceChildren();
  const briefing = asset?.availability === "available" && isRecord(asset.briefing) && isRecord(asset.briefing.briefing)
    ? asset.briefing.briefing
    : null;

  if (!briefing || typeof briefing.headline !== "string" || typeof briefing.summary !== "string" || !Array.isArray(briefing.keyPoints)) {
    const message = asset?.unavailableReason === "noEligibleTarget"
      ? "Der er ingen kommende kamp, som kan få en AI-briefing lige nu."
      : asset?.unavailableReason === "generationFailed"
        ? "AI Match Briefing kunne ikke genereres denne gang."
        : "AI Match Briefing er ikke tilgængelig lige nu.";
    elements.briefingContent.append(createElement("p", "briefing-panel__unavailable", message));
    return;
  }

  const article = createElement("article", "briefing-card");
  article.append(createElement("h3", null, briefing.headline), createElement("p", "briefing-card__summary", briefing.summary));
  const points = createElement("ul", "briefing-card__points");
  briefing.keyPoints.filter(point => typeof point === "string").forEach(point => points.append(createElement("li", null, point)));
  article.append(points);
  elements.briefingContent.append(article);
}

async function start() {
  showState("loading");
  const briefingPromise = loadBriefing();
  try {
    const { matches, stats } = await loadData();
    renderBriefing(await briefingPromise);
    if (matches.length === 0) {
      showState("empty");
      return;
    }
    renderForm(stats.recentForm);
    renderHeadToHead(stats.headToHead);
    renderMatches(matches);
    showState("content");
  } catch (error) {
    renderBriefing(await briefingPromise);
    showState("error", error instanceof Error ? error.message : "Statistikkerne kunne ikke indlæses.");
  }
}

start();
