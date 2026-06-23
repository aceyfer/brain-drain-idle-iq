// Throwaway pacing/balance simulation -- NOT part of the game, lives outside Assets/ so Unity
// never sees it. Models the real formulas from CurrencyManager/UpgradeManager/HUDController:
// cost = baseCost * 1.15^level, greedy "buy cheapest unlocked+affordable" purchasing AI,
// periodic convert-all-cash-to-points + spend-all-points-on-restoration every 30s (matching
// the CONVERT/RESTORE buttons' actual "do everything at once" behavior), no rebirths.

const BUILDINGS_TEMPLATE = [
  { name: "The Literal Library", unlock: 0, baseCost: 15, costMult: 1.15, bpps: 0.1, cps: 0 },
  { name: "Doomscroll Engine", unlock: 0, baseCost: 10, costMult: 1.15, bpps: 0.3, cps: 0 },
  { name: "Underground Economy", unlock: 500, baseCost: 75, costMult: 1.15, bpps: 0, cps: 0.5 },
  { name: "Podcaster Soundboard", unlock: 10000, baseCost: 150, costMult: 1.15, bpps: 5, cps: 0 },
  { name: "Crypto-Bro Compound", unlock: 65000, baseCost: 1200, costMult: 1.15, bpps: 60, cps: 0 },
  { name: "Reality TV Syndicate", unlock: 185000, baseCost: 15000, costMult: 1.15, bpps: 320, cps: 0 },
  { name: "Brain-Rot Think Tank", unlock: 725000, baseCost: 200000, costMult: 1.15, bpps: 4500, cps: 0 },
];

const WORLD_STAGES = [
  { name: "Toxic Wasteland", threshold: 0 },
  { name: "Smog-Choked Sprawl", threshold: 2500 },
  { name: "Patchwork Recovery Zone", threshold: 10000 },
  { name: "Green Shoots Initiative", threshold: 50000 },
  { name: "Renewed Skyline", threshold: 250000 },
  { name: "Utopia Achieved", threshold: 1000000 },
];

const REBIRTH_GATE_POINTS_SPENT = 50000;
const CANDIDATE_COST_MULTIPLIER = 1.21;

function fmt(n) {
  if (n >= 1e9) return (n / 1e9).toFixed(2) + "B";
  if (n >= 1e6) return (n / 1e6).toFixed(2) + "M";
  if (n >= 1e3) return (n / 1e3).toFixed(2) + "K";
  return n.toFixed(1);
}

function fmtTime(seconds) {
  const h = Math.floor(seconds / 3600);
  const m = Math.floor((seconds % 3600) / 60);
  const s = Math.floor(seconds % 60);
  return `${h}h ${m}m ${s}s`;
}

function simulate(tapsPerSecond, maxSeconds, label, costMultiplierOverride) {
  const buildings = BUILDINGS_TEMPLATE.map(b => ({ ...b, level: 0, costMult: costMultiplierOverride ?? b.costMult }));
  let brainPower = 0, cumulativeBP = 0, idleBPPS = 0, cashPerSecond = 0, currentCash = 0, currentPoints = 0;
  const pointsConversionRate = 0.1;
  let playerIQ = 100;
  let cumulativeSpentOnRestoration = 0;
  let lastConvertTime = 0;

  const firstPurchaseAt = {};
  const stageReachedAt = {};
  const otherMilestones = [];
  let rebirthUnlockedAt = null;

  const currentCost = b => b.baseCost * Math.pow(b.costMult, b.level);

  for (let t = 0; t <= maxSeconds; t++) {
    brainPower += tapsPerSecond;
    cumulativeBP += tapsPerSecond;
    brainPower += idleBPPS;
    cumulativeBP += idleBPPS;
    currentCash += cashPerSecond;

    let bought = true;
    while (bought) {
      bought = false;
      const candidates = buildings
        .filter(b => cumulativeBP >= b.unlock)
        .sort((a, b) => currentCost(a) - currentCost(b));
      for (const b of candidates) {
        const cost = currentCost(b);
        if (brainPower >= cost) {
          brainPower -= cost;
          b.level++;
          idleBPPS += b.bpps;
          cashPerSecond += b.cps;
          playerIQ += 1;
          if (firstPurchaseAt[b.name] === undefined) firstPurchaseAt[b.name] = t;
          bought = true;
          break;
        }
      }
    }

    if (t - lastConvertTime >= 30) {
      lastConvertTime = t;
      if (currentCash > 0) {
        currentPoints += currentCash * pointsConversionRate;
        currentCash = 0;
      }
      if (currentPoints > 0) {
        cumulativeSpentOnRestoration += currentPoints;
        currentPoints = 0;
      }
    }

    if (rebirthUnlockedAt === null && cumulativeSpentOnRestoration >= REBIRTH_GATE_POINTS_SPENT) {
      rebirthUnlockedAt = t;
    }
    for (const stage of WORLD_STAGES) {
      if (stageReachedAt[stage.name] === undefined && cumulativeSpentOnRestoration >= stage.threshold) {
        stageReachedAt[stage.name] = t;
      }
    }
  }

  console.log(`\n===== ${label} (${tapsPerSecond} taps/sec sustained, ${fmtTime(maxSeconds)} simulated) =====`);
  console.log("Building unlock order (first purchase time):");
  for (const b of BUILDINGS_TEMPLATE) {
    const t = firstPurchaseAt[b.name];
    console.log(`  ${b.name.padEnd(24)} ${t === undefined ? "NEVER bought" : fmtTime(t)}`);
  }
  console.log(`\nFinal PlayerIQ: ${playerIQ.toFixed(0)}  |  Final cumulative Brain Power: ${fmt(cumulativeBP)}  |  Final idle BPPS: ${fmt(idleBPPS)}`);
  console.log(`Cumulative Points spent on Restoration: ${fmt(cumulativeSpentOnRestoration)}`);
  console.log(`REBIRTH button unlocks (1000 pts spent) at: ${rebirthUnlockedAt === null ? "NEVER" : fmtTime(rebirthUnlockedAt)}`);
  console.log("World Restoration stages reached:");
  for (const stage of WORLD_STAGES) {
    const t = stageReachedAt[stage.name];
    console.log(`  ${stage.name.padEnd(24)} ${t === undefined ? "NEVER" : fmtTime(t)}`);
  }
}

const ONE_DAY = 24 * 3600;
console.log(`Candidate costMultiplier: ${CANDIDATE_COST_MULTIPLIER}  |  Rebirth gate: ${REBIRTH_GATE_POINTS_SPENT} points spent`);
simulate(3, ONE_DAY, "ACTIVE, 24h continuous optimal play, NEW costMultiplier", CANDIDATE_COST_MULTIPLIER);
simulate(3, ONE_DAY, "ACTIVE, 24h continuous optimal play, OLD costMultiplier 1.15", 1.15);
