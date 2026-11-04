/**
 * 首頁 Hero 的「光之樹」動畫（Canvas 2D）。
 *
 * - 枝枒會一直慢慢往上長；按住滑鼠或觸控時，最靠近、最朝向該處的枝枒會加速往那裡蔓延，
 *   同時有光點從根部送往那條枝枒，象徵創作者與讀者投注的心力。
 * - 沒有被引導的枝枒不會被放棄，只是長得比較慢；部分枝枒會成為「延伸創作」（藍色），
 *   它們的能量會沿著枝幹流回根部，讓主幹越來越粗壯。
 * - 現有的枝枒都長不動時，會從同一個根部長出全新的枝，就像舊系列開啟新的劇情支線；
 *   樹太茂密時，最舊的枝梢會慢慢淡化、退到背景成為景深，把前景的空間讓給新的枝枒。
 */

const ROOT_SELECTOR = '#story-hero';
const READY_CLASS = 'story-hero--ready';
const HOLDING_CLASS = 'story-hero--holding';

/** 世界座標的高度，畫布高度永遠對應到這個值，讓樹在不同尺寸下比例一致 */
const WORLD_HEIGHT = 1000;
const SEGMENT_LENGTH = 7;
const BASE_SPEED = 30;
const GUIDED_SPEED = 210;
/** 次要枝梢受引導的比例，避免所有枝梢擠向同一點 */
const SECONDARY_GUIDANCE = 0.3;
/** 擁擠度網格的格子大小與上限，讓枝枒自然錯開、長成樹冠 */
const CROWD_CELL = 24;
const CROWD_LIMIT = 9;
/** 枝梢進入引導點這個半徑內就不再轉向，讓枝枒在附近散開 */
const GUIDE_RADIUS = 80;
/** 開場加速的時間上限；主幹長到 introHeight 就提早結束 */
const INTRO_MAX_SECONDS = 12;
const INTRO_BOOST = 5;
const MAX_WIDTH = 20;
const MAX_GENERATION = 6;
const FEEDBACK_INTERVAL = 1.6;
const OFFERING_INTERVAL = 0.16;
/** 所有枝梢都停下後，隔多久從根部長出新枝（按住時會更快） */
const SHOOT_COOLDOWN = 4;
const SHOOT_COOLDOWN_HOLDING = 1;
/** 新枝離開根部的這段距離內不受擁擠度限制，才穿得過既有的枝幹 */
const SHOOT_FREE_LENGTH = 220;
/** 節點數超過上限的這個比例時，最舊的枝梢開始淡化、退到背景 */
const RECEDE_THRESHOLD = 0.9;
const RECEDE_CHAIN_LIMIT = 400;
const RECEDE_CONCURRENCY = 2;
const RECEDE_SECONDS = 3;
/** 背景枝枒的亮度與模糊程度；亮度會隨時間慢慢變淡，像越退越遠的景深 */
const BACKGROUND_ALPHA = 0.3;
const BACKGROUND_BLUR = 1.5;
const BACKGROUND_HALF_LIFE = 300;
const BACKGROUND_MIN_ALPHA = 0.03;
const BACKGROUND_REDRAW_SECONDS = 4;
const UP = -Math.PI / 2;

const COLORS = {
  core: '255, 244, 225',
  fork: '146, 208, 255',
  gold: '255, 184, 102',
};

/**
 * 取得範圍內的亂數。
 * @param {number} min - 最小值
 * @param {number} max - 最大值
 * @returns {number} 介於 min 與 max 之間的亂數
 */
const randomBetween = (min, max) => min + Math.random() * (max - min);

/**
 * 以指數衰減計算平滑插值係數，讓動畫不受影格率影響。
 * @param {number} speed - 收斂速度
 * @param {number} deltaSeconds - 影格間隔秒數
 * @returns {number} 0 ~ 1 的插值係數
 */
const damp = (speed, deltaSeconds) => 1 - Math.exp(-speed * deltaSeconds);

/**
 * 把角度差正規化到 -π ~ π。
 * @param {number} angle - 角度（弧度）
 * @returns {number} 正規化後的角度
 */
const wrapAngle = (angle) => Math.atan2(Math.sin(angle), Math.cos(angle));

/**
 * 預先繪製某個顏色的圓形光暈，之後用 drawImage 重複使用。
 * @param {string} rgb - 以逗號分隔的 RGB 字串
 * @returns {HTMLCanvasElement} 光暈圖
 */
const createGlowSprite = (rgb) => {
  const size = 64;
  const canvas = document.createElement('canvas');
  canvas.width = size;
  canvas.height = size;

  const context = canvas.getContext('2d');
  const gradient = context.createRadialGradient(size / 2, size / 2, 0, size / 2, size / 2, size / 2);
  gradient.addColorStop(0, `rgba(${rgb}, 1)`);
  gradient.addColorStop(0.25, `rgba(${rgb}, 0.6)`);
  gradient.addColorStop(1, `rgba(${rgb}, 0)`);
  context.fillStyle = gradient;
  context.fillRect(0, 0, size, size);
  return canvas;
};

