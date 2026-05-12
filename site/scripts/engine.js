(function () {
  const DATA = window.DISCONTINUITY_DATA;
  const METRICS = [
    "trust",
    "resentment",
    "fear",
    "gratitude",
    "shame",
    "suspicion",
    "obligation",
    "protectiveness",
    "guilt"
  ];
  const BIAS_MARGIN = 6;

  function clone(value) {
    return JSON.parse(JSON.stringify(value));
  }

  function createSave() {
    return {
      version: 1,
      unlockedCharacters: ["clara"],
      completedRuns: [],
      authoredBiases: {},
      discoveredTimeline: [],
      currentRun: null,
      lastEnding: null
    };
  }

  function loadSave() {
    try {
      const raw = window.localStorage.getItem(DATA.storageKey);
      if (!raw) return createSave();
      return { ...createSave(), ...JSON.parse(raw) };
    } catch (error) {
      console.warn("Could not load save.", error);
      return createSave();
    }
  }

  function persist(save) {
    window.localStorage.setItem(DATA.storageKey, JSON.stringify(save));
  }

  function resetSave() {
    const save = createSave();
    persist(save);
    return save;
  }

  function createRelationshipTable() {
    const table = {};
    Object.keys(DATA.characters).forEach((from) => {
      table[from] = {};
      Object.keys(DATA.characters).forEach((to) => {
        if (from === to) return;
        table[from][to] = {};
        METRICS.forEach((metric) => {
          table[from][to][metric] = 0;
        });
      });
    });
    return table;
  }

  function createInitialRun(playerId) {
    const people = {};
    Object.values(DATA.characters).forEach((character) => {
      people[character.id] = {
        location: character.startLocation,
        mood: "steady"
      };
    });

    const items = {};
    Object.values(DATA.items).forEach((item) => {
      items[item.id] = {
        owner: item.startOwner,
        location: item.startLocation
      };
    });

    return {
      playerId,
      timeIndex: 0,
      people,
      items,
      facts: {
        envelopeKnownMissing: false,
        valeTookEnvelope: false,
        claraHasVouch: false,
        jonahPubliclySuspected: false,
        claraPubliclySuspected: false,
        jonahAccused: false,
        accusationDeflected: false
      },
      relationships: createRelationshipTable(),
      memories: Object.fromEntries(Object.keys(DATA.characters).map((id) => [id, []])),
      eventLog: [],
      turnMessages: [
        {
          text: "The day begins again, but not cleanly. Old choices wait inside ordinary habits.",
          private: false
        }
      ],
      actionHistory: {},
      slotHistory: {},
      ended: false,
      ending: null
    };
  }

  function startRun(save, playerId) {
    save.currentRun = createInitialRun(playerId);
    save.lastEnding = null;
    persist(save);
    return save;
  }

  function getTime(run) {
    return DATA.timeSlots[run.timeIndex] || DATA.timeSlots[DATA.timeSlots.length - 1];
  }

  function getCharacter(id) {
    return DATA.characters[id];
  }

  function getLocation(id) {
    return DATA.locations[id];
  }

  function getPersonLocation(run, personId) {
    return run.people[personId] && run.people[personId].location;
  }

  function resolvePerson(value, actorId, targetId) {
    if (value === "actor") return actorId;
    if (value === "target") return targetId;
    return value;
  }

  function relationship(run, from, to, metric) {
    return (((run.relationships[from] || {})[to] || {})[metric]) || 0;
  }

  function ensureRelationship(run, from, to) {
    run.relationships[from] = run.relationships[from] || {};
    run.relationships[from][to] = run.relationships[from][to] || {};
    METRICS.forEach((metric) => {
      if (typeof run.relationships[from][to][metric] !== "number") {
        run.relationships[from][to][metric] = 0;
      }
    });
    return run.relationships[from][to];
  }

  function hasMemory(run, personId, memoryId) {
    return (run.memories[personId] || []).some((memory) => memory.id === memoryId);
  }

  function addMemory(run, personId, memory) {
    if (!personId || hasMemory(run, personId, memory.id)) return;
    run.memories[personId].push(memory);
  }

  function itemOwner(run, itemId) {
    return run.items[itemId] && run.items[itemId].owner;
  }

  function itemLocation(run, itemId) {
    return run.items[itemId] && run.items[itemId].location;
  }

  function conditionMet(condition, run, actorId, targetId) {
    if (!condition) return true;
    if (condition.type === "not") {
      return !conditionMet(condition.condition, run, actorId, targetId);
    }
    if (condition.type === "all") {
      return condition.conditions.every((entry) => conditionMet(entry, run, actorId, targetId));
    }
    if (condition.type === "any") {
      return condition.conditions.some((entry) => conditionMet(entry, run, actorId, targetId));
    }
    if (condition.type === "sameLocation") {
      const personId = resolvePerson(condition.person || "target", actorId, targetId);
      return getPersonLocation(run, actorId) === getPersonLocation(run, personId);
    }
    if (condition.type === "personAt") {
      const personId = resolvePerson(condition.person, actorId, targetId);
      return getPersonLocation(run, personId) === condition.location;
    }
    if (condition.type === "itemAt") {
      return itemLocation(run, condition.item) === condition.location && !itemOwner(run, condition.item);
    }
    if (condition.type === "itemOwner") {
      const owner = resolvePerson(condition.owner, actorId, targetId);
      return itemOwner(run, condition.item) === owner;
    }
    if (condition.type === "fact") {
      return run.facts[condition.key] === condition.value;
    }
    if (condition.type === "memory") {
      const personId = resolvePerson(condition.person, actorId, targetId);
      return hasMemory(run, personId, condition.id);
    }
    if (condition.type === "relationshipAtLeast") {
      const from = resolvePerson(condition.from, actorId, targetId);
      const to = resolvePerson(condition.to, actorId, targetId);
      return relationship(run, from, to, condition.metric) >= condition.value;
    }
    if (condition.type === "relationshipAtMost") {
      const from = resolvePerson(condition.from, actorId, targetId);
      const to = resolvePerson(condition.to, actorId, targetId);
      return relationship(run, from, to, condition.metric) <= condition.value;
    }
    return false;
  }

  function actionTimeMatches(action, timeId) {
    return !action.timeIds || action.timeIds.includes(timeId);
  }

  function isActionValid(action, run, actorId) {
    const time = getTime(run);
    const targetId = action.targetId || null;
    if (action.actorIds && !action.actorIds.includes(actorId)) return false;
    if (!actionTimeMatches(action, time.id)) return false;
    if (action.locationId && getPersonLocation(run, actorId) !== action.locationId) return false;
    if (action.once !== false && (run.actionHistory || {})[`${actorId}:${action.id}`]) return false;
    if (action.slotId && (run.slotHistory || {})[`${actorId}:${action.slotId}`]) return false;
    return (action.preconditions || []).every((condition) =>
      conditionMet(condition, run, actorId, targetId)
    );
  }

  function authoredBiasFor(save, actorId, action) {
    if (!action.slotId) return 0;
    const bias = (((save.authoredBiases[actorId] || {})[action.slotId]) || {});
    return bias.actionId === action.id ? bias.bonus || 0 : 0;
  }

  function scoreAction(action, run, save, actorId) {
    let score = action.baseScore || 0;
    const targetId = action.targetId || null;
    (action.modifiers || []).forEach((modifier) => {
      if (conditionMet(modifier.condition, run, actorId, targetId)) {
        score += modifier.add || 0;
      }
    });
    score += authoredBiasFor(save, actorId, action);
    return score;
  }

  function nextStepToward(run, actorId, destinationId) {
    const start = getPersonLocation(run, actorId);
    if (!start || start === destinationId) return null;
    const queue = [{ id: start, path: [] }];
    const seen = new Set([start]);

    while (queue.length) {
      const current = queue.shift();
      const exits = DATA.locations[current.id].exits || [];
      for (const exit of exits) {
        if (seen.has(exit)) continue;
        const path = current.path.concat(exit);
        if (exit === destinationId) return path[0];
        seen.add(exit);
        queue.push({ id: exit, path });
      }
    }
    return null;
  }

  function movementActionsFor(run, save, actorId) {
    const locationId = getPersonLocation(run, actorId);
    const location = getLocation(locationId);
    const time = getTime(run);
    const isPlayer = actorId === run.playerId;
    const routineDestination = getCharacter(actorId).routine[time.id];
    const routineStep = nextStepToward(run, actorId, routineDestination);

    return (location.exits || []).map((exitId) => {
      const destination = getLocation(exitId);
      const routineBonus = exitId === routineStep ? 8 : 0;
      const baseScore = isPlayer ? -0.5 : -1;
      return {
        id: `move_${exitId}`,
        label: `Go to ${destination.name}`,
        actorIds: [actorId],
        baseScore: baseScore + routineBonus,
        tags: ["movement"],
        generated: true,
        effects: [{ type: "move", person: "actor", to: exitId }],
        text: {
          actor: `You go to the ${destination.name}.`,
          observer: `{actor} goes to the ${destination.name}.`
        }
      };
    });
  }

  function waitAction(actorId) {
    return {
      id: "wait",
      label: "Wait and watch",
      actorIds: [actorId],
      baseScore: 0,
      tags: ["careful"],
      generated: true,
      effects: [
        {
          type: "memory",
          person: "actor",
          id: `waited_${Date.now()}`,
          text: "You let this turn pass and watched the room."
        }
      ],
      text: {
        actor: "You let the moment pass and watch what the room does without your help.",
        observer: "{actor} waits and watches."
      }
    };
  }

  function validActionsFor(run, save, actorId) {
    const authored = DATA.actions.filter((action) => isActionValid(action, run, actorId));
    return authored.concat(movementActionsFor(run, save, actorId), waitAction(actorId));
  }

  function scoredActionsFor(run, save, actorId) {
    return validActionsFor(run, save, actorId)
      .map((action) => ({
        action,
        score: scoreAction(action, run, save, actorId)
      }))
      .sort((left, right) => right.score - left.score || left.action.label.localeCompare(right.action.label));
  }

  function formatText(template, run, actorId, targetId) {
    const actor = getCharacter(actorId);
    const target = targetId && getCharacter(targetId);
    return (template || "")
      .replaceAll("{actor}", actor ? actor.name : "Someone")
      .replaceAll("{target}", target ? target.name : "someone");
  }

  function eventTextFor(action, run, viewerId, actorId, targetId) {
    const text = action.text || {};
    const viewerLocation = getPersonLocation(run, viewerId);
    const actorLocation = getPersonLocation(run, actorId);
    if (viewerId === actorId && text.actor) return formatText(text.actor, run, actorId, targetId);
    if (viewerId === targetId && text.target) return formatText(text.target, run, actorId, targetId);
    if (viewerLocation === actorLocation && text.observer) {
      return formatText(text.observer, run, actorId, targetId);
    }
    return "";
  }

  function discoverTimeline(save, id) {
    if (!id || save.discoveredTimeline.includes(id)) return;
    save.discoveredTimeline.push(id);
  }

  function applyEffect(effect, run, save, actorId, targetId) {
    if (effect.type === "move") {
      const personId = resolvePerson(effect.person, actorId, targetId);
      run.people[personId].location = effect.to;
    }
    if (effect.type === "transferItem") {
      const owner = resolvePerson(effect.to, actorId, targetId);
      run.items[effect.item].owner = owner;
      run.items[effect.item].location = null;
    }
    if (effect.type === "setItemLocation") {
      run.items[effect.item].owner = null;
      run.items[effect.item].location = effect.location;
    }
    if (effect.type === "setFact") {
      run.facts[effect.key] = effect.value;
    }
    if (effect.type === "relationshipDelta") {
      const from = resolvePerson(effect.from, actorId, targetId);
      const to = resolvePerson(effect.to, actorId, targetId);
      if (from && to && from !== to) {
        const entry = ensureRelationship(run, from, to);
        entry[effect.metric] = (entry[effect.metric] || 0) + effect.delta;
      }
    }
    if (effect.type === "memory") {
      const personId = resolvePerson(effect.person, actorId, targetId);
      addMemory(run, personId, {
        id: effect.id,
        text: effect.text,
        timeId: getTime(run).id
      });
    }
    if (effect.type === "discoverTimeline") {
      discoverTimeline(save, effect.id);
    }
  }

  function applyAction(action, run, save, actorId) {
    const targetId = action.targetId || null;
    const time = getTime(run);
    const locationId = getPersonLocation(run, actorId);
    const viewerText = eventTextFor(action, run, run.playerId, actorId, targetId);
    const isVisible =
      viewerText &&
      (run.playerId === actorId ||
        run.playerId === targetId ||
        getPersonLocation(run, run.playerId) === getPersonLocation(run, actorId));

    (action.effects || []).forEach((effect) => applyEffect(effect, run, save, actorId, targetId));
    if (!action.generated) {
      run.actionHistory = run.actionHistory || {};
      run.slotHistory = run.slotHistory || {};
      run.actionHistory[`${actorId}:${action.id}`] = true;
      if (action.slotId) {
        run.slotHistory[`${actorId}:${action.slotId}`] = true;
      }
    }

    const event = {
      timeId: time.id,
      timeLabel: time.label,
      actionId: action.id,
      label: action.label,
      actorId,
      targetId,
      locationId,
      tags: action.tags || [],
      text: viewerText
    };
    run.eventLog.push(event);

    if (isVisible) {
      run.turnMessages.push({
        text: viewerText,
        private: !(action.tags || []).includes("public")
      });
    }
  }

  function recordAuthoredBias(save, run, chosenAction, scoredActions) {
    if (!chosenAction.slotId) return;
    const actorId = run.playerId;
    const best = scoredActions[0];
    const chosen = scoredActions.find((entry) => entry.action.id === chosenAction.id);
    if (!chosen) return;

    const bonus = Math.max(0, (best ? best.score : 0) - chosen.score + BIAS_MARGIN);
    save.authoredBiases[actorId] = save.authoredBiases[actorId] || {};
    save.authoredBiases[actorId][chosenAction.slotId] = {
      actionId: chosenAction.id,
      actionLabel: chosenAction.label,
      bonus,
      lastSetAt: new Date().toISOString()
    };
  }

  function chooseNpcAction(run, save, actorId) {
    const scored = scoredActionsFor(run, save, actorId);
    if (!scored.length) return null;
    const top = scored[0];
    return top.score >= 0 ? top.action : null;
  }

  function runNpcTurns(run, save) {
    Object.keys(DATA.characters).forEach((actorId) => {
      if (actorId === run.playerId || run.ended) return;
      const action = chooseNpcAction(run, save, actorId);
      if (action) applyAction(action, run, save, actorId);
    });
  }

  function endingFor(run) {
    const envelope = run.items.blueEnvelope;
    const ownerName = envelope.owner ? getCharacter(envelope.owner).name : "no one";
    if (run.facts.accusationDeflected) {
      return `The accusation breaks before it can become a verdict. The blue envelope ends the morning with ${ownerName}, and the Hall has learned to doubt its easiest story.`;
    }
    if (run.facts.jonahAccused) {
      return `By noon, Jonah stands beneath the Hall portrait with every eye on him. The blue envelope ends the morning with ${ownerName}, but suspicion has already found a body to inhabit.`;
    }
    if (envelope.owner === "fatherVale") {
      return "Father Vale keeps the blue envelope folded into his prayer book. No one is accused yet, which is not the same as anyone being safe.";
    }
    return `The day closes without a clean accusation. The blue envelope ends the morning with ${ownerName}, and the household keeps rearranging itself around that fact.`;
  }

  function completeRun(save, run) {
    run.ended = true;
    run.ending = endingFor(run);
    save.lastEnding = {
      playerId: run.playerId,
      text: run.ending
    };
    if (!save.completedRuns.includes(run.playerId)) {
      save.completedRuns.push(run.playerId);
    }
    Object.values(DATA.characters).forEach((character) => {
      if (character.unlocksAfter === run.playerId && !save.unlockedCharacters.includes(character.id)) {
        save.unlockedCharacters.push(character.id);
      }
    });
  }

  function advanceTime(save) {
    const run = save.currentRun;
    if (run.timeIndex >= DATA.timeSlots.length - 1) {
      completeRun(save, run);
      return;
    }
    run.timeIndex += 1;
  }

  function takePlayerAction(save, actionId) {
    const run = save.currentRun;
    if (!run || run.ended) return save;

    const scored = scoredActionsFor(run, save, run.playerId);
    const chosen = scored.find((entry) => entry.action.id === actionId);
    if (!chosen) return save;

    run.turnMessages = [];
    recordAuthoredBias(save, run, chosen.action, scored);
    applyAction(chosen.action, run, save, run.playerId);
    runNpcTurns(run, save);
    advanceTime(save);
    persist(save);
    return save;
  }

  function timelineFor(save) {
    const entries = new Map();
    DATA.defaultTimeline.forEach((entry) => entries.set(entry.id, entry));
    save.discoveredTimeline.forEach((id) => {
      const entry = DATA.timelineEntries[id];
      if (entry) entries.set(entry.id, entry);
    });
    const timeIndex = Object.fromEntries(DATA.timeSlots.map((slot, index) => [slot.id, index]));
    return Array.from(entries.values()).sort((left, right) => {
      return (timeIndex[left.timeId] || 0) - (timeIndex[right.timeId] || 0);
    });
  }

  function inventoryFor(run, personId) {
    return Object.values(DATA.items)
      .filter((item) => run.items[item.id].owner === personId)
      .map((item) => ({ ...item }));
  }

  function visibleItemsFor(run, locationId) {
    return Object.values(DATA.items)
      .filter((item) => run.items[item.id].location === locationId)
      .map((item) => ({ ...item }));
  }

  function peopleAt(run, locationId) {
    return Object.keys(run.people)
      .filter((id) => run.people[id].location === locationId)
      .map((id) => DATA.characters[id]);
  }

  function relationshipHighlights(run, personId) {
    const rows = [];
    Object.keys(DATA.characters).forEach((otherId) => {
      if (otherId === personId) return;
      const values = run.relationships[personId][otherId];
      const strongest = METRICS
        .map((metric) => ({ metric, value: values[metric] || 0 }))
        .filter((entry) => entry.value !== 0)
        .sort((a, b) => Math.abs(b.value) - Math.abs(a.value))[0];
      if (strongest) {
        rows.push({
          person: DATA.characters[otherId].name,
          metric: strongest.metric,
          value: strongest.value
        });
      }
    });
    return rows;
  }

  function biasesFor(save) {
    const rows = [];
    Object.entries(save.authoredBiases).forEach(([characterId, slots]) => {
      Object.entries(slots).forEach(([slotId, bias]) => {
        rows.push({
          character: DATA.characters[characterId].name,
          slotId,
          actionLabel: bias.actionLabel,
          bonus: bias.bonus
        });
      });
    });
    return rows;
  }

  function viewModel(save) {
    const run = save.currentRun;
    if (!run || run.ended) {
      return {
        mode: "start",
        save,
        characters: Object.values(DATA.characters),
        timeline: timelineFor(save),
        biases: biasesFor(save)
      };
    }

    const player = getCharacter(run.playerId);
    const locationId = getPersonLocation(run, run.playerId);
    const location = getLocation(locationId);
    return {
      mode: "run",
      save,
      run,
      player,
      time: getTime(run),
      location,
      peoplePresent: peopleAt(run, locationId),
      inventory: inventoryFor(run, run.playerId),
      visibleItems: visibleItemsFor(run, locationId),
      memories: run.memories[run.playerId],
      relationships: relationshipHighlights(run, run.playerId),
      timeline: timelineFor(save),
      biases: biasesFor(save),
      actions: scoredActionsFor(run, save, run.playerId)
    };
  }

  window.DiscontinuityEngine = {
    createSave,
    loadSave,
    persist,
    resetSave,
    startRun,
    takePlayerAction,
    viewModel,
    data: DATA
  };
})();
