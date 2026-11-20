/**
 * 單淘汰賽程表的核心邏輯（不碰 DOM，方便單獨測試）：
 * 種子排位、抽籤、輪空、晉級計算、修改結果後清掉失效的後續賽果、匯出 HTML。
 *
 * 賽程狀態 (tournament) 的格式：
 * {
 *   version: 1,
 *   title: string,
 *   scoring: boolean,          // 是否有比分
 *   thirdPlace: boolean,       // 是否有季軍戰
 *   participants: [{ id, name, seed }],  // seed 為 1..K 的種子序號，非種子為 null
 *   slots: [participantId | BYE],        // 第一輪的籤位，長度為 2 的次方
 *   results: { [matchKey]: { aId, bId, scoreA, scoreB, winnerId } }
 * }
 * 賽果會記下當時的對戰雙方 (aId, bId)；前面的賽果改變導致對戰雙方不同時，該賽果就失效並被清除。
 */

/** 輪空的籤位 */
export const BYE = 'bye';

/** 參賽者人數下限 */
export const MIN_PARTICIPANTS = 2;

/** 參賽者人數上限 */
export const MAX_PARTICIPANTS = 128;

/** 參賽者名稱長度上限 */
export const MAX_NAME_LENGTH = 40;

/** 賽事名稱長度上限 */
export const MAX_TITLE_LENGTH = 60;

/** 季軍戰的賽果 key */
export const THIRD_PLACE_KEY = 'third';

const STATE_VERSION = 1;

/**
 * 取得容納指定人數所需的賽程規模（2 的次方）。
 * @param {number} count - 參賽人數
 * @returns {number} 賽程規模，例如 5～8 人為 8
 */
export const getBracketSize = (count) => {
  let size = 2;
  while (size < count) {
    size *= 2;
  }
  return size;
};

/**
 * 依標準種子排位取得每個籤位的種子序號，確保種子 1、2 只會在決賽相遇，1～4 只會在四強後相遇，以此類推。
 * 例如 8 籤為 [1, 8, 4, 5, 2, 7, 3, 6]。
 * @param {number} size - 賽程規模（2 的次方）
 * @returns {number[]} 每個籤位對應的種子序號
 */
export const getSeedOrder = (size) => {
  let order = [1];
  while (order.length < size) {
    const nextSize = order.length * 2;
    order = order.flatMap((seed) => [seed, nextSize + 1 - seed]);
  }
  return order;
};

/**
 * 預設的種子人數：約為賽程規模的四分之一（16 強 4 種子、32 強 8 種子），且至少 2 位。
 * @param {number} count - 參賽人數
 * @returns {number} 建議的種子人數
 */
export const getDefaultSeededCount = (count) => {
  if (count < MIN_PARTICIPANTS) {
    return 0;
  }
  return Math.min(count, Math.max(2, getBracketSize(count) / 4));
};

/**
 * 依字元（code point）截斷字串，避免把 emoji 等代理對切成半個而顯示成亂碼。
 * @param {unknown} value - 要截斷的值
 * @param {number} maxLength - 最多幾個字元
 * @returns {string} 截斷後的字串
 */
export const truncate = (value, maxLength) => [...String(value ?? '')].slice(0, maxLength).join('');

/**
 * 將多行文字整理成參賽者名稱清單：去掉空白行與前後空白、截斷過長名稱。
 * @param {string} text - 每行一位參賽者
 * @returns {string[]} 參賽者名稱
 */
export const parseNames = (text) => (text || '')
  .split(/\r?\n/)
  .map((line) => truncate(line.trim(), MAX_NAME_LENGTH))
  .filter((line) => line.length > 0);


/**
 * Fisher–Yates 洗牌（不改動原陣列）。
 * @param {Array} items - 要洗牌的項目
 * @param {() => number} random - 0～1 的亂數產生器
 * @returns {Array} 洗牌後的新陣列
 */
const shuffle = (items, random) => {
  const result = [...items];
  for (let i = result.length - 1; i > 0; i -= 1) {
    const j = Math.floor(random() * (i + 1));
    [result[i], result[j]] = [result[j], result[i]];
  }
  return result;
};