/**
 * 會持續生長、可被引導的光之樹（只負責模擬，不負責繪製）。
 */
class LightTree {
  /**
   * @param {number} maxNodes - 節點上限，達到後樹就算長成
   * @param {number} introHeight - 開場加速要讓主幹長到的高度（世界單位）
   */
  constructor(maxNodes, introHeight) {
    this.maxNodes = maxNodes;
    this.introHeight = introHeight;
    this.reset();
  }

  /**
   * 從一顆種子重新開始。
   */
  reset() {
    this.root = this.createNode(0, 0, null, 'trunk');
    this.root.leaves = 1;
    this.nodes = [this.root];
    this.tips = [this.createTip(this.root, UP, 'trunk', 0)];
    this.buds = [];
    this.pulses = [];
    this.age = 0;
    this.feedbackTimer = 0;
    this.offeringTimer = 0;
    this.rootGlow = 0;
    this.focusTip = null;
    this.crowd = new Map();
    this.trunkHeight = 0;
    this.shootTimer = 0;
    this.receding = [];
    this.background = [];
    this.backgroundVersion = 0;
    this.backgroundTimer = 0;
  }

  /**
   * 是否還在開場加速：主幹還沒長到指定高度，且未超過時間上限。
   * @returns {boolean}
   */
  get isIntro() {
    return this.age < INTRO_MAX_SECONDS && this.trunkHeight < this.introHeight;
  }

  /**
   * 取得座標所在擁擠度網格的 key。
   * @param {number} x - X 座標
   * @param {number} y - Y 座標
   * @returns {string} 網格 key
   */
  cellOf(x, y) {
    return `${Math.floor(x / CROWD_CELL)},${Math.floor(y / CROWD_CELL)}`;
  }

  /**
   * 建立枝幹上的節點。
   * @param {number} x - X 座標
   * @param {number} y - Y 座標（往上為負）
   * @param {Object | null} parent - 父節點
   * @param {string} kind - trunk（主幹）、branch（分支）或 fork（延伸創作）
   * @returns {Object} 節點
   */
  createNode(x, y, parent, kind) {
    return { children: 0, isReceding: false, isRemoved: false, kind, leaves: 0, nourish: 0, parent, width: 1, x, y };
  }

  /**
   * 建立正在生長的枝梢。
   * @param {Object} node - 枝梢目前所在的節點
   * @param {number} angle - 生長方向（弧度）
   * @param {string} kind - 枝梢種類
   * @param {number} generation - 分岔代數
   * @param {number} [side=0] - 往左（-1）或往右（1）展開
   * @returns {Object} 枝梢
   */
  createTip(node, angle, kind, generation, side = 0) {
    return {
      angle,
      side,
      carry: 0,
      generation,
      grown: 0,
      guided: 0,
      isDead: false,
      isShoot: false,
      kind,
      life: kind === 'trunk' ? Infinity : randomBetween(240, 520) / (1 + generation * 0.3),
      node,
      seed: Math.random() * 1000,
      splitAt: this.nextSplitLength(kind),
      traveled: 0,
    };
  }

  /**
   * 決定下一次分岔前要長多長；主幹分岔得比較慢。
   * @param {string} kind - 枝梢種類
   * @returns {number} 長度（世界單位）
   */
  nextSplitLength(kind) {
    return kind === 'trunk' ? randomBetween(130, 210) : randomBetween(90, 170);
  }

  /**
   * 樹是否已經長成（不再有枝梢在生長）。
   * @returns {boolean}
   */
  get isMature() {
    return this.tips.length === 0;
  }

  /**
   * 計算枝梢受引導點吸引的程度：越近、越朝向引導點，吸引力越大。
   * @param {Object} tip - 枝梢
   * @param {{ x: number, y: number, strength: number }} guide - 引導點（世界座標）
   * @returns {number} 吸引力
   */
  pullOf(tip, guide) {
    if (guide.strength <= 0.01) {
      return 0;
    }

    const dx = guide.x - tip.node.x;
    const dy = guide.y - tip.node.y;
    const distance = Math.hypot(dx, dy);
    if (distance < GUIDE_RADIUS) {
      // 已經抵達的枝梢讓出引導，換下一條枝梢接著長過來
      return 0;
    }

    const alignment = (Math.cos(tip.angle) * dx + Math.sin(tip.angle) * dy) / distance;
    return (alignment * 0.5 + 0.5) / (1 + distance / 220);
  }

