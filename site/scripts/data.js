(function () {
  window.DISCONTINUITY_DATA = {
    storageKey: "discontinuity-save-v1",
    title: "Discontinuity",
    finalTimeId: "1115",
    timeSlots: [
      { id: "0800", label: "8:00" },
      { id: "0815", label: "8:15" },
      { id: "0830", label: "8:30" },
      { id: "0845", label: "8:45" },
      { id: "0900", label: "9:00" },
      { id: "0915", label: "9:15" },
      { id: "0930", label: "9:30" },
      { id: "0945", label: "9:45" },
      { id: "1000", label: "10:00" },
      { id: "1015", label: "10:15" },
      { id: "1030", label: "10:30" },
      { id: "1045", label: "10:45" },
      { id: "1100", label: "11:00" },
      { id: "1115", label: "11:15" }
    ],
    locations: {
      hall: {
        id: "hall",
        name: "Hall",
        crowd: "mourners and house staff",
        image: "assets/locations/hall.svg",
        description:
          "The old hall concentrates every whisper in the house. Wet coats steam near the stair, and the black-draped portrait over the hearth seems to listen.",
        exits: ["kitchen", "chapel", "archive", "garden"]
      },
      kitchen: {
        id: "kitchen",
        name: "Kitchen",
        crowd: "servants",
        image: "assets/locations/kitchen.svg",
        description:
          "Copper pans breathe heat into the narrow kitchen. Orders arrive as scraps of gossip, then leave disguised as trays.",
        exits: ["hall", "garden"]
      },
      chapel: {
        id: "chapel",
        name: "Chapel",
        crowd: "mourners",
        image: "assets/locations/chapel.svg",
        description:
          "Candles gutter beneath a vaulted ceiling. In the back pews, grief and calculation sit shoulder to shoulder.",
        exits: ["hall", "garden"]
      },
      archive: {
        id: "archive",
        name: "Archive",
        crowd: "dust and locked drawers",
        image: "assets/locations/archive.svg",
        description:
          "Bundles of estate papers lean in numbered shelves. The center desk is clean except for the objects everyone pretends not to need.",
        exits: ["hall"]
      },
      garden: {
        id: "garden",
        name: "Garden",
        crowd: "townspeople beyond the wall",
        image: "assets/locations/garden.svg",
        description:
          "Rain beads on the box hedges. The garden is public enough for safety and private enough for a dangerous promise.",
        exits: ["hall", "kitchen", "chapel"]
      }
    },
    characters: {
      clara: {
        id: "clara",
        name: "Clara",
        role: "maid",
        color: "#792d31",
        startLocation: "kitchen",
        unlocksAfter: "start",
        motive: "Keep her place, protect her brother, and learn why the blue envelope matters.",
        routine: {
          "0800": "hall",
          "0815": "hall",
          "0830": "hall",
          "0845": "hall",
          "0900": "hall",
          "0915": "hall",
          "0930": "chapel",
          "0945": "archive",
          "1000": "archive",
          "1015": "hall",
          "1030": "hall",
          "1045": "kitchen",
          "1100": "hall",
          "1115": "hall"
        }
      },
      jonah: {
        id: "jonah",
        name: "Jonah",
        role: "printer's apprentice",
        color: "#2c5f86",
        startLocation: "archive",
        unlocksAfter: "clara",
        motive: "Leave with his dignity intact and keep the printer's errand from becoming evidence.",
        routine: {
          "0800": "archive",
          "0815": "archive",
          "0830": "archive",
          "0845": "hall",
          "0900": "hall",
          "0915": "hall",
          "0930": "garden",
          "0945": "chapel",
          "1000": "hall",
          "1015": "hall",
          "1030": "hall",
          "1045": "archive",
          "1100": "hall",
          "1115": "hall"
        }
      },
      fatherVale: {
        id: "fatherVale",
        name: "Father Vale",
        role: "priest",
        color: "#516851",
        startLocation: "chapel",
        unlocksAfter: "jonah",
        motive: "Keep a confession sealed while steering blame away from the parish.",
        routine: {
          "0800": "chapel",
          "0815": "chapel",
          "0830": "hall",
          "0845": "archive",
          "0900": "archive",
          "0915": "chapel",
          "0930": "chapel",
          "0945": "hall",
          "1000": "chapel",
          "1015": "hall",
          "1030": "hall",
          "1045": "archive",
          "1100": "hall",
          "1115": "hall"
        }
      },
      doctorMerrow: {
        id: "doctorMerrow",
        name: "Dr. Merrow",
        role: "physician",
        color: "#ad7b39",
        startLocation: "garden",
        unlocksAfter: "fatherVale",
        motive: "Prevent a public disgrace without exposing the patient who confessed too much.",
        routine: {
          "0800": "garden",
          "0815": "kitchen",
          "0830": "garden",
          "0845": "hall",
          "0900": "garden",
          "0915": "hall",
          "0930": "garden",
          "0945": "hall",
          "1000": "garden",
          "1015": "hall",
          "1030": "hall",
          "1045": "hall",
          "1100": "hall",
          "1115": "hall"
        }
      }
    },
    items: {
      blueEnvelope: {
        id: "blueEnvelope",
        name: "blue envelope",
        startLocation: "archive",
        startOwner: null,
        description: "Heavy blue paper, sealed in black wax, addressed in a practiced hand."
      },
      ledger: {
        id: "ledger",
        name: "estate ledger",
        startLocation: "archive",
        startOwner: null,
        description: "A red-backed account book with three pages cut loose near the end."
      },
      laudanum: {
        id: "laudanum",
        name: "laudanum vial",
        startLocation: null,
        startOwner: "doctorMerrow",
        description: "A small amber vial with Dr. Merrow's thumbprint on the cork."
      },
      chapelKey: {
        id: "chapelKey",
        name: "chapel key",
        startLocation: null,
        startOwner: "fatherVale",
        description: "A brass key worn smooth around the bow."
      }
    },
    goals: [
      {
        id: "clara_hear_about_envelope",
        label: "Learn why the envelope matters",
        actorIds: ["clara"],
        baseScore: 0,
        activeThreshold: 3,
        variables: [
          { label: "morning kitchen position", weight: 2, condition: { type: "personAt", person: "actor", location: "kitchen" } },
          { label: "before memorial begins", weight: 2, condition: { type: "timeAtMost", time: "0845" } },
          { label: "not yet informed", weight: 2, condition: { type: "not", condition: { type: "memory", person: "actor", id: "heard_vale_needs_envelope" } } }
        ],
        adjustments: [
          {
            label: "listen while in the kitchen",
            actionId: "clara_listen_pantry",
            amount: 5,
            conditions: [
              { type: "personAt", person: "actor", location: "kitchen" },
              { type: "timeAtMost", time: "0845" },
              { type: "not", condition: { type: "memory", person: "actor", id: "heard_vale_needs_envelope" } }
            ]
          }
        ],
        instructions: [
          {
            type: "action",
            actionId: "clara_listen_pantry",
            label: "Listen at the pantry door",
            satisfied: { type: "memory", person: "actor", id: "heard_vale_needs_envelope" }
          }
        ],
        completion: { type: "memory", person: "actor", id: "heard_vale_needs_envelope" }
      },
      {
        id: "clara_get_envelope_first",
        label: "Reach the Archive before Vale",
        actorIds: ["clara"],
        baseScore: 0,
        activeThreshold: 7,
        variables: [
          { label: "heard Vale wants it", weight: 5, condition: { type: "memory", person: "actor", id: "heard_vale_needs_envelope" } },
          { label: "envelope still in Archive", weight: 4, condition: { type: "itemAt", item: "blueEnvelope", location: "archive" } },
          { label: "Archive errand window", weight: 2, condition: { type: "timeBetween", start: "0845", end: "1015" } }
        ],
        adjustments: [
          {
            label: "leave Kitchen toward Archive",
            actionId: "move_hall",
            amount: 3,
            conditions: [
              { type: "personAt", person: "actor", location: "kitchen" },
              { type: "memory", person: "actor", id: "heard_vale_needs_envelope" },
              { type: "itemAt", item: "blueEnvelope", location: "archive" },
              { type: "timeBetween", start: "0845", end: "1015" }
            ]
          },
          {
            label: "enter Archive from Hall",
            actionId: "move_archive",
            amount: 5,
            conditions: [
              { type: "personAt", person: "actor", location: "hall" },
              { type: "memory", person: "actor", id: "heard_vale_needs_envelope" },
              { type: "itemAt", item: "blueEnvelope", location: "archive" },
              { type: "timeBetween", start: "0845", end: "1015" }
            ]
          },
          {
            label: "take envelope while alone enough to try",
            actionId: "clara_hide_envelope",
            amount: 6,
            conditions: [
              { type: "personAt", person: "actor", location: "archive" },
              { type: "memory", person: "actor", id: "heard_vale_needs_envelope" },
              { type: "itemAt", item: "blueEnvelope", location: "archive" },
              { type: "timeBetween", start: "0845", end: "1015" }
            ]
          }
        ],
        instructions: [
          { type: "goTo", location: "archive", label: "Travel to the Archive" },
          {
            type: "action",
            actionId: "clara_hide_envelope",
            label: "Take the blue envelope",
            satisfied: { type: "itemOwner", item: "blueEnvelope", owner: "actor" }
          }
        ],
        completion: { type: "itemOwner", item: "blueEnvelope", owner: "actor" }
      },
      {
        id: "jonah_finish_printer_errand",
        label: "Finish the printer's errand",
        actorIds: ["jonah"],
        baseScore: 0,
        activeThreshold: 5,
        variables: [
          { label: "printer's early window", weight: 5, condition: { type: "timeBetween", start: "0800", end: "0900" } },
          { label: "envelope still readable", weight: 3, condition: { type: "itemAt", item: "blueEnvelope", location: "archive" } },
          { label: "address not copied yet", weight: 2, condition: { type: "not", condition: { type: "memory", person: "actor", id: "copied_envelope_address" } } }
        ],
        adjustments: [
          {
            label: "return to Archive if pulled away early",
            actionId: "move_archive",
            amount: 4,
            conditions: [
              { type: "personAt", person: "actor", location: "hall" },
              { type: "timeBetween", start: "0800", end: "0900" },
              { type: "itemAt", item: "blueEnvelope", location: "archive" },
              { type: "not", condition: { type: "memory", person: "actor", id: "copied_envelope_address" } }
            ]
          },
          {
            label: "copy address when envelope is present",
            actionId: "jonah_copy_address",
            amount: 5,
            conditions: [
              { type: "personAt", person: "actor", location: "archive" },
              { type: "timeBetween", start: "0800", end: "0900" },
              { type: "itemAt", item: "blueEnvelope", location: "archive" },
              { type: "not", condition: { type: "memory", person: "actor", id: "copied_envelope_address" } }
            ]
          }
        ],
        instructions: [
          { type: "goTo", location: "archive", label: "Travel to the Archive" },
          {
            type: "action",
            actionId: "jonah_copy_address",
            label: "Copy the envelope address",
            satisfied: { type: "memory", person: "actor", id: "copied_envelope_address" }
          }
        ],
        completion: { type: "memory", person: "actor", id: "copied_envelope_address" }
      },
      {
        id: "father_secure_envelope",
        label: "Secure the blue envelope",
        actorIds: ["fatherVale"],
        baseScore: 0,
        activeThreshold: 8,
        variables: [
          { label: "safekeeping window opened", weight: 5, condition: { type: "timeAtLeast", time: "0845" } },
          { label: "envelope still on Archive desk", weight: 5, condition: { type: "itemAt", item: "blueEnvelope", location: "archive" } },
          { label: "not already carrying it", weight: 1, condition: { type: "not", condition: { type: "itemOwner", item: "blueEnvelope", owner: "actor" } } }
        ],
        adjustments: [
          {
            label: "leave Chapel toward Archive",
            actionId: "move_hall",
            amount: 4,
            conditions: [
              { type: "personAt", person: "actor", location: "chapel" },
              { type: "timeAtLeast", time: "0845" },
              { type: "itemAt", item: "blueEnvelope", location: "archive" },
              { type: "not", condition: { type: "itemOwner", item: "blueEnvelope", owner: "actor" } }
            ]
          },
          {
            label: "leave Garden toward Archive",
            actionId: "move_hall",
            amount: 4,
            conditions: [
              { type: "personAt", person: "actor", location: "garden" },
              { type: "timeAtLeast", time: "0845" },
              { type: "itemAt", item: "blueEnvelope", location: "archive" },
              { type: "not", condition: { type: "itemOwner", item: "blueEnvelope", owner: "actor" } }
            ]
          },
          {
            label: "enter Archive from Hall",
            actionId: "move_archive",
            amount: 5.5,
            conditions: [
              { type: "personAt", person: "actor", location: "hall" },
              { type: "timeAtLeast", time: "0845" },
              { type: "itemAt", item: "blueEnvelope", location: "archive" },
              { type: "not", condition: { type: "itemOwner", item: "blueEnvelope", owner: "actor" } }
            ]
          },
          {
            label: "take envelope at Archive",
            actionId: "father_take_envelope",
            amount: 6,
            conditions: [
              { type: "personAt", person: "actor", location: "archive" },
              { type: "timeAtLeast", time: "0845" },
              { type: "itemAt", item: "blueEnvelope", location: "archive" }
            ]
          }
        ],
        instructions: [
          { type: "goTo", location: "archive", label: "Travel to the Archive" },
          {
            type: "action",
            actionId: "father_take_envelope",
            label: "Remove the envelope for safekeeping",
            satisfied: { type: "itemOwner", item: "blueEnvelope", owner: "actor" }
          }
        ],
        completion: { type: "itemOwner", item: "blueEnvelope", owner: "actor" }
      },
      {
        id: "father_return_envelope_to_chapel",
        label: "Carry the envelope to the Chapel",
        actorIds: ["fatherVale"],
        baseScore: 0,
        activeThreshold: 6,
        variables: [
          { label: "carrying envelope", weight: 8, condition: { type: "itemOwner", item: "blueEnvelope", owner: "actor" } },
          { label: "still before public accusation", weight: 2, condition: { type: "timeAtMost", time: "1045" } }
        ],
        adjustments: [
          {
            label: "leave Archive with envelope",
            actionId: "move_hall",
            amount: 5,
            conditions: [
              { type: "personAt", person: "actor", location: "archive" },
              { type: "itemOwner", item: "blueEnvelope", owner: "actor" },
              { type: "timeAtMost", time: "1045" }
            ]
          },
          {
            label: "enter Chapel with envelope",
            actionId: "move_chapel",
            amount: 5.5,
            conditions: [
              { type: "personAt", person: "actor", location: "hall" },
              { type: "itemOwner", item: "blueEnvelope", owner: "actor" },
              { type: "timeAtMost", time: "1045" }
            ]
          }
        ],
        instructions: [
          {
            type: "goTo",
            location: "chapel",
            label: "Travel to the Chapel with the envelope",
            satisfied: {
              type: "all",
              conditions: [
                { type: "personAt", person: "actor", location: "chapel" },
                { type: "itemOwner", item: "blueEnvelope", owner: "actor" }
              ]
            }
          }
        ],
        completion: {
          type: "all",
          conditions: [
            { type: "personAt", person: "actor", location: "chapel" },
            { type: "itemOwner", item: "blueEnvelope", owner: "actor" }
          ]
        }
      },
      {
        id: "doctor_prevent_bad_accusation",
        label: "Prevent a public disgrace",
        actorIds: ["doctorMerrow"],
        baseScore: 0,
        activeThreshold: 7,
        variables: [
          { label: "final accusation window", weight: 5, condition: { type: "timeAtLeast", time: "1100" } },
          { label: "Jonah has been named", weight: 6, condition: { type: "fact", key: "jonahAccused", value: true } },
          { label: "Jonah is reachable", weight: 2, condition: { type: "personAt", person: "jonah", location: "hall" } }
        ],
        adjustments: [
          {
            label: "reach the Hall before accusation settles",
            actionId: "move_hall",
            amount: 5,
            conditions: [
              { type: "timeAtLeast", time: "1100" },
              { type: "fact", key: "jonahAccused", value: true },
              { type: "personAt", person: "jonah", location: "hall" },
              { type: "not", condition: { type: "personAt", person: "actor", location: "hall" } }
            ]
          },
          {
            label: "deflect when Jonah is present",
            actionId: "doctor_deflect_accusation",
            amount: 7,
            conditions: [
              { type: "personAt", person: "actor", location: "hall" },
              { type: "personAt", person: "jonah", location: "hall" },
              { type: "fact", key: "jonahAccused", value: true }
            ]
          }
        ],
        instructions: [
          { type: "goTo", location: "hall", label: "Reach the Hall" },
          {
            type: "action",
            actionId: "doctor_deflect_accusation",
            label: "Deflect the accusation",
            satisfied: { type: "fact", key: "accusationDeflected", value: true }
          }
        ],
        completion: { type: "fact", key: "accusationDeflected", value: true }
      }
    ],
    defaultTimeline: [
      {
        id: "day_begins",
        timeId: "0800",
        text: "The household gathers for the old magistrate's memorial.",
        certainty: "known"
      },
      {
        id: "envelope_uncertain",
        timeId: "0910",
        text: "The blue envelope is expected to disappear.",
        certainty: "uncertain"
      },
      {
        id: "evening_accusation_uncertain",
        timeId: "1115",
        text: "Someone is likely to be blamed in the Hall.",
        certainty: "uncertain"
      }
    ],
    timelineEntries: {
      pantry_gossip: {
        id: "pantry_gossip",
        timeId: "0800",
        text: "Father Vale expects the blue envelope to be in the Archive.",
        certainty: "known"
      },
      jonah_archive_address: {
        id: "jonah_archive_address",
        timeId: "0815",
        text: "Jonah reads the address on the blue envelope but may not take it.",
        certainty: "known"
      },
      envelope_removed: {
        id: "envelope_removed",
        timeId: "0900",
        text: "The blue envelope leaves the Archive.",
        certainty: "uncertain"
      },
      vale_takes_envelope: {
        id: "vale_takes_envelope",
        timeId: "0900",
        text: "Father Vale removes the blue envelope from the Archive.",
        certainty: "known"
      },
      jonah_humiliated: {
        id: "jonah_humiliated",
        timeId: "0915",
        text: "Clara humiliates Jonah over the ink on his cuff.",
        certainty: "known"
      },
      jonah_helped: {
        id: "jonah_helped",
        timeId: "0915",
        text: "Clara quietly helps Jonah hide the ink on his cuff.",
        certainty: "known"
      },
      clara_threatens_jonah: {
        id: "clara_threatens_jonah",
        timeId: "0915",
        text: "Clara corners Jonah about the blue envelope.",
        certainty: "known"
      },
      clara_asks_vouch: {
        id: "clara_asks_vouch",
        timeId: "1030",
        text: "Clara asks Jonah to vouch for her before the accusation forms.",
        certainty: "known"
      },
      jonah_refuses_clara: {
        id: "jonah_refuses_clara",
        timeId: "1030",
        text: "Jonah refuses to help Clara.",
        certainty: "known"
      },
      jonah_vouches: {
        id: "jonah_vouches",
        timeId: "1030",
        text: "Jonah agrees to vouch for Clara.",
        certainty: "known"
      },
      accusation: {
        id: "accusation",
        timeId: "1115",
        text: "Father Vale accuses Jonah in front of the Hall.",
        certainty: "known"
      },
      accusation_deflected: {
        id: "accusation_deflected",
        timeId: "1115",
        text: "Dr. Merrow deflects the accusation before it settles on Jonah.",
        certainty: "known"
      }
    },
    actions: [
      {
        id: "clara_listen_pantry",
        label: "Listen at the pantry door",
        actorIds: ["clara"],
        slotId: "clara_morning_errand",
        timeIds: ["0800", "0815"],
        timeWindow: { start: "0800", end: "0845" },
        locationId: "kitchen",
        baseScore: 3,
        tags: ["private", "suspicious"],
        effects: [
          {
            type: "memory",
            person: "actor",
            id: "heard_vale_needs_envelope",
            text: "You heard Father Vale ask whether the blue envelope had arrived."
          },
          { type: "discoverTimeline", id: "pantry_gossip" }
        ],
        text: {
          actor:
            "You slow your hands at the pantry door. Father Vale's voice slips through: the blue envelope must be in the Archive before the memorial ends.",
          observer: "{actor} pauses beside the pantry door a little too long."
        }
      },
      {
        id: "clara_hide_envelope",
        label: "Hide the blue envelope under your apron",
        actorIds: ["clara"],
        slotId: "clara_archive_envelope",
        timeIds: ["0845", "0900", "0945", "1000"],
        timeWindow: { start: "0845", end: "1015" },
        locationId: "archive",
        baseScore: 2,
        tags: ["private", "risky", "suspicious"],
        preconditions: [{ type: "itemAt", item: "blueEnvelope", location: "archive" }],
        modifiers: [
          {
            add: 5,
            condition: { type: "memory", person: "actor", id: "heard_vale_needs_envelope" },
            reason: "Clara knows Vale wants it."
          }
        ],
        effects: [
          { type: "transferItem", item: "blueEnvelope", to: "actor" },
          {
            type: "memory",
            person: "actor",
            id: "hid_blue_envelope",
            text: "You hid the blue envelope under your apron."
          },
          { type: "discoverTimeline", id: "envelope_removed" }
        ],
        text: {
          actor:
            "You slide the blue envelope beneath your apron. The seal feels cold through the linen.",
          observer: "{actor} leaves the Archive with one hand held still at her waist."
        }
      },
      {
        id: "clara_leave_envelope",
        label: "Leave the envelope where it is",
        actorIds: ["clara"],
        slotId: "clara_archive_envelope",
        timeIds: ["0845", "0900", "0945", "1000"],
        timeWindow: { start: "0845", end: "1015" },
        locationId: "archive",
        baseScore: 4,
        tags: ["careful"],
        preconditions: [{ type: "itemAt", item: "blueEnvelope", location: "archive" }],
        effects: [
          {
            type: "memory",
            person: "actor",
            id: "left_blue_envelope",
            text: "You left the blue envelope in the Archive."
          }
        ],
        text: {
          actor:
            "You leave the blue envelope in the exact center of the desk, as if neatness could absolve you.",
          observer: "{actor} studies the Archive desk, then steps away empty-handed."
        }
      },
      {
        id: "jonah_copy_address",
        label: "Copy the envelope address",
        actorIds: ["jonah"],
        slotId: "jonah_archive_decision",
        timeIds: ["0800", "0815", "0830"],
        timeWindow: { start: "0800", end: "0900" },
        locationId: "archive",
        baseScore: 7,
        tags: ["private", "helpful"],
        preconditions: [{ type: "itemAt", item: "blueEnvelope", location: "archive" }],
        effects: [
          {
            type: "memory",
            person: "actor",
            id: "copied_envelope_address",
            text: "You copied the blue envelope's address without breaking the seal."
          },
          { type: "discoverTimeline", id: "jonah_archive_address" }
        ],
        text: {
          actor:
            "You copy the address into the margin of your proof sheet. The seal remains unbroken. Your hand still shakes.",
          observer: "{actor} bends over the Archive desk, writing quickly."
        }
      },
      {
        id: "jonah_take_envelope",
        label: "Slip the blue envelope into your satchel",
        actorIds: ["jonah"],
        slotId: "jonah_archive_decision",
        timeIds: ["0800", "0815", "0830"],
        timeWindow: { start: "0800", end: "0900" },
        locationId: "archive",
        baseScore: 3,
        tags: ["private", "risky", "suspicious"],
        preconditions: [{ type: "itemAt", item: "blueEnvelope", location: "archive" }],
        modifiers: [
          {
            add: 4,
            condition: { type: "relationshipAtLeast", from: "actor", to: "fatherVale", metric: "fear", value: 2 },
            reason: "Fear of Vale makes hiding evidence feel safer."
          }
        ],
        effects: [
          { type: "transferItem", item: "blueEnvelope", to: "actor" },
          {
            type: "memory",
            person: "actor",
            id: "took_blue_envelope",
            text: "You took the blue envelope from the Archive."
          },
          { type: "discoverTimeline", id: "envelope_removed" }
        ],
        text: {
          actor:
            "You slip the blue envelope into your satchel and close the flap before your courage can run out.",
          observer: "{actor} makes a small, guilty motion at the Archive desk."
        }
      },
      {
        id: "jonah_leave_archive",
        label: "Leave the Archive before anyone asks why",
        actorIds: ["jonah"],
        slotId: "jonah_archive_decision",
        timeIds: ["0800", "0815", "0830"],
        timeWindow: { start: "0800", end: "0900" },
        locationId: "archive",
        baseScore: 2,
        tags: ["careful"],
        effects: [
          { type: "move", person: "actor", to: "hall" },
          {
            type: "memory",
            person: "actor",
            id: "left_archive_early",
            text: "You left the Archive before touching the blue envelope."
          }
        ],
        text: {
          actor:
            "You back away from the desk and return to the Hall with your errand unfinished.",
          observer: "{actor} leaves the Archive in a hurry."
        }
      },
      {
        id: "father_take_envelope",
        label: "Remove the blue envelope for safekeeping",
        actorIds: ["fatherVale"],
        slotId: "father_archive_decision",
        timeIds: ["0900", "0915"],
        timeWindow: { start: "0845", end: "1015" },
        locationId: "archive",
        baseScore: 7,
        tags: ["private", "suspicious"],
        preconditions: [{ type: "itemAt", item: "blueEnvelope", location: "archive" }],
        effects: [
          { type: "transferItem", item: "blueEnvelope", to: "actor" },
          { type: "setFact", key: "valeTookEnvelope", value: true },
          {
            type: "memory",
            person: "actor",
            id: "took_envelope_for_confession",
            text: "You removed the blue envelope to protect a confession."
          },
          { type: "discoverTimeline", id: "vale_takes_envelope" }
        ],
        text: {
          actor:
            "You take the blue envelope and press it inside your prayer book. Mercy, you remind yourself, has a talent for looking like theft.",
          observer: "{actor} places the blue envelope inside his prayer book."
        }
      },
      {
        id: "father_find_missing_envelope",
        label: "Search the empty Archive desk",
        actorIds: ["fatherVale"],
        slotId: "father_archive_decision",
        timeIds: ["0900", "0915", "1045"],
        timeWindow: { start: "0900", end: "1045" },
        locationId: "archive",
        baseScore: 8,
        tags: ["private", "suspicious"],
        preconditions: [
          { type: "not", condition: { type: "itemAt", item: "blueEnvelope", location: "archive" } },
          { type: "not", condition: { type: "itemOwner", item: "blueEnvelope", owner: "actor" } }
        ],
        effects: [
          { type: "setFact", key: "envelopeKnownMissing", value: true },
          {
            type: "memory",
            person: "actor",
            id: "found_envelope_missing",
            text: "The blue envelope was gone before you could secure it."
          },
          { type: "relationshipDelta", from: "actor", to: "jonah", metric: "suspicion", delta: 2 },
          { type: "relationshipDelta", from: "actor", to: "clara", metric: "suspicion", delta: 1 },
          { type: "discoverTimeline", id: "envelope_removed" }
        ],
        text: {
          actor:
            "The Archive desk is bare. You check beneath the ledger, then under it again, as if the second search might be forgiven.",
          observer: "{actor} searches the Archive desk with a priest's calm and a thief's urgency."
        }
      },
      {
        id: "clara_mock_jonah_cuff",
        label: "Mock Jonah's ink-stained cuff",
        actorIds: ["clara"],
        targetId: "jonah",
        slotId: "clara_jonah_hall_ink",
        timeIds: ["0915"],
        timeWindow: { start: "0900", end: "0945" },
        locationId: "hall",
        baseScore: 4,
        tags: ["public", "cruel", "humiliating"],
        preconditions: [{ type: "sameLocation", person: "target" }],
        modifiers: [
          {
            add: 3,
            condition: { type: "memory", person: "actor", id: "heard_vale_needs_envelope" },
            reason: "Clara suspects Jonah knows more than he says."
          },
          {
            add: 2,
            condition: { type: "relationshipAtLeast", from: "actor", to: "target", metric: "resentment", value: 1 },
            reason: "Existing resentment sharpens the joke."
          },
          {
            add: -4,
            condition: { type: "relationshipAtLeast", from: "actor", to: "target", metric: "gratitude", value: 1 },
            reason: "Gratitude makes cruelty harder."
          }
        ],
        effects: [
          { type: "relationshipDelta", from: "target", to: "actor", metric: "resentment", delta: 3 },
          { type: "relationshipDelta", from: "target", to: "actor", metric: "shame", delta: 2 },
          { type: "relationshipDelta", from: "actor", to: "target", metric: "guilt", delta: 1 },
          {
            type: "memory",
            person: "target",
            id: "clara_mocked_cuff",
            text: "Clara mocked the ink on your cuff in front of the Hall."
          },
          {
            type: "memory",
            person: "actor",
            id: "mocked_jonah_cuff",
            text: "You mocked Jonah's ink-stained cuff in public."
          },
          { type: "discoverTimeline", id: "jonah_humiliated" }
        ],
        text: {
          actor:
            "You let your eyes fall to Jonah's cuff. The footman laughs before Jonah can answer.",
          target:
            "Clara notices the ink on your cuff. The footman laughs before you can hide it.",
          observer:
            "Clara mocks Jonah's stained cuff. The footman laughs, then looks instantly sorry."
        }
      },
      {
        id: "clara_help_jonah_cuff",
        label: "Help Jonah hide the stain",
        actorIds: ["clara"],
        targetId: "jonah",
        slotId: "clara_jonah_hall_ink",
        timeIds: ["0915"],
        timeWindow: { start: "0900", end: "0945" },
        locationId: "hall",
        baseScore: 3,
        tags: ["private", "kind", "helpful"],
        preconditions: [{ type: "sameLocation", person: "target" }],
        modifiers: [
          {
            add: 3,
            condition: { type: "relationshipAtLeast", from: "actor", to: "target", metric: "trust", value: 1 },
            reason: "Trust gives Clara a gentler route."
          },
          {
            add: 2,
            condition: { type: "memory", person: "actor", id: "left_blue_envelope" },
            reason: "Having stayed clean, Clara can afford kindness."
          }
        ],
        effects: [
          { type: "relationshipDelta", from: "target", to: "actor", metric: "gratitude", delta: 3 },
          { type: "relationshipDelta", from: "target", to: "actor", metric: "trust", delta: 2 },
          { type: "relationshipDelta", from: "actor", to: "target", metric: "protectiveness", delta: 1 },
          {
            type: "memory",
            person: "target",
            id: "clara_helped_cuff",
            text: "Clara helped you hide the ink on your cuff before anyone laughed."
          },
          {
            type: "memory",
            person: "actor",
            id: "helped_jonah_cuff",
            text: "You helped Jonah hide the ink on his cuff."
          },
          { type: "discoverTimeline", id: "jonah_helped" }
        ],
        text: {
          actor:
            "You step close enough to block the footman's view and press a folded cloth into Jonah's hand.",
          target:
            "Clara moves between you and the Hall, then palms you a folded cloth before the footman sees the stain.",
          observer:
            "Clara shields Jonah from the room and passes him a cloth with practiced speed."
        }
      },
      {
        id: "clara_threaten_jonah",
        label: "Threaten Jonah about the envelope",
        actorIds: ["clara"],
        targetId: "jonah",
        slotId: "clara_jonah_hall_ink",
        timeIds: ["0915"],
        timeWindow: { start: "0900", end: "0945" },
        locationId: "hall",
        baseScore: 2,
        tags: ["private", "threatening", "suspicious"],
        preconditions: [{ type: "sameLocation", person: "target" }],
        modifiers: [
          {
            add: 5,
            condition: { type: "memory", person: "actor", id: "heard_vale_needs_envelope" },
            reason: "The pantry gossip makes Jonah look useful."
          },
          {
            add: 3,
            condition: { type: "itemOwner", item: "blueEnvelope", owner: "target" },
            reason: "Jonah visibly has what Clara wants."
          }
        ],
        effects: [
          { type: "relationshipDelta", from: "target", to: "actor", metric: "fear", delta: 2 },
          { type: "relationshipDelta", from: "target", to: "actor", metric: "resentment", delta: 1 },
          { type: "relationshipDelta", from: "actor", to: "target", metric: "suspicion", delta: 2 },
          {
            type: "memory",
            person: "target",
            id: "clara_threatened_envelope",
            text: "Clara threatened you over the blue envelope."
          },
          {
            type: "memory",
            person: "actor",
            id: "threatened_jonah_envelope",
            text: "You threatened Jonah about the blue envelope."
          },
          { type: "discoverTimeline", id: "clara_threatens_jonah" }
        ],
        text: {
          actor:
            "You keep your smile for the Hall and lower your voice for Jonah: if he has the envelope, he should fear you before he fears Vale.",
          target:
            "Clara smiles for everyone else. For you, she says the blue envelope will cost you if you lie.",
          observer:
            "Clara smiles at Jonah with the warmth of a locked drawer."
        }
      },
      {
        id: "clara_ignore_jonah",
        label: "Ignore Jonah and pass",
        actorIds: ["clara"],
        targetId: "jonah",
        slotId: "clara_jonah_hall_ink",
        timeIds: ["0915"],
        timeWindow: { start: "0900", end: "0945" },
        locationId: "hall",
        baseScore: 5,
        tags: ["public", "careful"],
        preconditions: [{ type: "sameLocation", person: "target" }],
        effects: [
          {
            type: "memory",
            person: "actor",
            id: "ignored_jonah_hall",
            text: "You passed Jonah in the Hall without drawing attention."
          },
          {
            type: "memory",
            person: "target",
            id: "clara_ignored_hall",
            text: "Clara saw you in the Hall and chose not to speak."
          }
        ],
        text: {
          actor:
            "You see the ink, see Jonah see you seeing it, and pass without spending either of you.",
          target:
            "Clara sees the ink. She also sees your face. She passes without saying a word.",
          observer:
            "Clara and Jonah notice each other, then let the moment die quietly."
        }
      },
      {
        id: "clara_ask_jonah_vouch",
        label: "Ask Jonah to vouch for you",
        actorIds: ["clara"],
        targetId: "jonah",
        slotId: "clara_midday_appeal",
        timeIds: ["1030", "1045"],
        timeWindow: { start: "1015", end: "1100" },
        locationId: "hall",
        baseScore: 5,
        tags: ["public", "helpful", "risky"],
        preconditions: [{ type: "sameLocation", person: "target" }],
        modifiers: [
          {
            add: 2,
            condition: { type: "memory", person: "actor", id: "helped_jonah_cuff" },
            reason: "Clara knows she gave Jonah a reason to answer kindly."
          },
          {
            add: -3,
            condition: { type: "memory", person: "actor", id: "mocked_jonah_cuff" },
            reason: "Asking after cruelty tastes dangerous."
          }
        ],
        effects: [
          {
            type: "memory",
            person: "target",
            id: "clara_asked_vouch",
            text: "Clara asked you to vouch for her before the Hall turned on someone."
          },
          {
            type: "memory",
            person: "actor",
            id: "asked_jonah_vouch",
            text: "You asked Jonah to vouch for you."
          },
          { type: "discoverTimeline", id: "clara_asks_vouch" }
        ],
        text: {
          actor:
            "You ask Jonah, plainly enough for witnesses, whether he saw you leave the Archive empty-handed.",
          target:
            "Clara asks you to tell the Hall she left the Archive empty-handed. Everyone nearby learns how badly she needs your answer.",
          observer:
            "Clara asks Jonah to speak for her. The room's attention tilts toward him."
        }
      },
      {
        id: "clara_apologize_jonah",
        label: "Apologize to Jonah",
        actorIds: ["clara"],
        targetId: "jonah",
        slotId: "clara_midday_appeal",
        timeIds: ["1030", "1045"],
        timeWindow: { start: "1015", end: "1100" },
        locationId: "hall",
        baseScore: 1,
        tags: ["private", "kind"],
        preconditions: [
          { type: "sameLocation", person: "target" },
          { type: "memory", person: "actor", id: "mocked_jonah_cuff" }
        ],
        modifiers: [
          {
            add: 5,
            condition: { type: "relationshipAtLeast", from: "actor", to: "target", metric: "guilt", value: 1 },
            reason: "Guilt presses for repair."
          }
        ],
        effects: [
          { type: "relationshipDelta", from: "target", to: "actor", metric: "resentment", delta: -2 },
          { type: "relationshipDelta", from: "target", to: "actor", metric: "trust", delta: 1 },
          {
            type: "memory",
            person: "target",
            id: "clara_apologized",
            text: "Clara apologized for humiliating you."
          },
          {
            type: "memory",
            person: "actor",
            id: "apologized_to_jonah",
            text: "You apologized to Jonah."
          }
        ],
        text: {
          actor:
            "You catch Jonah at the edge of the room and say the apology before pride can tidy it into something smaller.",
          target:
            "Clara finds you at the edge of the room and apologizes. It is awkward, which helps you believe it.",
          observer:
            "Clara says something quiet to Jonah. His face changes by a very small amount."
        }
      },
      {
        id: "clara_accuse_jonah",
        label: "Hint that Jonah took the envelope",
        actorIds: ["clara"],
        targetId: "jonah",
        slotId: "clara_midday_appeal",
        timeIds: ["1030", "1045"],
        timeWindow: { start: "1015", end: "1100" },
        locationId: "hall",
        baseScore: 2,
        tags: ["public", "cruel", "suspicious"],
        preconditions: [{ type: "sameLocation", person: "target" }],
        modifiers: [
          {
            add: 5,
            condition: { type: "itemOwner", item: "blueEnvelope", owner: "target" },
            reason: "Jonah actually has the envelope."
          },
          {
            add: 2,
            condition: { type: "relationshipAtLeast", from: "actor", to: "target", metric: "suspicion", value: 2 },
            reason: "Suspicion wants an audience."
          }
        ],
        effects: [
          { type: "relationshipDelta", from: "target", to: "actor", metric: "resentment", delta: 3 },
          { type: "relationshipDelta", from: "fatherVale", to: "target", metric: "suspicion", delta: 2 },
          { type: "setFact", key: "jonahPubliclySuspected", value: true },
          {
            type: "memory",
            person: "target",
            id: "clara_publicly_suspected_you",
            text: "Clara made the Hall wonder whether you took the blue envelope."
          }
        ],
        text: {
          actor:
            "You do not accuse Jonah. You only say he was near the Archive, and let the Hall do the rest.",
          target:
            "Clara says you were near the Archive. She does not need to say thief. The Hall supplies it for her.",
          observer:
            "Clara places Jonah beside the missing envelope without quite accusing him."
        }
      },
      {
        id: "jonah_vouch_for_clara",
        label: "Vouch for Clara",
        actorIds: ["jonah"],
        targetId: "clara",
        slotId: "jonah_clara_midday_answer",
        timeIds: ["1030", "1045"],
        timeWindow: { start: "1015", end: "1100" },
        locationId: "hall",
        baseScore: 2,
        tags: ["public", "kind", "helpful"],
        preconditions: [
          { type: "sameLocation", person: "target" },
          { type: "memory", person: "actor", id: "clara_asked_vouch" }
        ],
        modifiers: [
          {
            add: 5,
            condition: { type: "relationshipAtLeast", from: "actor", to: "target", metric: "gratitude", value: 1 },
            reason: "Gratitude makes Jonah brave."
          },
          {
            add: 3,
            condition: { type: "memory", person: "actor", id: "clara_helped_cuff" },
            reason: "Jonah remembers Clara shielding him."
          },
          {
            add: -5,
            condition: { type: "relationshipAtLeast", from: "actor", to: "target", metric: "resentment", value: 2 },
            reason: "Resentment makes help feel undeserved."
          }
        ],
        effects: [
          { type: "setFact", key: "claraHasVouch", value: true },
          { type: "relationshipDelta", from: "target", to: "actor", metric: "gratitude", delta: 2 },
          {
            type: "memory",
            person: "target",
            id: "jonah_vouched_for_you",
            text: "Jonah vouched for you when the Hall began choosing a culprit."
          },
          { type: "discoverTimeline", id: "jonah_vouches" }
        ],
        text: {
          actor:
            "You tell them Clara left the Archive empty-handed. Your voice cracks once and then steadies.",
          target:
            "Jonah says he saw you leave the Archive empty-handed. It costs him more than he wants the Hall to know.",
          observer:
            "Jonah vouches for Clara, and the room loses one easy answer."
        }
      },
      {
        id: "jonah_refuse_clara",
        label: "Refuse Clara's request",
        actorIds: ["jonah"],
        targetId: "clara",
        slotId: "jonah_clara_midday_answer",
        timeIds: ["1030", "1045"],
        timeWindow: { start: "1015", end: "1100" },
        locationId: "hall",
        baseScore: 3,
        tags: ["public", "cruel"],
        preconditions: [
          { type: "sameLocation", person: "target" },
          { type: "memory", person: "actor", id: "clara_asked_vouch" }
        ],
        modifiers: [
          {
            add: 5,
            condition: { type: "memory", person: "actor", id: "clara_mocked_cuff" },
            reason: "Humiliation asks to be returned."
          },
          {
            add: 2,
            condition: { type: "relationshipAtLeast", from: "actor", to: "target", metric: "resentment", value: 2 },
            reason: "Resentment makes silence feel righteous."
          },
          {
            add: -4,
            condition: { type: "memory", person: "actor", id: "clara_apologized" },
            reason: "The apology weakens the revenge."
          }
        ],
        effects: [
          { type: "relationshipDelta", from: "target", to: "actor", metric: "resentment", delta: 2 },
          {
            type: "memory",
            person: "target",
            id: "jonah_refused_you",
            text: "Jonah refused to vouch for you."
          },
          { type: "discoverTimeline", id: "jonah_refuses_clara" }
        ],
        text: {
          actor:
            "You look at Clara's hands, then her face, and say you did not see enough to help her.",
          target:
            "Jonah looks at your hands, then your face, and refuses to save you from the room.",
          observer:
            "Jonah refuses Clara with a politeness that lands harder than anger."
        }
      },
      {
        id: "jonah_expose_clara",
        label: "Say Clara knows more than she admits",
        actorIds: ["jonah"],
        targetId: "clara",
        slotId: "jonah_clara_midday_answer",
        timeIds: ["1030", "1045"],
        timeWindow: { start: "1015", end: "1100" },
        locationId: "hall",
        baseScore: 1,
        tags: ["public", "risky", "confrontational"],
        preconditions: [{ type: "sameLocation", person: "target" }],
        modifiers: [
          {
            add: 4,
            condition: { type: "memory", person: "actor", id: "clara_threatened_envelope" },
            reason: "The threat makes Clara look involved."
          },
          {
            add: 5,
            condition: { type: "memory", person: "actor", id: "clara_publicly_suspected_you" },
            reason: "Public suspicion invites a public answer."
          }
        ],
        effects: [
          { type: "relationshipDelta", from: "target", to: "actor", metric: "resentment", delta: 2 },
          { type: "relationshipDelta", from: "fatherVale", to: "target", metric: "suspicion", delta: 2 },
          { type: "setFact", key: "claraPubliclySuspected", value: true },
          {
            type: "memory",
            person: "target",
            id: "jonah_exposed_your_interest",
            text: "Jonah told the Hall you knew more about the envelope than you admitted."
          }
        ],
        text: {
          actor:
            "You tell the Hall Clara asked about the envelope before anyone said it was missing.",
          target:
            "Jonah tells the Hall you asked about the envelope too early. Your own timing turns against you.",
          observer:
            "Jonah says Clara knew about the envelope before she should have."
        }
      },
      {
        id: "doctor_warn_clara",
        label: "Warn Clara that blame is moving",
        actorIds: ["doctorMerrow"],
        targetId: "clara",
        slotId: "doctor_midday_patient_choice",
        timeIds: ["1015", "1030", "1045"],
        timeWindow: { start: "1000", end: "1100" },
        locationId: "hall",
        baseScore: 3,
        tags: ["private", "helpful"],
        preconditions: [{ type: "sameLocation", person: "target" }],
        modifiers: [
          {
            add: 3,
            condition: { type: "fact", key: "claraPubliclySuspected", value: true },
            reason: "Clara is already in danger."
          },
          {
            add: 2,
            condition: { type: "relationshipAtLeast", from: "actor", to: "target", metric: "protectiveness", value: 1 },
            reason: "Protectiveness pulls Merrow toward Clara."
          }
        ],
        effects: [
          {
            type: "memory",
            person: "target",
            id: "doctor_warned_blame",
            text: "Dr. Merrow warned you that the Hall was choosing a culprit."
          },
          { type: "relationshipDelta", from: "target", to: "actor", metric: "gratitude", delta: 1 }
        ],
        text: {
          actor:
            "You tell Clara that accusation moves like fever: slowly, then all at once.",
          target:
            "Dr. Merrow tells you accusation moves like fever: slowly, then all at once.",
          observer:
            "Dr. Merrow gives Clara a warning too quiet for the Hall to keep."
        }
      },
      {
        id: "doctor_deflect_accusation",
        label: "Deflect the accusation from Jonah",
        actorIds: ["doctorMerrow"],
        targetId: "jonah",
        slotId: "doctor_final_choice",
        timeIds: ["1115"],
        timeWindow: { start: "1100", end: "1115" },
        locationId: "hall",
        baseScore: 2,
        tags: ["public", "kind", "risky"],
        preconditions: [
          { type: "sameLocation", person: "target" },
          { type: "fact", key: "jonahAccused", value: true }
        ],
        modifiers: [
          {
            add: 5,
            condition: { type: "memory", person: "target", id: "clara_helped_cuff" },
            reason: "Jonah has shown he can be saved by public kindness."
          },
          {
            add: 4,
            condition: { type: "fact", key: "claraHasVouch", value: true },
            reason: "Jonah has already risked himself for another."
          }
        ],
        effects: [
          { type: "setFact", key: "accusationDeflected", value: true },
          {
            type: "memory",
            person: "target",
            id: "doctor_deflected_accusation",
            text: "Dr. Merrow interrupted before Father Vale's accusation could settle on you."
          },
          { type: "discoverTimeline", id: "accusation_deflected" }
        ],
        text: {
          actor:
            "You ask Father Vale whether charity now requires evidence, and the Hall remembers itself.",
          target:
            "Dr. Merrow interrupts before Father Vale can turn suspicion into a verdict.",
          observer:
            "Dr. Merrow cuts across Father Vale's accusation, and the Hall releases the breath it was holding."
        }
      },
      {
        id: "father_accuse_jonah",
        label: "Accuse Jonah of taking the envelope",
        actorIds: ["fatherVale"],
        targetId: "jonah",
        slotId: "father_final_accusation",
        timeIds: ["1115"],
        timeWindow: { start: "1100", end: "1115" },
        locationId: "hall",
        baseScore: 4,
        tags: ["public", "threatening", "suspicious"],
        preconditions: [
          { type: "sameLocation", person: "target" },
          { type: "not", condition: { type: "itemOwner", item: "blueEnvelope", owner: "actor" } }
        ],
        modifiers: [
          {
            add: 5,
            condition: { type: "fact", key: "jonahPubliclySuspected", value: true },
            reason: "The Hall is already primed against Jonah."
          },
          {
            add: 3,
            condition: { type: "memory", person: "actor", id: "found_envelope_missing" },
            reason: "Vale knows someone reached the envelope first."
          },
          {
            add: 4,
            condition: { type: "itemOwner", item: "blueEnvelope", owner: "target" },
            reason: "Jonah actually has the envelope."
          },
          {
            add: -5,
            condition: { type: "fact", key: "claraHasVouch", value: true },
            reason: "Jonah's public help makes him harder to isolate."
          }
        ],
        effects: [
          { type: "setFact", key: "jonahAccused", value: true },
          { type: "relationshipDelta", from: "target", to: "actor", metric: "fear", delta: 2 },
          {
            type: "memory",
            person: "target",
            id: "father_accused_you",
            text: "Father Vale accused you in front of the Hall."
          },
          { type: "discoverTimeline", id: "accusation" }
        ],
        text: {
          actor:
            "You name Jonah because the Hall wants a name, and because a frightened boy is easier to move than a sealed confession.",
          target:
            "Father Vale says your name. Around it, the Hall builds a shape that looks like guilt.",
          observer:
            "Father Vale names Jonah as the one who removed the envelope."
        }
      }
    ]
  };
})();