/**
 * 抽籤：名單前 seededCount 位依排名放進種子籤位，其餘參賽者隨機分配到剩下的籤位；
 * 人數不足 2 的次方時，空出的籤位為輪空，且輪空會優先給排名較高的種子。
 * @param {{ id: string }[]} participants - 依排名排序的參賽者
 * @param {number} seededCount - 種子人數
 * @param {() => number} [random] - 亂數產生器，測試時可替換
 * @returns {string[]} 第一輪籤位（參賽者 id 或 BYE）
 */
export const createDraw = (participants, seededCount, random = Math.random) => {
  const size = getBracketSize(participants.length);
  const seedOrder = getSeedOrder(size);
  const seeded = participants.slice(0, seededCount);
  const unseeded = shuffle(participants.slice(seededCount), random);
  // 種子序號 → 參賽者：1..K 為種子，K+1..N 為抽籤後的非種子，N+1..size 為輪空
  const bySeedNumber = [...seeded, ...unseeded];

  return seedOrder.map((seedNumber) => bySeedNumber[seedNumber - 1]?.id ?? BYE);
};

/**
 * 建立新的賽程。
 * @param {Object} options - 賽程設定
 * @param {string} options.title - 賽事名稱
 * @param {string[]} options.names - 依排名排序的參賽者名稱
 * @param {number} options.seededCount - 種子人數（名單前幾位）
 * @param {boolean} options.scoring - 是否有比分
 * @param {boolean} options.thirdPlace - 是否有季軍戰
 * @param {() => number} [random] - 亂數產生器
 * @returns {Object} 賽程狀態
 */
export const createTournament = ({ title, names, seededCount, scoring, thirdPlace }, random = Math.random) => {
  if (names.length < MIN_PARTICIPANTS || names.length > MAX_PARTICIPANTS) {
    throw new RangeError(`參賽人數需介於 ${MIN_PARTICIPANTS}～${MAX_PARTICIPANTS} 之間。`);
  }

  const seeds = Math.max(0, Math.min(names.length, Math.floor(seededCount) || 0));
  const participants = names.map((name, index) => ({
    id: `p${index + 1}`,
    name,
    seed: index < seeds ? index + 1 : null
  }));

  return {
    version: STATE_VERSION,
    title: truncate((title || '').trim(), MAX_TITLE_LENGTH),
    scoring: Boolean(scoring),
    // 季軍戰需要兩場準決賽都有實際對戰，至少 4 人
    thirdPlace: Boolean(thirdPlace) && names.length >= 4,
    participants,
    slots: createDraw(participants, seeds, random),
    results: {}
  };
};

/**
 * 重新抽籤：保留種子位置，非種子重新隨機分配，並清除所有賽果。
 * @param {Object} tournament - 賽程狀態
 * @param {() => number} [random] - 亂數產生器
 * @returns {Object} 新的賽程狀態
 */
export const redrawTournament = (tournament, random = Math.random) => {
  const seededCount = tournament.participants.filter((p) => p.seed !== null).length;
  const ordered = [...tournament.participants].sort((x, y) => (x.seed ?? Infinity) - (y.seed ?? Infinity));
  return {
    ...tournament,
    slots: createDraw(ordered, seededCount, random),
    results: {}
  };
};

/**
 * 取得賽果 key。
 * @param {number} round - 輪次（從 0 開始）
 * @param {number} index - 該輪的第幾場（從 0 開始）
 * @returns {string} 賽果 key
 */
export const getMatchKey = (round, index) => `${round}-${index}`;

/**
 * 取得輪次名稱：決賽、準決賽、8 強……
 * @param {number} round - 輪次（從 0 開始）
 * @param {number} size - 賽程規模
 * @returns {string} 輪次名稱
 */
export const getRoundName = (round, size) => {
  const remaining = size / (2 ** round);
  if (remaining === 2) {
    return '決賽';
  }
  if (remaining === 4) {
    return '準決賽';
  }
  return `${remaining} 強`;
};

/**
 * 依對戰雙方與賽果決定勝者。
 * @param {string|null} a - 上方參賽者 id、BYE，或 null（尚未產生）
 * @param {string|null} b - 下方參賽者 id、BYE，或 null（尚未產生）
 * @param {Object|undefined} result - 賽果
 * @returns {{ winner: string|null, loser: string|null, result: Object|null }} 勝者、敗者與仍有效的賽果
 */