  /**
   * 調整枝梢方向：自然的擺動、向光性，以及被引導時轉向引導點。
   * @param {Object} tip - 枝梢
   * @param {{ x: number, y: number }} guide - 引導點
   * @param {number} deltaSeconds - 影格間隔秒數
   */
  steer(tip, guide, deltaSeconds) {
    const isFork = tip.kind === 'fork';
    const sway = Math.sin(this.age * 0.8 + tip.seed) + Math.sin(this.age * 1.9 + tip.seed * 2) * 0.5;
    tip.angle += sway * (isFork ? 0.5 : 0.3) * deltaSeconds;

    // 主幹朝正上方；分支則維持往外展開的角度，越外層越開，長成樹冠
    const lean = tip.kind === 'trunk' ? 0 : tip.side * Math.min(1.1, 0.45 + tip.generation * 0.12);
    tip.angle += wrapAngle(UP + lean - tip.angle) * (tip.kind === 'trunk' ? 0.5 : 0.3) * deltaSeconds;

    const distance = Math.hypot(guide.x - tip.node.x, guide.y - tip.node.y);
    if (tip.guided > 0.01 && distance > GUIDE_RADIUS) {
      const target = Math.atan2(guide.y - tip.node.y, guide.x - tip.node.x);
      tip.angle += wrapAngle(target - tip.angle) * 1.8 * tip.guided * deltaSeconds;
    }

    tip.angle = Math.min(0.35, Math.max(-Math.PI - 0.35, tip.angle));
  }

  /**
   * 讓枝梢往前長一段距離，必要時新增節點、分岔或停止生長。
   * @param {Object} tip - 枝梢
   * @param {number} distance - 這一影格要長的距離
   * @param {{ left: number, right: number, top: number }} bounds - 可生長範圍
   */
  grow(tip, distance, bounds) {
    tip.carry += distance;
    while (tip.carry >= SEGMENT_LENGTH && !tip.isDead) {
      tip.carry -= SEGMENT_LENGTH;
      const x = tip.node.x + Math.cos(tip.angle) * SEGMENT_LENGTH;
      const y = tip.node.y + Math.sin(tip.angle) * SEGMENT_LENGTH;
      const isOutside = x < bounds.left || x > bounds.right || y < bounds.top || y > -4;
      const cell = this.cellOf(x, y);
      const crowd = this.crowd.get(cell) ?? 0;
      const isFreeShoot = tip.isShoot && tip.traveled < SHOOT_FREE_LENGTH;
      const isCrowded = tip.kind !== 'trunk' && !isFreeShoot && crowd >= CROWD_LIMIT;
      if (isOutside || isCrowded || this.nodes.length >= this.maxNodes) {
        this.retire(tip);
        return;
      }

      this.crowd.set(cell, crowd + 1);
      const node = this.createNode(x, y, tip.node, tip.kind);
      node.leaves = 1;
      node.width = tip.node.width * 0.6;
      tip.node.children += 1;
      this.nodes.push(node);
      tip.node = node;
      tip.traveled += SEGMENT_LENGTH;
      if (tip.kind === 'trunk') {
        this.trunkHeight = Math.max(this.trunkHeight, -y);
      }
      tip.grown += SEGMENT_LENGTH;
      tip.life -= SEGMENT_LENGTH;

      if (tip.life <= 0) {
        this.retire(tip);
      } else if (tip.grown >= tip.splitAt) {
        this.split(tip);
      }
    }
  }

  /**
   * 枝梢分岔：原本的枝梢繼續長，旁邊再長出一條新的。
   * 從分支再分出去的，有機會成為「延伸創作」。
   * @param {Object} tip - 枝梢
   */
  split(tip) {
    tip.grown = 0;
    tip.splitAt = this.nextSplitLength(tip.kind);
    if (tip.generation >= MAX_GENERATION) {
      return;
    }

    let kind = 'branch';
    if (tip.kind === 'fork' || (tip.kind === 'branch' && Math.random() < 0.3)) {
      kind = 'fork';
    }

    // 分支優先往遠離主軸的一側長，避免整棵樹擠在中間
    const outward = tip.kind === 'trunk' ? (Math.random() < 0.5 ? -1 : 1) : tip.side;
    const side = Math.random() < 0.7 ? outward : -outward;
    const spread = randomBetween(0.5, 0.9);
    this.tips.push(this.createTip(tip.node, tip.angle + side * spread, kind, tip.generation + 1, side));
    tip.angle -= side * spread * 0.3;

    for (let node = tip.node; node; node = node.parent) {
      node.leaves += 1;
    }
  }

  /**
   * 停止生長，枝梢化為花苞。
   * @param {Object} tip - 枝梢
   */
  retire(tip) {
    tip.isDead = true;
    this.buds.push({ glow: 0, kind: tip.kind, node: tip.node, seed: tip.seed });
  }

  /**
   * 從根部長出一條全新的枝，象徵在同一個基礎上開啟新的劇情支線。
   */
  spawnShoot() {
    const side = Math.random() < 0.5 ? -1 : 1;
    const shoot = this.createTip(this.root, UP + side * randomBetween(0.2, 0.55), 'branch', 0, side);
    shoot.isShoot = true;
    shoot.life = randomBetween(600, 900);
    this.tips.push(shoot);
    this.root.leaves += 1;
    this.rootGlow = 1;
  }

