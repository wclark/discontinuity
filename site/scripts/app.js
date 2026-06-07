(function () {
  const Engine = window.DiscontinuityEngine;
  const DATA = Engine.data;
  let save = Engine.loadSave();

  const app = document.querySelector("#app");
  const saveButton = document.querySelector("#save-button");
  const resetButton = document.querySelector("#reset-button");

  function escapeHtml(value) {
    return String(value)
      .replaceAll("&", "&amp;")
      .replaceAll("<", "&lt;")
      .replaceAll(">", "&gt;")
      .replaceAll('"', "&quot;")
      .replaceAll("'", "&#039;");
  }

  function timeLabel(timeId) {
    return (DATA.timeSlots.find((slot) => slot.id === timeId) || {}).label || timeId;
  }

  function tagsHtml(tags) {
    if (!tags || !tags.length) return "";
    return `
      <span class="action-meta">
        ${tags
          .slice(0, 4)
          .map((tag) => `<span class="tag ${escapeHtml(tag)}">${escapeHtml(tag)}</span>`)
          .join("")}
      </span>
    `;
  }

  function emptyNote(text) {
    return `<p class="empty-note">${escapeHtml(text)}</p>`;
  }

  function renderStart(model) {
    const ending = model.save.lastEnding
      ? `<div class="end-note">${escapeHtml(model.save.lastEnding.text)}</div>`
      : "";
    const characterButtons = model.characters
      .map((character) => {
        const unlocked = model.save.unlockedCharacters.includes(character.id);
        const status = unlocked ? "Available" : `Unlocks after ${DATA.characters[character.unlocksAfter].name}`;
        return `
          <button class="character-button" type="button" data-start-character="${character.id}" ${unlocked ? "" : "disabled"}>
            <span class="character-name">${escapeHtml(character.name)}</span>
            <span class="character-role">${escapeHtml(character.role)} &middot; ${escapeHtml(status)}</span>
            <span class="character-role">${escapeHtml(character.motive)}</span>
          </button>
        `;
      })
      .join("");

    return `
      <section class="start-panel">
        <p class="eyebrow">Choose a viewpoint</p>
        <h2 class="location-name">The same day, remembered differently</h2>
        ${ending}
        <div class="start-grid">${characterButtons}</div>
        ${renderTimeline(model.timeline)}
      </section>
    `;
  }

  function renderRun(model) {
    return `
      <section class="layout">
        <aside>
          <div class="location-frame">
            <img src="${escapeHtml(model.location.image)}" alt="Empty ${escapeHtml(model.location.name)}">
            <div class="location-caption">
              <div class="time-location">
                <span>${escapeHtml(model.time.label)}</span>
                <span>${escapeHtml(model.player.name)}</span>
              </div>
              <h2 class="location-name">${escapeHtml(model.location.name)}</h2>
              <p class="empty-note">${escapeHtml(model.location.crowd)}</p>
              ${renderMap(model)}
            </div>
          </div>
        </aside>

        <section class="panel scene">
          <div class="scene-heading">
            <p class="scene-kicker">${escapeHtml(model.player.role)} &middot; ${escapeHtml(model.player.motive)}</p>
            <h2>${escapeHtml(model.location.name)}, ${escapeHtml(model.time.label)}</h2>
          </div>
          <div class="scene-copy">
            <p>${escapeHtml(model.location.description)}</p>
            ${scenePopulation(model)}
          </div>
          ${renderEvents(model.run.turnMessages)}
          <section>
            <h3 class="section-title">Available Actions</h3>
            <div class="action-list">
              ${model.actions
                .map(
                  ({ action }) => `
                    <button class="action-button" type="button" data-action-id="${escapeHtml(action.id)}">
                      <span class="action-label">${escapeHtml(action.label)}</span>
                      ${tagsHtml(action.tags)}
                    </button>
                  `
                )
                .join("")}
            </div>
          </section>
        </section>

        <aside class="sidebar">
          <section class="panel">
            ${renderPeople(model)}
            ${renderInventory(model)}
            ${renderVisibleItems(model)}
          </section>
          <section class="panel">
            ${renderCharacterState(model)}
          </section>
          <section class="panel">
            ${renderTimeline(model.timeline)}
          </section>
          <section class="panel debug-panel">
            ${renderDecisionDebug(model.decisionDebug)}
          </section>
        </aside>
      </section>
    `;
  }

  function scenePopulation(model) {
    const others = model.peoplePresent
      .filter((person) => person.id !== model.player.id)
      .map((person) => person.name);
    if (!others.length) return `<p>You are alone here.</p>`;
    return `<p>Present: ${escapeHtml(others.join(", "))}.</p>`;
  }

  function renderMap(model) {
    const nodes = Object.values(DATA.locations)
      .map((location) => {
        const here = location.id === model.location.id;
        return `<li class="map-node ${here ? "is-here" : ""}">${escapeHtml(location.name)}</li>`;
      })
      .join("");
    return `<ul class="map-list" aria-label="Map">${nodes}</ul>`;
  }

  function renderEvents(messages) {
    if (!messages || !messages.length) {
      return `<div class="event-stack">${emptyNote("No new event has reached you yet.")}</div>`;
    }
    return `
      <div class="event-stack">
        ${messages
          .map(
            (event) => `
              <p class="event-line ${event.private ? "is-private" : ""}">
                ${escapeHtml(event.text)}
              </p>
            `
          )
          .join("")}
      </div>
    `;
  }

  function renderPeople(model) {
    const people = model.peoplePresent
      .map((person) => {
        const you = person.id === model.player.id ? "You" : person.role;
        return `
          <li class="chip" style="border-color: ${escapeHtml(person.color)}">
            <span>
              ${escapeHtml(person.name)}
              <small>${escapeHtml(you)}</small>
            </span>
          </li>
        `;
      })
      .join("");
    return `
      <div class="dashboard-section">
        <h3 class="section-title">People Present</h3>
        <ul class="chip-list">${people}</ul>
      </div>
    `;
  }

  function renderInventory(model) {
    const items = model.inventory
      .map((item) => `<li class="inventory-item">${escapeHtml(item.name)}</li>`)
      .join("");
    return `
      <div class="dashboard-section">
        <h3 class="section-title">Inventory</h3>
        ${items ? `<ul class="inventory-list">${items}</ul>` : emptyNote("Nothing carried.")}
      </div>
    `;
  }

  function renderVisibleItems(model) {
    const items = model.visibleItems
      .map((item) => `<li class="inventory-item">${escapeHtml(item.name)}: ${escapeHtml(item.description)}</li>`)
      .join("");
    return `
      <div class="dashboard-section">
        <h3 class="section-title">Visible Items</h3>
        ${items ? `<ul class="inventory-list">${items}</ul>` : emptyNote("No loose object draws your eye.")}
      </div>
    `;
  }

  function renderCharacterState(model) {
    const rows = model.relationships
      .map(
        (row) => `
          <div class="stat-row">
            <strong>${escapeHtml(row.person)}</strong>
            ${escapeHtml(row.metric)} ${row.value > 0 ? "+" : ""}${escapeHtml(row.value)}
          </div>
        `
      )
      .join("");
    return `
      <div class="dashboard-section">
        <h3 class="section-title">Emotional State</h3>
        <div class="stat-grid">
          <div class="stat-row"><strong>Mood</strong>${escapeHtml(model.run.people[model.player.id].mood)}</div>
          ${rows || '<div class="stat-row"><strong>Social weather</strong>unsettled but unmarked</div>'}
        </div>
      </div>
    `;
  }

  function scoreText(value) {
    return Number(value).toFixed(1);
  }

  function adjustmentHtml(value) {
    if (Math.abs(value) < 0.05) return "";
    return `<span class="debug-adjustment">applied ${value > 0 ? "+" : ""}${scoreText(value)}</span>`;
  }

  function renderManualAdjustments(adjustments) {
    const rows = adjustments
      .map(
        (adjustment) => `
          <li class="debug-manual-adjustment">
            <span class="debug-label">${escapeHtml(adjustment.actionLabel)}</span>
            <span class="debug-score">+${scoreText(adjustment.amount)}</span>
            <span class="debug-adjustment-meta">
              ${escapeHtml(adjustment.startsAtLabel)}
              ${adjustment.location ? `&middot; ${escapeHtml(adjustment.location)}` : ""}
              &middot; ${adjustment.active ? "active" : "pending"}
            </span>
            ${tagsHtml(adjustment.tags)}
          </li>
        `
      )
      .join("");
    return `
      <div class="debug-subsection">
        <h4 class="debug-subtitle">Manual Adjustments</h4>
        ${rows ? `<ul class="debug-manual-list">${rows}</ul>` : emptyNote("No manual adjustments saved for this character.")}
      </div>
    `;
  }

  function renderDecisionDebug(debugRows) {
    const rows = debugRows
      .map((entry) => {
        const top = entry.options[0];
        const optionRows = entry.options
          .map(
            (option) => `
              <li class="debug-option ${option.rank === 1 ? "is-top" : ""}">
                <span class="debug-rank">${option.rank}</span>
                <span class="debug-label">${escapeHtml(option.label)}</span>
                <span class="debug-score">${scoreText(option.score)}</span>
                ${adjustmentHtml(option.adjustment)}
                ${tagsHtml(option.tags)}
              </li>
            `
          )
          .join("");
        return `
          <details class="debug-character" ${entry.isPlayer ? "open" : ""}>
            <summary>
              <span>
                <strong>${escapeHtml(entry.name)}</strong>
                ${entry.isPlayer ? '<small class="debug-current">player</small>' : ""}
              </span>
              <span class="debug-location">${escapeHtml(entry.location)}</span>
              <span class="debug-pick">${top ? escapeHtml(top.label) : "No valid action"}</span>
              <span class="debug-score">${top ? scoreText(top.score) : "--"}</span>
            </summary>
            ${renderManualAdjustments(entry.manualAdjustments)}
            <h4 class="debug-subtitle debug-options-title">Ranked Options</h4>
            <ol class="debug-option-list">${optionRows}</ol>
          </details>
        `;
      })
      .join("");
    return `
      <div class="dashboard-section">
        <h3 class="section-title">Adjusted Default Path</h3>
        <div class="debug-character-list">${rows}</div>
      </div>
    `;
  }

  function renderTimeline(timeline) {
    const rows = timeline
      .map(
        (entry) => `
          <li class="timeline-item">
            <span class="timeline-time">${escapeHtml(timeLabel(entry.timeId))}</span>
            <span class="timeline-text ${entry.certainty === "uncertain" ? "uncertain" : ""}">
              ${escapeHtml(entry.text)}
            </span>
          </li>
        `
      )
      .join("");
    return `
      <div class="dashboard-section">
        <h3 class="section-title">Known Timeline</h3>
        <ul class="timeline-list">${rows}</ul>
      </div>
    `;
  }

  function render() {
    const model = Engine.viewModel(save);
    app.innerHTML = model.mode === "run" ? renderRun(model) : renderStart(model);
  }

  app.addEventListener("click", (event) => {
    const actionButton = event.target.closest("[data-action-id]");
    if (actionButton) {
      save = Engine.takePlayerAction(save, actionButton.dataset.actionId);
      render();
      return;
    }

    const startButton = event.target.closest("[data-start-character]");
    if (startButton && !startButton.disabled) {
      save = Engine.startRun(save, startButton.dataset.startCharacter);
      render();
    }
  });

  saveButton.addEventListener("click", () => {
    Engine.persist(save);
    saveButton.textContent = "Saved";
    window.setTimeout(() => {
      saveButton.textContent = "Save";
    }, 900);
  });

  resetButton.addEventListener("click", () => {
    if (!window.confirm("Reset Discontinuity's local save?")) return;
    save = Engine.resetSave();
    render();
  });

  render();
})();