const decideMatch = (a, b, result) => {
  if (a === BYE && b === BYE) {
    return { winner: BYE, loser: BYE, result: null };
  }
  if (a === BYE && b !== null) {
    return { winner: b, loser: BYE, result: null };
  }
  if (b === BYE && a !== null) {
    return { winner: a, loser: BYE, result: null };
  }
  if (a === null || b === null) {
    return { winner: null, loser: null, result: null };
  }

  const isValid = Boolean(result) && result.aId === a && result.bId === b;
  if (!isValid) {
    return { winner: null, loser: null, result: null };
  }
  if (result.winnerId === a) {
    return { winner: a, loser: b, result };
  }
  if (result.winnerId === b) {
    return { winner: b, loser: a, result };
  }
  return { winner: null, loser: null, result };
};

/**
 * 依籤位與賽果推算每一場的對戰雙方與勝負。
 * @param {Object} tournament - 賽程狀態
 * @returns {{ size: number, rounds: Object[][], thirdPlace: Object|null }} 每場比賽的 { key, a, b, winner, loser, result }
 */
export const computeBracket = (tournament) => {
  const size = tournament.slots.length;
  const rounds = [];
  let previous = null;

  for (let matchCount = size / 2, round = 0; matchCount >= 1; matchCount /= 2, round += 1) {
    const matches = [];
    for (let index = 0; index < matchCount; index += 1) {
      const key = getMatchKey(round, index);
      const a = previous ? previous[index * 2].winner : tournament.slots[index * 2];
      const b = previous ? previous[index * 2 + 1].winner : tournament.slots[index * 2 + 1];
      matches.push({ key, round, index, a, b, ...decideMatch(a, b, tournament.results[key]) });
    }
    rounds.push(matches);
    previous = matches;
  }

  let thirdPlace = null;
  if (tournament.thirdPlace && rounds.length >= 2) {
    const semifinals = rounds[rounds.length - 2];
    const a = semifinals[0].loser;
    const b = semifinals[1].loser;
    thirdPlace = { key: THIRD_PLACE_KEY, round: -1, index: 0, a, b, ...decideMatch(a, b, tournament.results[THIRD_PLACE_KEY]) };
  }

  return { size, rounds, thirdPlace };
};

/**
 * 找出指定 key 的比賽。
 * @param {{ rounds: Object[][], thirdPlace: Object|null }} computed - computeBracket 的結果
 * @param {string} key - 賽果 key
 * @returns {Object|null} 比賽
 */
const findMatch = (computed, key) => {
  if (key === THIRD_PLACE_KEY) {
    return computed.thirdPlace;
  }
  return computed.rounds.flat().find((match) => match.key === key) ?? null;
};

/**
 * 清除對戰雙方已改變（因前面的賽果被修改）的賽果，回傳新的賽程狀態。
 * @param {Object} tournament - 賽程狀態
 * @returns {Object} 新的賽程狀態
 */
export const pruneResults = (tournament) => {
  const computed = computeBracket(tournament);
  const results = {};
  Object.entries(tournament.results).forEach(([key, result]) => {
    const match = findMatch(computed, key);
    if (match && match.result === result) {
      results[key] = result;
    }
  });
  return { ...tournament, results };
};

/**
 * 該場比賽目前是否可以登錄結果（雙方都已產生，且不是輪空）。
 * @param {Object} match - computeBracket 產生的比賽
 * @returns {boolean} 是否可登錄
 */
export const isPlayable = (match) => Boolean(match)
  && match.a !== null && match.b !== null && match.a !== BYE && match.b !== BYE;

/**
 * 判斷比分是否已決定勝負（兩邊都有分數且不同）。
 * @param {Object|null} result - 賽果
 * @returns {boolean} 是否由比分決定勝負
 */
export const isDecidedByScore = (result) => Boolean(result)
  && Number.isFinite(result.scoreA) && Number.isFinite(result.scoreB) && result.scoreA !== result.scoreB;

/**
 * 指定某場比賽的勝者；再點一次同一位則取消。
 * @param {Object} tournament - 賽程狀態
 * @param {string} key - 賽果 key
 * @param {string} winnerId - 勝者 id
 * @returns {Object} 新的賽程狀態
 */