  /**
   * 讓最舊的一條枝梢開始淡化：從花苞往回找到分岔點或主幹為止。
   */
  startRecede() {
    const tipNodes = new Set(this.tips.map((tip) => tip.node));
    for (let attempt = this.buds.length; attempt > 0; attempt -= 1) {
      const bud = this.buds.shift();
      const chain = [];
      let node = bud.node;
      while (
        node.parent
        && node.kind !== 'trunk'
        && !tipNodes.has(node)
        && node.children === (chain.length === 0 ? 0 : 1)
        && chain.length < RECEDE_CHAIN_LIMIT
      ) {
        chain.push(node);
        node = node.parent;
      }

      if (chain.length === 0) {
        // 主幹頂端之類退不到背景的花苞，放回隊伍尾端
        this.buds.push(bud);
        continue;
      }

      chain.forEach((item) => {
        item.isReceding = true;
      });
      this.receding.push({ age: 0, anchor: node, bud, chain });
      return;
    }
  }

  /**
   * 推進淡化中的枝梢；淡化完成後轉成背景線段，並把前景的空間讓出來。
   * 主幹的粗細不會因此變細，因為這些故事仍然是樹的一部分。
   * @param {number} deltaSeconds - 影格間隔秒數
   */
  updateReceding(deltaSeconds) {
    let hasMoved = false;
    this.receding = this.receding.filter((item) => {
      item.age += deltaSeconds;
      if (item.age < RECEDE_SECONDS) {
        return true;
      }

      item.chain.forEach((node) => {
        const cell = this.cellOf(node.x, node.y);
        this.crowd.set(cell, Math.max(0, (this.crowd.get(cell) ?? 1) - 1));
        this.background.push({
          bornAt: this.age,
          kind: node.kind,
          width: node.width * 0.8,
          x1: node.parent.x,
          x2: node.x,
          y1: node.parent.y,
          y2: node.y,
        });
        node.isRemoved = true;
      });

      const { anchor } = item;
      anchor.children -= 1;
      if (anchor.children === 0) {
        this.buds.push({ glow: 0, kind: anchor.kind, node: anchor, seed: item.bud.seed });
      }
      hasMoved = true;
      return false;
    });

    if (hasMoved) {
      this.nodes = this.nodes.filter((node) => !node.isRemoved);
      this.backgroundVersion += 1;
    }

    // 背景會越退越遠；淡到幾乎看不見的線段才真正移除，避免無限累積
    this.backgroundTimer += deltaSeconds;
    if (this.backgroundTimer >= BACKGROUND_REDRAW_SECONDS) {
      this.backgroundTimer = 0;
      const count = this.background.length;
      this.background = this.background.filter((segment) => this.backgroundAlphaOf(segment) >= BACKGROUND_MIN_ALPHA);
      if (this.background.length !== count) {
        this.backgroundVersion += 1;
      }
    }
  }

  /**
   * 背景線段目前的亮度。
   * @param {{ bornAt: number }} segment - 背景線段
   * @returns {number} 透明度
   */
  backgroundAlphaOf(segment) {
    return BACKGROUND_ALPHA * 0.5 ** ((this.age - segment.bornAt) / BACKGROUND_HALF_LIFE);
  }

  /**
   * 取得從某個節點回到根部的路徑。
   * @param {Object} node - 起點節點
   * @returns {Object[]} 由起點到根部的節點陣列
   */
  pathToRoot(node) {
    const path = [];
    for (let current = node; current; current = current.parent) {
      path.push(current);
    }
    return path;
  }

  /**
   * 延伸創作反哺原作：從枝梢送一顆光點沿著枝幹流回根部。
   */
  spawnFeedback() {
    const sources = [...this.tips.map((tip) => tip.node), ...this.buds.map((bud) => bud.node)]
      .filter((node) => node.kind !== 'trunk');
    if (sources.length === 0) {
      return;
    }

    const forks = sources.filter((node) => node.kind === 'fork');
    const pool = forks.length > 0 && Math.random() < 0.75 ? forks : sources;
    const source = pool[Math.floor(Math.random() * pool.length)];
    this.pulses.push({ isFeedback: true, kind: source.kind, path: this.pathToRoot(source), position: 0, speed: 150 });
  }

  /**
   * 投注心力：從根部送一顆光點到正在被引導的枝梢（或長成後最近的花苞）。
   * @param {Object} target - 目標節點
   */
  spawnOffering(target) {
    if (!target.parent) {
      return;
    }
    this.pulses.push({ isFeedback: false, kind: 'core', path: this.pathToRoot(target).reverse(), position: 0, speed: 360 });
  }

  /**
   * 找出最接近引導點的花苞。
   * @param {{ x: number, y: number }} guide - 引導點
   * @returns {Object | null} 花苞
   */
  nearestBud(guide) {
    let nearest = null;
    let nearestDistance = Infinity;
    this.buds.forEach((bud) => {
      const distance = Math.hypot(bud.node.x - guide.x, bud.node.y - guide.y);
      if (distance < nearestDistance) {
        nearest = bud;
        nearestDistance = distance;
      }
    });
    return nearest;
  }

