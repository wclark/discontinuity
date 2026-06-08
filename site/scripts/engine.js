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
    if (save.currentRun && !save.currentRun.runId) {
      save.currentRun.runId = `${save.currentRun.playerId || "run"}-${Date.now()}`;
    }

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

  function displayName(kind, id) {
    if (!id) return "";
    if (id === "actor") return "actor";
    if (id === "target") return "target";
    if (kind === "person") return (DATA.characters[id] && DATA.characters[id].name) || id;
    if (kind === "location") return (DATA.locations[id] && DATA.locations[id].name) || id;
    if (kind === "item") return (DATA.items[id] && DATA.items[id].name) || id;
    return id;
  }

  function conditionLabel(condition) {
    if (!condition) return "always";
    if (condition.type === "not") return `not (${conditionLabel(condition.condition)})`;
    if (condition.type === "all") return (condition.conditions || []).map(conditionLabel).join(" and ");
    if (condition.type === "any") return (condition.conditions || []).map(conditionLabel).join(" or ");
    if (condition.type === "sameLocation") {
      return `${displayName("person", condition.person || "target")} present`;
    }
    if (condition.type === "personAt") {
      return `${displayName("person", condition.person)} at ${displayName("location", condition.location)}`;
    }
    if (condition.type === "itemAt") {
      return `${displayName("item", condition.item)} at ${displayName("location", condition.location)}`;
    }
    if (condition.type === "itemOwner") {
      return `${displayName("item", condition.item)} held by ${displayName("person", condition.owner)}`;
    }
    if (condition.type === "fact") {
      return `${condition.key} is ${String(condition.value)}`;
    }
    if (condition.type === "timeAtLeast") return `time at/after ${condition.time}`;
    if (condition.type === "timeAtMost") return `time at/before ${condition.time}`;
    if (condition.type === "timeBetween") return `time ${condition.start}-${condition.end}`;
    if (condition.type === "memory") {
      return `${displayName("person", condition.person)} has ${condition.id}`;
    }
    if (condition.type === "relationshipAtLeast") {
      return `${condition.metric} ${displayName("person", condition.from)}->${displayName("person", condition.to)} >= ${condition.value}`;
    }
    if (condition.type === "relationshipAtMost") {
      return `${condition.metric} ${displayName("person", condition.from)}->${displayName("person", condition.to)} <= ${condition.value}`;
    }
    return condition.type || "condition";
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
      runId: `${playerId}-${Date.now()}`,
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
    save.behaviorFudges = save.behaviorFudges || {};
    save.authoredBiases = save.authoredBiases || {};
    save.behaviorFudges[playerId] = {};
    save.authoredBiases[playerId] = {};
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
    if (condition.type === "timeAtLeast") {
      return run.timeIndex >= timeIndexOf(condition.time);
    }
    if (condition.type === "timeAtMost") {
      return run.timeIndex <= timeIndexOf(condition.time);
    }
    if (condition.type === "timeBetween") {
      return run.timeIndex >= timeIndexOf(condition.start) && run.timeIndex <= timeIndexOf(condition.end);
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
    const actorLocation = getPersonLocation(run, actorId);
    const matches = activeFudgesFor(save, run, actorId)
      .filter((fudge) => {
        if (fudge.createdRunId && fudge.createdRunId === run.runId) return false;
        if (fudge.actionId !== action.id) return false;
        if (fudge.locationId && actorLocation !== fudge.locationId) return false;
        if (fudge.targetId && action.targetId !== fudge.targetId) return false;
        return true;
      })
      .sort((left, right) => {
        return (
          timeIndexOf(right.startsAt) - timeIndexOf(left.startsAt) ||
          (right.amount || 0) - (left.amount || 0)
        );
      });
    return matches.length ? matches[0].amount || 0 : 0;
  }

  function goalDefinitionsFor(actorId) {
    return (DATA.goals || []).filter((goal) => {
      return !goal.actorIds || goal.actorIds.includes(actorId);
    });
  }

  function stepSatisfied(step, run, actorId) {
    if (step.satisfied) return conditionMet(step.satisfied, run, actorId, step.targetId || null);
    if (step.type === "goTo") return getPersonLocation(run, actorId) === step.location;
    return false;
  }

  function goalSteps(goal, run, actorId) {
    let foundCurrent = false;
    return (goal.instructions || []).map((step) => {
      const satisfied = stepSatisfied(step, run, actorId);
      const isCurrent = !satisfied && !foundCurrent;
      if (isCurrent) foundCurrent = true;
      return {
        ...step,
        status: satisfied ? "satisfied" : isCurrent ? "current" : "pending"
      };
    });
  }

  function goalAdjustments(goal, run, actorId) {
    return (goal.adjustments || []).map((adjustment) => {
      const conditions = (adjustment.conditions || []).map((condition) => {
        return {
          label: conditionLabel(condition),
          active: conditionMet(condition, run, actorId, adjustment.targetId || null)
        };
      });
      return {
        label: adjustment.label || adjustment.actionId,
        actionId: adjustment.actionId,
        amount: adjustment.amount || 0,
        active: conditions.every((condition) => condition.active),
        conditions
      };
    });
  }

  function scoredGoalsFor(run, save, actorId) {
    return goalDefinitionsFor(actorId)
      .map((goal) => {
        const instructions = goalSteps(goal, run, actorId);
        const adjustments = goalAdjustments(goal, run, actorId);
        const currentInstruction = instructions.find((step) => step.status === "current") || null;
        const satisfied = goal.completion
          ? conditionMet(goal.completion, run, actorId, goal.targetId || null)
          : !currentInstruction;
        const score = adjustments.reduce((total, adjustment) => {
          return total + (adjustment.active ? adjustment.amount || 0 : 0);
        }, 0);
        const status = satisfied ? "satisfied" : score > 0 ? "active" : "dormant";
        return {
          id: goal.id,
          label: goal.label,
          score,
          status,
          instructions,
          adjustments,
          currentInstruction
        };
      })
      .sort((left, right) => {
        const order = { active: 0, dormant: 1, satisfied: 2 };
        return (
          order[left.status] - order[right.status] ||
          right.score - left.score ||
          left.label.localeCompare(right.label)
        );
      });
  }

  function conditionBonusesFor(action) {
    return action.conditionBonuses || action.modifiers || [];
  }

  function conditionBonusPartsFor(action, run, actorId) {
    const targetId = action.targetId || null;
    return conditionBonusesFor(action).map((bonus) => {
      const conditions = bonus.conditions || (bonus.condition ? [bonus.condition] : []);
      const active = conditions.every((condition) => conditionMet(condition, run, actorId, targetId));
      return {
        label: bonus.label || conditions.map(conditionLabel).join(" + ") || "condition",
        value: active ? bonus.amount ?? bonus.add ?? 0 : 0,
        kind: "condition",
        active,
        conditions: conditions.map((condition) => ({
          label: conditionLabel(condition),
          active: conditionMet(condition, run, actorId, targetId)
        }))
      };
    });
  }

  function choiceBonusPartsFor(action, run, save, actorId) {
    const parts = [];
    goalDefinitionsFor(actorId).forEach((goal) => {
      if (goal.completion && conditionMet(goal.completion, run, actorId, goal.targetId || null)) return;
      goalAdjustments(goal, run, actorId).forEach((adjustment) => {
        if (!adjustment.active || adjustment.actionId !== action.id) return;
        parts.push({
          label: adjustment.label || goal.label || adjustment.actionId,
          value: adjustment.amount || 0,
          kind: "condition",
          active: true,
          conditions: adjustment.conditions
        });
      });
    });
    return parts;
  }

  function scorePartsFor(action, run, save, actorId, options = {}) {
    const includeFudges = options.includeFudges !== false;
    const includeConditions = options.includeConditions !== false;
    const parts = [];
    if (includeConditions) {
      parts.push(...conditionBonusPartsFor(action, run, actorId));
      parts.push(...choiceBonusPartsFor(action, run, save, actorId));
    }
    if (includeFudges) {
      parts.push({ label: "manual", value: behaviorFudgeFor(save, run, actorId, action), kind: "manual" });
    }
    return parts;
  }

  function sumScoreParts(parts) {
    return parts.reduce((total, part) => total + (part.value || 0), 0);
  }

  function scoreAction(action, run, save, actorId, options = {}) {
    return sumScoreParts(scorePartsFor(action, run, save, actorId, options));
  }

  function movementActionsFor(run, save, actorId, options = {}) {
    const locationId = getPersonLocation(run, actorId);
    const location = getLocation(locationId);

    return (location.exits || []).map((exitId) => {
      const destination = getLocation(exitId);
      return {
        id: `move_${exitId}`,
        label: `Go to ${destination.name}`,
        actorIds: [actorId],
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

  function validActionsFor(run, save, actorId, options = {}) {
    const authored = DATA.actions.filter((action) => isActionValid(action, run, actorId));
    return authored.concat(movementActionsFor(run, save, actorId, options), waitAction(actorId));
  }

  function scoredActionsFor(run, save, actorId, options = {}) {
    return validActionsFor(run, save, actorId, options)
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

  function behaviorFudgeKey(action, run, actorId) {
    if (action.slotId) return action.slotId;
    if (action.generated) {
      return `${getTime(run).id}:${getPersonLocation(run, actorId)}:${action.id}`;
    }
    return action.id;
  }

  function recordBehaviorFudge(save, run, chosenAction, baselineScores) {
    const actorId = run.playerId;
    const key = behaviorFudgeKey(chosenAction, run, actorId);
    const best = baselineScores[0];
    const chosen = baselineScores.find((entry) => entry.action.id === chosenAction.id);
    if (!chosen) return;
    if (best && chosen.score >= best.score - 0.0001) return;

    save.behaviorFudges[actorId] = save.behaviorFudges[actorId] || {};

    const amount = Math.max(MIN_FUDGE, (best ? best.score : chosen.score) - chosen.score + FUDGE_MARGIN);
    save.behaviorFudges[actorId][key] = {
      actionId: chosenAction.id,
      actionLabel: chosenAction.label,
      amount,
      startsAt: getTime(run).id,
      locationId: chosenAction.locationId || getPersonLocation(run, actorId),
      targetId: chosenAction.targetId || null,
      tags: chosenAction.tags || [],
      createdRunId: run.runId || null,
      lastSetAt: new Date().toISOString()
    };
  }

  function chooseNpcAction(run, save, actorId) {
    const scored = scoredActionsFor(run, save, actorId);
    if (!scored.length) return null;
    const top = scored[0];
    return top.score > 0 ? top.action : null;
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

  function manualAdjustmentsFor(save, run, actorId) {
    return Object.entries((save.behaviorFudges || {})[actorId] || {})
      .map(([key, adjustment]) => {
        const location = adjustment.locationId ? getLocation(adjustment.locationId) : null;
        const startsAt = DATA.timeSlots.find((slot) => slot.id === adjustment.startsAt);
        return {
          key,
          actionId: adjustment.actionId,
          actionLabel: adjustment.actionLabel || adjustment.actionId,
          amount: adjustment.amount || 0,
          startsAt: adjustment.startsAt,
          startsAtLabel: startsAt ? startsAt.label : adjustment.startsAt,
          location: location ? location.name : null,
          active:
            run.timeIndex >= timeIndexOf(adjustment.startsAt) &&
            adjustment.createdRunId !== run.runId
        };
      })
      .sort((left, right) => {
        return (
          timeIndexOf(left.startsAt) - timeIndexOf(right.startsAt) ||
          left.actionLabel.localeCompare(right.actionLabel)
        );
      });
  }

  function decisionDebugFor(run, save) {
    return Object.keys(DATA.characters).map((actorId) => {
      const locationId = getPersonLocation(run, actorId);
      const baseline = new Map(
        scoredActionsFor(run, save, actorId, { includeFudges: false }).map((entry) => [
          entry.action.id,
          entry.score
        ])
      );
      const options = scoredActionsFor(run, save, actorId).map((entry, index) => {
        const scoreParts = scorePartsFor(entry.action, run, save, actorId);
        const conditionScore = sumScoreParts(scoreParts.filter((part) => part.kind === "condition"));
        const manualScore = sumScoreParts(scoreParts.filter((part) => part.kind === "manual"));
        const baselineScore = baseline.has(entry.action.id) ? baseline.get(entry.action.id) : entry.score;
        return {
          rank: index + 1,
          id: entry.action.id,
          label: entry.action.label,
          score: entry.score,
          conditionScore,
          manualScore,
          adjustment: entry.score - baselineScore,
          scoreParts: scoreParts.filter((part) => part.kind === "condition" && Math.abs(part.value || 0) >= 0.05),
          tags: entry.action.tags || []
        };
      });
      const availableActionIds = new Set(options.map((option) => option.id));
      const goals = scoredGoalsFor(run, save, actorId).map((goal) => {
        const adjustments = goal.adjustments.map((adjustment) => ({
          ...adjustment,
          available: availableActionIds.has(adjustment.actionId),
          active: adjustment.active && availableActionIds.has(adjustment.actionId)
        }));
        const score = adjustments.reduce((total, adjustment) => {
          return total + (adjustment.active ? adjustment.amount || 0 : 0);
        }, 0);
        return {
          ...goal,
          score,
          status: goal.status === "satisfied" ? "satisfied" : score > 0 ? "active" : "dormant",
          adjustments
        };
      });
      return {
        characterId: actorId,
        name: DATA.characters[actorId].name,
        role: DATA.characters[actorId].role,
        isPlayer: actorId === run.playerId,
        location: getLocation(locationId).name,
        topGoal: goals[0] || null,
        goals,
        manualAdjustments: manualAdjustmentsFor(save, run, actorId),
        options
      };
    });
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
      actions: scoredActionsFor(run, save, run.playerId),
      decisionDebug: decisionDebugFor(run, save)
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
