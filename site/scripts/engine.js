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
  const FUDGE_MARGIN = 3;
  const MAX_FUDGE = 9;
  const MIN_FUDGE = 1.5;

  function clone(value) {
    return JSON.parse(JSON.stringify(value));
  }

  function createSave() {
    return {
      version: 1,
      unlockedCharacters: ["clara"],
      completedRuns: [],
      authoredBiases: {},
      behaviorFudges: {},
      discoveredTimeline: [],
      currentRun: null,
      lastEnding: null
    };
  }

  function loadSave() {
    try {
      const raw = window.localStorage.getItem(DATA.storageKey);
      if (!raw) return createSave();
      return normalizeSave({ ...createSave(), ...JSON.parse(raw) });
    } catch (error) {
      console.warn("Could not load save.", error);
      return createSave();
    }
  }

  function normalizeSave(save) {
    save.behaviorFudges = save.behaviorFudges || {};
    save.authoredBiases = save.authoredBiases || {};

    Object.entries(save.authoredBiases).forEach(([actorId, slots]) => {
      if (!slots || save.behaviorFudges[actorId]) return;
      save.behaviorFudges[actorId] = {};
      Object.entries(slots).forEach(([slotId, bias]) => {
        const action = DATA.actions.find((entry) => entry.id === bias.actionId);
        if (!action) return;
        save.behaviorFudges[actorId][slotId] = {
          actionId: action.id,
          actionLabel: action.label,
          amount: bias.bonus || FUDGE_MARGIN,
          startsAt: firstActionTime(action),
          locationId: action.locationId || null,
          targetId: action.targetId || null,
          tags: action.tags || [],
          lastSetAt: bias.lastSetAt || new Date().toISOString()
        };
      });
    });

    return save;
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

  function timeIndexOf(timeId) {
    const index = DATA.timeSlots.findIndex((slot) => slot.id === timeId);
    return index === -1 ? 0 : index;
  }

  function firstActionTime(action) {
    if (action.timeWindow && action.timeWindow.start) return action.timeWindow.start;
    if (action.timeIds && action.timeIds.length) return action.timeIds[0];
    return DATA.timeSlots[0].id;
  }

  function actionWindow(action) {
    if (action.timeWindow) {
      return {
        start: action.timeWindow.start || DATA.timeSlots[0].id,
        end: action.timeWindow.end || DATA.timeSlots[DATA.timeSlots.length - 1].id
      };
    }
    if (action.timeIds && action.timeIds.length) {
      return {
        start: action.timeIds[0],
        end: action.timeIds[action.timeIds.length - 1]
      };
    }
    return {
      start: DATA.timeSlots[0].id,
      end: DATA.timeSlots[DATA.timeSlots.length - 1].id
    };
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
    if (action.timeIds && action.timeIds.includes(timeId)) return true;
    if (!action.timeWindow) return !action.timeIds;
    const index = timeIndexOf(timeId);
    return index >= timeIndexOf(action.timeWindow.start) && index <= timeIndexOf(action.timeWindow.end);
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

  function activeFudgesFor(save, run, actorId) {
    const currentIndex = run ? run.timeIndex : 0;
    return Object.values((save.behaviorFudges || {})[actorId] || {}).filter((fudge) => {
      return currentIndex >= timeIndexOf(fudge.startsAt);
    });
  }

  function behaviorFudgeFor(save, run, actorId, action) {
    let total = 0;
    activeFudgesFor(save, run, actorId).forEach((fudge) => {
      if (fudge.actionId === action.id) {
        total += fudge.amount || 0;
        return;
      }
      if (fudge.targetId && action.targetId === fudge.targetId) {
        total += (fudge.amount || 0) * 0.22;
      }
      const overlap = (action.tags || []).filter((tag) => (fudge.tags || []).includes(tag)).length;
      if (overlap) {
        total += Math.min(1.5, overlap * 0.35);
      }
      if (fudge.locationId && action.locationId === fudge.locationId) {
        total += 0.5;
      }
    });
    return total;
  }

  function contextualScoreFor(action, run, actorId) {
    if (action.generated) return 0;

    let score = 0;
    const targetId = action.targetId || null;
    const locationId = getPersonLocation(run, actorId);
    const peopleHere = peopleAt(run, locationId);
    const otherPeopleCount = Math.max(0, peopleHere.length - 1);
    const tags = action.tags || [];
    const window = actionWindow(action);
    const currentIndex = run.timeIndex;
    const startIndex = timeIndexOf(window.start);
    const endIndex = timeIndexOf(window.end);

    if (currentIndex >= startIndex && currentIndex <= endIndex) {
      score += Math.min(2.4, Math.max(0, currentIndex - startIndex) * 0.35);
      if (endIndex - currentIndex <= 1) score += 1.2;
    }

    if (targetId && getPersonLocation(run, targetId) === locationId) {
      score += 1.4;
    }

    if (tags.includes("private") && otherPeopleCount > 1) score -= 1.3;
    if ((tags.includes("risky") || tags.includes("suspicious")) && otherPeopleCount > 0) {
      score -= Math.min(2.2, otherPeopleCount * 0.55);
    }
    if (tags.includes("public") && tags.includes("humiliating")) {
      score += Math.min(2.4, otherPeopleCount * 0.6);
    }
    if (tags.includes("public") && tags.includes("helpful")) {
      score += Math.min(1.2, otherPeopleCount * 0.3);
    }

    if (targetId) {
      const towardTarget = (metric) => relationship(run, actorId, targetId, metric);
      if (tags.includes("kind") || tags.includes("helpful")) {
        score += towardTarget("trust") * 0.45;
        score += towardTarget("gratitude") * 0.75;
        score += towardTarget("protectiveness") * 0.65;
        score -= towardTarget("resentment") * 0.55;
      }
      if (tags.includes("cruel") || tags.includes("threatening") || tags.includes("confrontational")) {
        score += towardTarget("resentment") * 0.7;
        score += towardTarget("suspicion") * 0.45;
        score -= towardTarget("gratitude") * 0.55;
        score -= towardTarget("guilt") * 0.4;
      }
      if (tags.includes("suspicious")) {
        score += towardTarget("suspicion") * 0.35;
      }
    }

    return score;
  }

  function scoreAction(action, run, save, actorId, options = {}) {
    const includeFudges = options.includeFudges !== false;
    let score = action.baseScore || 0;
    const targetId = action.targetId || null;
    (action.modifiers || []).forEach((modifier) => {
      if (conditionMet(modifier.condition, run, actorId, targetId)) {
        score += modifier.add || 0;
      }
    });
    score += contextualScoreFor(action, run, actorId);
    if (includeFudges) score += behaviorFudgeFor(save, run, actorId, action);
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
      const routineBonus = exitId === routineStep ? 2.2 : 0;
      const opportunityBonus = movementOpportunityScore(run, save, actorId, exitId);
      const baseScore = isPlayer ? -0.25 : -0.7;
      return {
        id: `move_${exitId}`,
        label: `Go to ${destination.name}`,
        actorIds: [actorId],
        baseScore: baseScore + routineBonus + opportunityBonus,
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

  function movementOpportunityScore(run, save, actorId, exitId) {
    let score = 0;
    const currentLocation = getPersonLocation(run, actorId);

    DATA.actions.forEach((action) => {
      if (action.generated || (action.actorIds && !action.actorIds.includes(actorId))) return;
      if (!actionTimeMatches(action, getTime(run).id)) return;
      if (action.once !== false && (run.actionHistory || {})[`${actorId}:${action.id}`]) return;
      if (action.slotId && (run.slotHistory || {})[`${actorId}:${action.slotId}`]) return;
      if (!action.locationId) return;

      const step = nextStepToward(run, actorId, action.locationId);
      if (step !== exitId && action.locationId !== exitId) return;

      let actionPull = Math.max(0, (action.baseScore || 0) * 0.18);
      const targetId = action.targetId || null;
      if (targetId && getPersonLocation(run, targetId) === action.locationId) actionPull += 1.2;
      if (action.preconditions || []) {
        actionPull += (action.preconditions || []).some((condition) => {
          return condition.type === "itemAt" && itemLocation(run, condition.item) === action.locationId;
        })
          ? 1.2
          : 0;
      }
      if (currentLocation !== action.locationId) {
        actionPull += behaviorFudgeFor(save, run, actorId, action) * 0.45;
      }
      score += Math.min(4, actionPull);
    });

    Object.keys(DATA.characters).forEach((otherId) => {
      if (otherId === actorId) return;
      const otherLocation = getPersonLocation(run, otherId);
      if (otherLocation === currentLocation) return;
      if (nextStepToward(run, actorId, otherLocation) !== exitId) return;
      const tension = Math.abs(relationship(run, actorId, otherId, "resentment"));
      const trust = relationship(run, actorId, otherId, "trust") + relationship(run, actorId, otherId, "gratitude");
      score += Math.min(1.5, (tension + Math.max(0, trust)) * 0.2);
    });

    return Math.min(5.5, score);
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

  function scoredActionsFor(run, save, actorId, options = {}) {
    return validActionsFor(run, save, actorId)
      .map((action) => ({
        action,
        score: scoreAction(action, run, save, actorId, options)
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

  function recordBehaviorFudge(save, run, chosenAction, baselineScores) {
    if (chosenAction.generated) return;
    const actorId = run.playerId;
    const key = chosenAction.slotId || chosenAction.id;
    const best = baselineScores[0];
    const chosen = baselineScores.find((entry) => entry.action.id === chosenAction.id);
    if (!chosen) return;

    save.behaviorFudges[actorId] = save.behaviorFudges[actorId] || {};
    if (best && best.action.id === chosenAction.id) {
      delete save.behaviorFudges[actorId][key];
      return;
    }

    const amount = Math.min(
      MAX_FUDGE,
      Math.max(MIN_FUDGE, (best ? best.score : 0) - chosen.score + FUDGE_MARGIN)
    );
    save.behaviorFudges[actorId][key] = {
      actionId: chosenAction.id,
      actionLabel: chosenAction.label,
      amount,
      startsAt: getTime(run).id,
      locationId: chosenAction.locationId || getPersonLocation(run, actorId),
      targetId: chosenAction.targetId || null,
      tags: chosenAction.tags || [],
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
    const baselineScores = scoredActionsFor(run, save, run.playerId, { includeFudges: false });
    const chosen = scored.find((entry) => entry.action.id === actionId);
    if (!chosen) return save;

    run.turnMessages = [];
    recordBehaviorFudge(save, run, chosen.action, baselineScores);
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

  function viewModel(save) {
    const run = save.currentRun;
    if (!run || run.ended) {
      return {
        mode: "start",
        save,
        characters: Object.values(DATA.characters),
        timeline: timelineFor(save)
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
      relationships: relationshipHighlights(run, run.playerId),
      timeline: timelineFor(save),
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