  /**
   * 推進光點；反哺的光點經過的節點會累積養分，讓枝幹變粗。
   * @param {number} deltaSeconds - 影格間隔秒數
   */
  updatePulses(deltaSeconds) {
    this.pulses = this.pulses.filter((pulse) => {
      const previous = Math.floor(pulse.position);
      pulse.position += (pulse.speed / SEGMENT_LENGTH) * deltaSeconds;
      const last = pulse.path.length - 1;

      if (pulse.isFeedback) {
        for (let index = previous + 1; index <= Math.min(Math.floor(pulse.position), last); index += 1) {
          pulse.path[index].nourish += 0.02;
        }
      }

      if (pulse.position < last) {
        return true;
      }

      if (pulse.isFeedback) {
        this.rootGlow = 1;
      } else {
        const bud = this.buds.find((item) => item.node === pulse.path[last]);
        if (bud) {
          bud.glow = 1;
        }
      }
      return false;
    });
  }

  /**
   * 推進整棵樹一個影格。
   * @param {number} deltaSeconds - 影格間隔秒數
   * @param {{ x: number, y: number, strength: number }} guide - 引導點（世界座標）
   * @param {{ left: number, right: number, top: number }} bounds - 可生長範圍
   * @param {boolean} [withPulses=true] - 是否模擬光點流動
   */
  update(deltaSeconds, guide, bounds, withPulses = true) {
    this.age += deltaSeconds;
    const boost = this.isIntro ? INTRO_BOOST : 1;

    let strongest = 0;
    this.tips.forEach((tip) => {
      tip.pull = this.pullOf(tip, guide);
      strongest = Math.max(strongest, tip.pull);
    });

    this.focusTip = null;
    this.tips.forEach((tip) => {
      const isFocus = strongest > 0 && tip.pull === strongest;
      const relative = isFocus ? 1 : SECONDARY_GUIDANCE * (strongest > 0 ? tip.pull / strongest : 0) ** 4;
      tip.guided += (relative * guide.strength - tip.guided) * damp(3, deltaSeconds);
      if (isFocus && guide.strength > 0.3) {
        this.focusTip = tip;
      }

      this.steer(tip, guide, deltaSeconds);
      const nearGuide = Math.min(1, Math.hypot(guide.x - tip.node.x, guide.y - tip.node.y) / GUIDE_RADIUS);
      const baseSpeed = BASE_SPEED * (tip.kind === 'trunk' ? 1 : 0.7) * boost;
      this.grow(tip, (baseSpeed + GUIDED_SPEED * tip.guided * nearGuide) * deltaSeconds, bounds);
    });
    this.tips = this.tips.filter((tip) => !tip.isDead);

    const widthBlend = damp(1.2, deltaSeconds);
    this.nodes.forEach((node) => {
      const target = Math.min(MAX_WIDTH, 1.1 + Math.sqrt(node.leaves) * 1.5 + Math.sqrt(node.nourish) * 1.6);
      node.width += (target - node.width) * widthBlend;
    });

    this.rootGlow *= 1 - damp(1.5, deltaSeconds);
    this.buds.forEach((bud) => {
      bud.glow *= 1 - damp(1.2, deltaSeconds);
    });

    if (!withPulses) {
      return;
    }

    this.feedbackTimer += deltaSeconds;
    if (this.feedbackTimer >= FEEDBACK_INTERVAL) {
      this.feedbackTimer = 0;
      this.spawnFeedback();
    }

    this.offeringTimer += deltaSeconds;
    if (guide.strength > 0.3 && this.offeringTimer >= OFFERING_INTERVAL) {
      this.offeringTimer = 0;
      const target = this.focusTip?.node ?? (this.isMature ? this.nearestBud(guide)?.node : null);
      if (target) {
        this.spawnOffering(target);
      }
    }

    this.updatePulses(deltaSeconds);

    // 現有的枝梢都長不動了，就從根部長出新的支線
    if (this.isMature) {
      this.shootTimer += deltaSeconds;
      if (this.shootTimer >= (guide.strength > 0.3 ? SHOOT_COOLDOWN_HOLDING : SHOOT_COOLDOWN)) {
        this.shootTimer = 0;
        this.spawnShoot();
      }
    } else {
      this.shootTimer = 0;
    }

    if (this.receding.length < RECEDE_CONCURRENCY && this.nodes.length > this.maxNodes * RECEDE_THRESHOLD) {
      this.startRecede();
    }
    this.updateReceding(deltaSeconds);
  }
}

/**
 * 初始化首頁 Hero 的光之樹。Canvas 無法使用時保留 CSS 背景，不影響頁面內容。
 */