export const setWinner = (tournament, key, winnerId) => {
  const match = findMatch(computeBracket(tournament), key);
  if (!isPlayable(match) || (winnerId !== match.a && winnerId !== match.b)) {
    return tournament;
  }

  const current = match.result ?? { aId: match.a, bId: match.b, scoreA: null, scoreB: null, winnerId: null };
  if (tournament.scoring && isDecidedByScore(current)) {
    // 比分已分出勝負時，勝者由比分決定
    return tournament;
  }

  const nextWinner = current.winnerId === winnerId ? null : winnerId;
  if (nextWinner === null && current.scoreA === null && current.scoreB === null) {
    // 取消勝者且沒有比分：整場回到未登錄，不留空白賽果
    return clearResult(tournament, key);
  }
  return pruneResults({
    ...tournament,
    results: { ...tournament.results, [key]: { ...current, winnerId: nextWinner } }
  });
};

/**
 * 登錄某一方的比分；兩邊都有分數且不同時自動決定勝者，平手則需另外指定勝者。
 * @param {Object} tournament - 賽程狀態
 * @param {string} key - 賽果 key
 * @param {'a'|'b'} side - 上方或下方
 * @param {string|number|null} value - 比分，空字串代表清除
 * @returns {Object} 新的賽程狀態
 */
export const setScore = (tournament, key, side, value) => {
  const match = findMatch(computeBracket(tournament), key);
  if (!isPlayable(match)) {
    return tournament;
  }

  const parsed = value === '' || value === null || value === undefined ? null : Number(value);
  // 比分只接受非負整數（輸入 -3 視為 0、2.5 視為 2）
  const score = Number.isFinite(parsed) ? Math.max(0, Math.trunc(parsed)) : null;
  const current = match.result ?? { aId: match.a, bId: match.b, scoreA: null, scoreB: null, winnerId: null };
  const next = { ...current, [side === 'a' ? 'scoreA' : 'scoreB']: score };

  if (isDecidedByScore(next)) {
    next.winnerId = next.scoreA > next.scoreB ? next.aId : next.bId;
  } else if (isDecidedByScore(current)) {
    // 原本由比分決定的勝負已不成立（改成平手或清掉分數），勝者要重新指定
    next.winnerId = null;
  }

  return pruneResults({ ...tournament, results: { ...tournament.results, [key]: next } });
};

/**
 * 清除某場比賽的賽果（連帶清除受影響的後續賽果）。
 * @param {Object} tournament - 賽程狀態
 * @param {string} key - 賽果 key
 * @returns {Object} 新的賽程狀態
 */
export const clearResult = (tournament, key) => {
  const results = { ...tournament.results };
  delete results[key];
  return pruneResults({ ...tournament, results });
};

/**
 * 修改參賽者名稱（賽果以 id 記錄，不受影響）。
 * @param {Object} tournament - 賽程狀態
 * @param {string} id - 參賽者 id
 * @param {string} name - 新名稱
 * @returns {Object} 新的賽程狀態
 */
export const renameParticipant = (tournament, id, name) => ({
  ...tournament,
  participants: tournament.participants.map((p) => (p.id === id
    ? { ...p, name: truncate(name, MAX_NAME_LENGTH) }
    : p))
});

/**
 * 將各輪拆成左右兩個半區的欄位，決賽放在中間：
 * [左半區第 1 輪 … 左半區準決賽, 決賽, 右半區準決賽 … 右半區第 1 輪]。
 * 標準種子排位下，種子 1 在左半區、種子 2 在右半區，兩人從兩側往中間的決賽匯聚。
 * @param {{ index: number, name: string, matches: Object[] }[]} rounds - 各輪資料
 * @returns {{ key: string, name: string, side: 'left'|'final'|'right', isOuter: boolean, isPair: boolean, matches: Object[] }[]} 欄位；
 *   isOuter 為最外側的第 1 輪（沒有連進來的線），isPair 代表這一欄每兩場匯聚成下一欄的一場（否則直接連到決賽）
 */
const toColumns = (rounds) => {
  const finalRound = rounds[rounds.length - 1];
  const halves = rounds.slice(0, -1).map((round) => {
    const half = round.matches.length / 2;
    const base = { name: round.name, isOuter: round.index === 0, isPair: half > 1 };
    return {
      left: { ...base, key: `left-${round.index}`, side: 'left', matches: round.matches.slice(0, half) },
      right: { ...base, key: `right-${round.index}`, side: 'right', matches: round.matches.slice(half) }
    };
  });

  return [
    ...halves.map((h) => h.left),
    { key: 'final', name: finalRound.name, side: 'final', isOuter: finalRound.index === 0, isPair: false, matches: finalRound.matches },
    ...halves.map((h) => h.right).reverse()
  ];
};

/**
 * 產生畫面需要的完整資料：每輪名稱、每場雙方的名稱 / 種子 / 比分 / 勝負、名次與各輪晉級名單。
 * @param {Object} tournament - 賽程狀態
 * @returns {Object} 畫面資料
 */
export const buildView = (tournament) => {
  const computed = computeBracket(tournament);
  const byId = new Map(tournament.participants.map((p) => [p.id, p]));

  const toSide = (match, side) => {
    const id = match[side];
    const participant = byId.get(id);
    const score = match.result ? match.result[side === 'a' ? 'scoreA' : 'scoreB'] : null;
    return {
      side,
      id,
      name: participant?.name || (id === BYE ? '輪空' : '待定'),
      seed: participant?.seed ?? null,
      isBye: id === BYE,
      isPending: id === null,
      score: Number.isFinite(score) ? score : null,
      isWinner: match.winner !== null && match.winner !== BYE && match.winner === id,
      isLoser: match.loser !== null && match.loser !== BYE && match.loser === id
    };
  };

  const toMatch = (match) => {
    const playable = isPlayable(match);
    const decidedByScore = isDecidedByScore(match.result);
    return {
      key: match.key,
      number: 0,
      sides: [toSide(match, 'a'), toSide(match, 'b')],
      isBye: match.a === BYE || match.b === BYE,
      isPlayable: playable,
      isDone: playable && match.winner !== null,
      // 有比分時，比分已分出勝負就不能再手動點選勝者
      canPick: playable && (!tournament.scoring || !decidedByScore),
      isTie: tournament.scoring && playable && Boolean(match.result)
        && Number.isFinite(match.result.scoreA) && match.result.scoreA === match.result.scoreB
    };
  };

  let matchNumber = 0;
  const rounds = computed.rounds.map((matches, round) => ({
    index: round,
    name: getRoundName(round, computed.size),
    matches: matches.map((match) => {
      const view = toMatch(match);
      if (!view.isBye) {
        matchNumber += 1;
        view.number = matchNumber;
      }
      return view;
    })
  }));

  const columns = toColumns(rounds);
  const final = computed.rounds[computed.rounds.length - 1][0];
  const nameOf = (id) => (id && id !== BYE ? byId.get(id) ?? null : null);

  // 各輪的晉級名單（不含決賽，決賽看名次）
  const advancement = computed.rounds.slice(0, -1).map((matches, round) => {
    const advanced = matches.map((m) => nameOf(m.winner)).filter(Boolean);
    return {
      name: getRoundName(round + 1, computed.size),
      players: advanced,
      total: matches.length
    };
  });

  const playableMatches = [...computed.rounds.flat(), ...(computed.thirdPlace ? [computed.thirdPlace] : [])]
    .filter(isPlayable);

  return {
    size: computed.size,
    rounds,
    columns,
    thirdPlace: computed.thirdPlace ? { ...toMatch(computed.thirdPlace), number: matchNumber + 1 } : null,
    champion: nameOf(final.winner),
    runnerUp: nameOf(final.loser),
    third: computed.thirdPlace ? nameOf(computed.thirdPlace.winner) : null,
    advancement,
    progress: {
      done: playableMatches.filter((m) => m.winner !== null).length,
      // 單淘汰賽 N 人需要 N - 1 場（輪空不算），季軍戰另加 1 場
      total: tournament.participants.length - 1 + (computed.thirdPlace ? 1 : 0)
    }
  };
};

/**
 * 驗證並整理從 JSON 匯入的賽程，避免格式錯誤或惡意內容。
 * @param {unknown} data - JSON.parse 的結果
 * @returns {Object} 賽程狀態
 * @throws {Error} 格式不正確時
 */