const initStoryHero = () => {
  const root = document.querySelector(ROOT_SELECTOR);
  const canvas = root?.querySelector('canvas');
  const context = canvas?.getContext('2d');
  if (!context) {
    return;
  }

  const isCompact = window.matchMedia('(max-width: 767.98px)').matches;
  const prefersReducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
  // 手機上文字壓在下半部，開場讓主幹先長過文字區，枝枒才看得到
  const tree = new LightTree(isCompact ? 1500 : 2800, isCompact ? 580 : 300);
  const sprites = {
    core: createGlowSprite(COLORS.core),
    fork: createGlowSprite(COLORS.fork),
    gold: createGlowSprite(COLORS.gold),
  };

  const view = { bounds: { left: -500, right: 500, top: -960 }, height: 0, pixelRatio: 1, rootX: 0, rootY: 0, scale: 1, width: 0 };
  const guide = { isHolding: false, screenX: 0, screenY: 0, strength: 0, x: 0, y: 0 };
  const motes = Array.from({ length: isCompact ? 36 : 70 }, () => ({
    phase: Math.random() * Math.PI * 2,
    size: randomBetween(4, 10),
    speed: randomBetween(6, 18),
    x: Math.random(),
    y: Math.random(),
  }));
  const buckets = new Map();
  // 退到背景的枝枒先畫在 scratch 上，再整張模糊貼到 layer；每一影格只要貼上 layer
  const background = {
    layer: document.createElement('canvas'),
    renderedAt: -Infinity,
    scratch: document.createElement('canvas'),
    version: -1,
  };

  /**
   * 依容器尺寸調整畫布；寬螢幕時樹種在文字右側，直式螢幕則置中。
   */
  const resize = () => {
    const { clientHeight, clientWidth } = root;
    if (clientWidth === 0 || clientHeight === 0) {
      return;
    }

    view.pixelRatio = Math.min(window.devicePixelRatio, isCompact ? 1.5 : 2);
    view.width = clientWidth;
    view.height = clientHeight;
    canvas.width = Math.round(clientWidth * view.pixelRatio);
    canvas.height = Math.round(clientHeight * view.pixelRatio);
    [background.layer, background.scratch].forEach((layer) => {
      layer.width = canvas.width;
      layer.height = canvas.height;
    });
    background.version = -1;

    view.scale = clientHeight / WORLD_HEIGHT;
    view.rootX = clientWidth * (clientWidth / clientHeight > 1.1 ? 0.7 : 0.5);
    view.rootY = clientHeight + 2;
    view.bounds = {
      left: -view.rootX / view.scale + 24,
      right: (clientWidth - view.rootX) / view.scale - 24,
      top: -WORLD_HEIGHT + 40,
    };
  };

  /**
   * 把引導點的螢幕座標換算成世界座標。
   */
  const syncGuide = () => {
    guide.x = (guide.screenX - view.rootX) / view.scale;
    guide.y = (guide.screenY - view.rootY) / view.scale;
  };

  /**
   * 畫一個光暈。
   * @param {HTMLCanvasElement} sprite - 光暈圖
   * @param {number} x - X 座標
   * @param {number} y - Y 座標
   * @param {number} size - 直徑
   * @param {number} alpha - 透明度
   */
  const drawGlow = (sprite, x, y, size, alpha) => {
    context.globalAlpha = alpha;
    context.drawImage(sprite, x - size / 2, y - size / 2, size, size);
  };

  /**
   * 依顏色與粗細分組畫出所有枝幹，減少 stroke 次數。
   * @param {number} breath - 整體呼吸亮度
   */
  const drawBranches = (breath) => {
    buckets.forEach((bucket) => {
      bucket.nodes.length = 0;
    });

    tree.nodes.forEach((node) => {
      if (!node.parent || node.isReceding) {
        return;
      }
      const color = node.kind === 'fork' ? 'fork' : 'gold';
      const width = Math.max(1, Math.round(node.width * 2) / 2);
      const key = `${color}|${width}`;
      if (!buckets.has(key)) {
        buckets.set(key, { color, nodes: [], width });
      }
      buckets.get(key).nodes.push(node);
    });

    context.globalAlpha = 1;
    buckets.forEach(({ color, nodes, width }) => {
      if (nodes.length === 0) {
        return;
      }

      context.beginPath();
      nodes.forEach((node) => {
        context.moveTo(node.parent.x, node.parent.y);
        context.lineTo(node.x, node.y);
      });

      const strength = Math.min(1, width / 8);
      context.strokeStyle = `rgba(${COLORS[color]}, ${0.05 * breath})`;
      context.lineWidth = width * 2.6 + 8;
      context.stroke();
      context.strokeStyle = `rgba(${COLORS[color]}, ${(0.45 + strength * 0.45) * breath})`;
      context.lineWidth = width;
      context.stroke();

      if (width >= 3) {
        context.strokeStyle = `rgba(${COLORS.core}, ${0.5 * strength * breath})`;
        context.lineWidth = width * 0.35;
        context.stroke();
      }
    });
  };

  /**
   * 世界座標到畫布像素的轉換。
   * @param {CanvasRenderingContext2D} target - 要設定的繪圖環境
   */
  const applyWorldTransform = (target) => {
    const { pixelRatio, scale } = view;
    target.setTransform(pixelRatio * scale, 0, 0, pixelRatio * scale, pixelRatio * view.rootX, pixelRatio * view.rootY);
  };

  /**
   * 重畫背景層：只有新增線段、尺寸改變，或隔一段時間要更新景深亮度時才重畫。
   */
  const renderBackground = () => {
    const isStale = tree.age - background.renderedAt >= BACKGROUND_REDRAW_SECONDS;
    if (background.version === tree.backgroundVersion && !isStale) {
      return;
    }
    background.version = tree.backgroundVersion;
    background.renderedAt = tree.age;

    const scratch = background.scratch.getContext('2d');
    scratch.setTransform(1, 0, 0, 1, 0, 0);
    scratch.clearRect(0, 0, background.scratch.width, background.scratch.height);
    scratch.globalCompositeOperation = 'lighter';
    scratch.lineCap = 'round';
    applyWorldTransform(scratch);

    const groups = new Map();
    tree.background.forEach((segment) => {
      const color = segment.kind === 'fork' ? 'fork' : 'gold';
      const width = Math.max(1, Math.round(segment.width));
      const alpha = Math.round(tree.backgroundAlphaOf(segment) * 100) / 100;
      const key = `${color}|${width}|${alpha}`;
      if (!groups.has(key)) {
        groups.set(key, { alpha, color, segments: [], width });
      }
      groups.get(key).segments.push(segment);
    });
    groups.forEach(({ alpha, color, segments, width }) => {
      scratch.beginPath();
      segments.forEach((segment) => {
        scratch.moveTo(segment.x1, segment.y1);
        scratch.lineTo(segment.x2, segment.y2);
      });
      scratch.strokeStyle = `rgba(${COLORS[color]}, ${alpha})`;
      scratch.lineWidth = width;
      scratch.stroke();
    });

    const layer = background.layer.getContext('2d');
    layer.setTransform(1, 0, 0, 1, 0, 0);
    layer.clearRect(0, 0, background.layer.width, background.layer.height);
    layer.filter = `blur(${BACKGROUND_BLUR * view.pixelRatio}px)`;
    layer.drawImage(background.scratch, 0, 0);
    layer.filter = 'none';
  };

  /**
   * 畫出正在淡化的枝梢：亮度、粗細與清晰度慢慢過渡到背景的樣子。
   */
  const drawReceding = () => {
    tree.receding.forEach(({ age, bud, chain }) => {
      const t = Math.min(1, age / RECEDE_SECONDS);
      const eased = t * t * (3 - 2 * t);
      const color = COLORS[bud.kind === 'fork' ? 'fork' : 'gold'];
      const last = chain[chain.length - 1];

      context.beginPath();
      context.moveTo(last.parent.x, last.parent.y);
      for (let index = chain.length - 1; index >= 0; index -= 1) {
        context.lineTo(chain[index].x, chain[index].y);
      }

      context.globalAlpha = 1;
      context.filter = `blur(${eased * BACKGROUND_BLUR * view.pixelRatio}px)`;
      context.strokeStyle = `rgba(${color}, ${0.05 * (1 - eased)})`;
      context.lineWidth = last.width * 2.6 + 8;
      context.stroke();
      context.strokeStyle = `rgba(${color}, ${0.8 + (BACKGROUND_ALPHA - 0.8) * eased})`;
      context.lineWidth = Math.max(1, last.width * (1 - 0.2 * eased));
      context.stroke();
      context.filter = 'none';
    });
  };

  /**
   * 繪製整個畫面。
   */
  const draw = () => {
    const { pixelRatio } = view;
    const breath = 0.9 + Math.sin(tree.age * 1.2) * 0.1;

    context.setTransform(1, 0, 0, 1, 0, 0);
    context.clearRect(0, 0, canvas.width, canvas.height);
    context.globalCompositeOperation = 'lighter';
    context.lineCap = 'round';

    context.setTransform(pixelRatio, 0, 0, pixelRatio, 0, 0);
    motes.forEach((mote) => {
      const twinkle = 0.5 + Math.sin(tree.age * 1.5 + mote.phase) * 0.5;
      drawGlow(sprites.gold, mote.x * view.width, mote.y * view.height, mote.size, 0.15 + twinkle * 0.25);
    });

    if (tree.background.length > 0) {
      renderBackground();
      context.setTransform(1, 0, 0, 1, 0, 0);
      context.globalAlpha = 1;
      context.drawImage(background.layer, 0, 0);
    }

    applyWorldTransform(context);
    drawGlow(sprites.gold, 0, 0, 260 + tree.rootGlow * 160, 0.25 + tree.rootGlow * 0.35);
    drawBranches(breath);
    drawReceding();

    tree.tips.forEach((tip) => {
      const headX = tip.node.x + Math.cos(tip.angle) * tip.carry;
      const headY = tip.node.y + Math.sin(tip.angle) * tip.carry;
      const sprite = tip.kind === 'fork' ? sprites.fork : sprites.gold;
      context.globalAlpha = 0.9;
      context.strokeStyle = `rgba(${COLORS[tip.kind === 'fork' ? 'fork' : 'gold']}, 0.9)`;
      context.lineWidth = Math.max(1, tip.node.width);
      context.beginPath();
      context.moveTo(tip.node.x, tip.node.y);
      context.lineTo(headX, headY);
      context.stroke();
      drawGlow(sprite, headX, headY, 22 + tip.guided * 40, 0.75);
      drawGlow(sprites.core, headX, headY, 8 + tip.guided * 10, 0.9);
    });

    tree.buds.forEach((bud) => {
      const twinkle = 0.5 + Math.sin(tree.age * 2 + bud.seed) * 0.5;
      const sprite = bud.kind === 'fork' ? sprites.fork : sprites.gold;
      drawGlow(sprite, bud.node.x, bud.node.y, 14 + twinkle * 8 + bud.glow * 40, 0.45 + bud.glow * 0.5);
    });

    tree.pulses.forEach((pulse) => {
      const index = Math.min(Math.floor(pulse.position), pulse.path.length - 2);
      const from = pulse.path[index];
      const to = pulse.path[index + 1] ?? from;
      const t = pulse.position - index;
      const x = from.x + (to.x - from.x) * t;
      const y = from.y + (to.y - from.y) * t;
      drawGlow(sprites[pulse.kind === 'fork' ? 'fork' : pulse.kind === 'core' ? 'core' : 'gold'], x, y, pulse.isFeedback ? 20 : 16, 0.85);
    });

    if (guide.strength > 0.01) {
      drawGlow(sprites.gold, guide.x, guide.y, 140 * guide.strength, 0.35 * guide.strength);
      drawGlow(sprites.core, guide.x, guide.y, 26 * guide.strength, 0.6 * guide.strength);
    }

    context.globalAlpha = 1;
  };

  new ResizeObserver(() => {
    resize();
    if (prefersReducedMotion) {
      draw();
    }
  }).observe(root);
  resize();

  if (prefersReducedMotion) {
    const idle = { strength: 0, x: 0, y: 0 };
    for (let step = 0; step < 6000 && !tree.isMature; step += 1) {
      tree.update(1 / 10, idle, view.bounds, false);
    }
    tree.update(10, idle, view.bounds, false);
    draw();
    root.classList.add(READY_CLASS);
    return;
  }

  /**
   * 記錄引導點的螢幕座標。
   * @param {PointerEvent} event - 指標事件
   */
  const trackGuide = (event) => {
    const rect = root.getBoundingClientRect();
    guide.screenX = event.clientX - rect.left;
    guide.screenY = event.clientY - rect.top;
  };

  /**
   * 放開滑鼠或手指，停止引導。
   */
  const release = () => {
    guide.isHolding = false;
    root.classList.remove(HOLDING_CLASS);
  };

  root.addEventListener('pointerdown', (event) => {
    if (event.button !== 0 || event.target.closest('a, button')) {
      return;
    }
    guide.isHolding = true;
    trackGuide(event);
    root.classList.add(HOLDING_CLASS);
    root.setPointerCapture(event.pointerId);
  });
  root.addEventListener('pointermove', (event) => {
    if (guide.isHolding) {
      trackGuide(event);
    }
  });
  root.addEventListener('pointerup', release);
  root.addEventListener('pointercancel', release);
  root.addEventListener('lostpointercapture', release);

  let lastTime = 0;
  let frameId = 0;

  /**
   * 每一影格的更新與繪製。
   * @param {number} time - requestAnimationFrame 時間戳記（毫秒）
   */
  const tick = (time) => {
    const deltaSeconds = lastTime === 0 ? 0 : Math.min(0.05, (time - lastTime) / 1000);
    lastTime = time;

    guide.strength += ((guide.isHolding ? 1 : 0) - guide.strength) * damp(guide.isHolding ? 3 : 2, deltaSeconds);
    syncGuide();
    tree.update(deltaSeconds, guide, view.bounds);

    motes.forEach((mote) => {
      mote.y -= (mote.speed * deltaSeconds) / view.height;
      if (mote.y < -0.02) {
        mote.x = Math.random();
        mote.y = 1.02;
      }
    });

    draw();
    frameId = requestAnimationFrame(tick);
  };

  let isVisible = false;

  /**
   * 只在 Hero 可見且分頁在前景時播放動畫，節省電力。
   */
  const syncPlayback = () => {
    cancelAnimationFrame(frameId);
    lastTime = 0;
    if (isVisible && document.visibilityState === 'visible') {
      frameId = requestAnimationFrame(tick);
    }
  };

  new IntersectionObserver(([entry]) => {
    isVisible = entry.isIntersecting;
    syncPlayback();
  }).observe(root);
  document.addEventListener('visibilitychange', syncPlayback);

  requestAnimationFrame(() => root.classList.add(READY_CLASS));
};

initStoryHero();