export const parseTournament = (data) => {
  if (!data || typeof data !== 'object' || data.version !== STATE_VERSION) {
    throw new Error('不是可辨識的賽程檔案。');
  }
  if (!Array.isArray(data.participants) || !Array.isArray(data.slots)) {
    throw new Error('賽程檔案缺少參賽名單或籤位。');
  }

  const participants = data.participants.map((p, index) => ({
    id: String(p?.id ?? `p${index + 1}`),
    name: truncate(p?.name, MAX_NAME_LENGTH),
    seed: Number.isInteger(p?.seed) && p.seed > 0 ? p.seed : null
  }));
  // 種子重新編為連續的 1..K（依原本的種子序，同序則依名單順序），避免手改檔案造成重複或跳號
  participants
    .filter((p) => p.seed !== null)
    .sort((x, y) => x.seed - y.seed)
    .forEach((p, index) => {
      p.seed = index + 1;
    });
  const ids = new Set(participants.map((p) => p.id));
  const size = getBracketSize(participants.length);

  if (participants.length < MIN_PARTICIPANTS || participants.length > MAX_PARTICIPANTS || ids.size !== participants.length) {
    throw new Error('參賽名單不正確。');
  }
  const slots = data.slots.map((slot) => (slot === BYE ? BYE : String(slot)));
  const slotIds = slots.filter((slot) => slot !== BYE);
  if (slots.length !== size || slotIds.length !== participants.length
    || new Set(slotIds).size !== slotIds.length || slotIds.some((id) => !ids.has(id))) {
    throw new Error('籤位與參賽名單不一致。');
  }

  const results = {};
  const toScore = (value) => (Number.isFinite(value) ? value : null);
  Object.entries(data.results && typeof data.results === 'object' ? data.results : {}).forEach(([key, r]) => {
    if (!r || typeof r !== 'object') {
      return;
    }
    results[key] = {
      aId: String(r.aId),
      bId: String(r.bId),
      scoreA: toScore(r.scoreA),
      scoreB: toScore(r.scoreB),
      winnerId: r.winnerId === null || r.winnerId === undefined ? null : String(r.winnerId)
    };
  });

  return pruneResults({
    version: STATE_VERSION,
    title: truncate(data.title, MAX_TITLE_LENGTH),
    scoring: Boolean(data.scoring),
    thirdPlace: Boolean(data.thirdPlace) && participants.length >= 4,
    participants,
    slots,
    results
  });
};

/**
 * HTML 跳脫，匯出時避免參賽者名稱被當成標籤。
 * @param {unknown} value - 要輸出的值
 * @returns {string} 跳脫後的字串
 */
export const escapeHtml = (value) => String(value ?? '')
  .replace(/&/g, '&amp;')
  .replace(/</g, '&lt;')
  .replace(/>/g, '&gt;')
  .replace(/"/g, '&quot;')
  .replace(/'/g, '&#39;');

const EXPORT_CSS = `
*{box-sizing:border-box}
body{background:#fdfaf6;color:#4c4842;font-family:'Noto Sans TC',system-ui,-apple-system,'Microsoft JhengHei',sans-serif;margin:0;padding:24px 16px}
h1{font-size:1.5rem;margin:0 0 4px}
h2{font-size:1.1rem;margin:32px 0 12px}
.meta{color:#7a746b;font-size:.85rem;margin:0 0 24px}
.bracket{display:flex;overflow-x:auto;padding-bottom:8px}
.round{display:flex;flex:0 0 auto;flex-direction:column;width:208px}
.round:first-child{margin-left:auto}
.round:last-child{margin-right:auto}
.round__name{font-size:.8rem;font-weight:700;letter-spacing:.04em;margin-bottom:8px;padding:0 16px;text-align:center}
.round__matches{display:flex;flex:1;flex-direction:column}
.slot{align-items:center;display:flex;flex:1;padding:6px 16px;position:relative}
.round--left .slot::after,.round--left:not(.round--outer) .slot::before,.round--right .slot::before,.round--right:not(.round--outer) .slot::after,.round--final:not(.round--outer) .slot::before,.round--final:not(.round--outer) .slot::after{border-top:2px solid #e0dcd5;content:'';position:absolute;top:50%;width:16px}
.slot::after{right:0}
.slot::before{left:0}
.round--left.round--pair .slot::after{border-right:2px solid #e0dcd5;height:50%}
.round--right.round--pair .slot::before{border-left:2px solid #e0dcd5;height:50%}
.round--left.round--pair .slot:nth-child(even)::after,.round--right.round--pair .slot:nth-child(even)::before{border-bottom:2px solid #e0dcd5;border-top:0;top:0}
.round--final .match{border-color:rgba(217,130,54,.55);box-shadow:0 4px 12px rgba(217,130,54,.15)}
.match{background:#fff;border:1px solid #e0dcd5;border-radius:8px;overflow:hidden;width:100%}
.match--bye{opacity:.55}
.side{align-items:center;display:flex;font-size:.9rem;gap:6px;min-height:32px;padding:4px 8px}
.side+.side{border-top:1px solid #e0dcd5}
.side__seed{background:rgba(217,130,54,.15);border-radius:4px;color:#a66128;font-size:.7rem;font-weight:700;min-width:22px;padding:1px 4px;text-align:center}
.side__name{flex:1;overflow:hidden;text-overflow:ellipsis;white-space:nowrap}
.side__score{font-variant-numeric:tabular-nums;font-weight:700;min-width:24px;text-align:right}
.side--winner{background:rgba(217,130,54,.14);font-weight:700}
.side--loser .side__name{color:#9a948b;text-decoration:line-through}
.side--muted .side__name{color:#9a948b;font-style:italic}
.podium{display:flex;flex-wrap:wrap;gap:12px;list-style:none;margin:0;padding:0}
.podium li{background:#fff;border:1px solid #e0dcd5;border-radius:8px;min-width:160px;padding:12px 16px}
.podium small{color:#7a746b;display:block;font-size:.75rem}
.podium strong{font-size:1.05rem}
.advance{margin:0;padding-left:20px}
.advance li{margin-bottom:6px}
footer{color:#9a948b;font-size:.75rem;margin-top:40px}
@media print{body{background:#fff}.bracket{overflow:visible}}
`;

/**
 * 匯出可單獨開啟、列印或嵌入網頁的 HTML 檔。
 * @param {Object} tournament - 賽程狀態
 * @param {Date} [now] - 匯出時間
 * @returns {string} 完整的 HTML 文件
 */
export const exportHtml = (tournament, now = new Date()) => {
  const view = buildView(tournament);
  const title = escapeHtml(tournament.title || '賽程表');

  const renderSide = (side, showScore) => {
    const classes = ['side'];
    if (side.isWinner) classes.push('side--winner');
    if (side.isLoser) classes.push('side--loser');
    if (side.isBye || side.isPending) classes.push('side--muted');
    const seed = side.seed ? `<span class="side__seed">${side.seed}</span>` : '';
    const score = showScore ? `<span class="side__score">${side.score ?? ''}</span>` : '';
    return `<div class="${classes.join(' ')}">${seed}<span class="side__name">${escapeHtml(side.name)}</span>${score}</div>`;
  };

  const renderMatch = (match) => `<div class="match${match.isBye ? ' match--bye' : ''}">${
    match.sides.map((side) => renderSide(side, tournament.scoring && !match.isBye)).join('')}</div>`;

  const rounds = view.columns.map((column) => {
    const classes = ['round', `round--${column.side}`];
    if (column.isOuter) classes.push('round--outer');
    if (column.isPair) classes.push('round--pair');
    return `<div class="${classes.join(' ')}"><div class="round__name">${escapeHtml(column.name)}</div><div class="round__matches">${
      column.matches.map((match) => `<div class="slot">${renderMatch(match)}</div>`).join('')}</div></div>`;
  }).join('');

  const podium = [
    ['冠軍', view.champion],
    ['亞軍', view.runnerUp],
    ...(view.thirdPlace ? [['季軍', view.third]] : [])
  ].map(([label, p]) => `<li><small>${label}</small><strong>${p ? escapeHtml(p.name) : '待定'}</strong></li>`).join('');

  const thirdPlace = view.thirdPlace
    ? `<h2>季軍戰</h2><div style="max-width:208px">${renderMatch(view.thirdPlace)}</div>`
    : '';

  const advancement = view.advancement.map((stage) => `<li><strong>${escapeHtml(stage.name)}</strong>（${stage.players.length}/${stage.total}）：${
    stage.players.length ? stage.players.map((p) => escapeHtml(p.name)).join('、') : '尚未產生'}</li>`).join('');

  const timestamp = now.toLocaleString('zh-TW', { hour12: false });

  return `<!DOCTYPE html>
<html lang="zh-Hant">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>${title}</title>
<style>${EXPORT_CSS}</style>
</head>
<body>
<h1>${title}</h1>
<p class="meta">${tournament.participants.length} 位參賽・${escapeHtml(view.rounds[0].name)}・已完成 ${view.progress.done}/${view.progress.total} 場・更新於 ${escapeHtml(timestamp)}</p>
<div class="bracket">${rounds}</div>
${thirdPlace}
<h2>名次</h2>
<ul class="podium">${podium}</ul>
${advancement ? `<h2>晉級名單</h2><ul class="advance">${advancement}</ul>` : ''}
<footer>由 Involver 賽程表產生器建立</footer>
</body>
</html>
`;
};
