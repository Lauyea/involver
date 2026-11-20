import { createApp } from 'vue';
import {
  MAX_NAME_LENGTH,
  MAX_PARTICIPANTS,
  MAX_TITLE_LENGTH,
  MIN_PARTICIPANTS,
  buildView,
  clearResult,
  createTournament,
  exportHtml,
  getBracketSize,
  getDefaultSeededCount,
  getRoundName,
  parseNames,
  parseTournament,
  redrawTournament,
  renameParticipant,
  setScore,
  setWinner
} from '/js/tools/bracket-core.js';

/** 進度存在瀏覽器的 key */
const STORAGE_KEY = 'involver.bracketBuilder.v1';

/**
 * 讀取瀏覽器中儲存的賽程；無痕模式或資料損毀時回傳 null。
 * @returns {Object|null} 賽程狀態
 */
const loadSaved = () => {
  try {
    const raw = window.localStorage.getItem(STORAGE_KEY);
    return raw ? parseTournament(JSON.parse(raw)) : null;
  } catch {
    return null;
  }
};

/**
 * 將賽程存到瀏覽器；null 代表刪除。
 * @param {Object|null} tournament - 賽程狀態
 */
const saveTournament = (tournament) => {
  try {
    if (tournament) {
      window.localStorage.setItem(STORAGE_KEY, JSON.stringify(tournament));
    } else {
      window.localStorage.removeItem(STORAGE_KEY);
    }
  } catch {
    // 儲存空間被封鎖時仍可正常使用，只是重新整理後不會保留
  }
};

/**
 * 讓瀏覽器下載文字檔。
 * @param {string} content - 檔案內容
 * @param {string} fileName - 檔名
 * @param {string} type - MIME type
 */
const downloadText = (content, fileName, type) => {
  const url = URL.createObjectURL(new Blob([content], { type: `${type};charset=utf-8` }));
  const link = document.createElement('a');
  link.href = url;
  link.download = fileName;
  document.body.appendChild(link);
  link.click();
  link.remove();
  setTimeout(() => URL.revokeObjectURL(url), 1000);
};

/**
 * 產生安全的檔名（去掉 Windows / macOS 不允許的字元）。
 * @param {string} title - 賽事名稱
 * @returns {string} 不含副檔名的檔名
 */
const toFileName = (title) => (title || '賽程表').replace(/[\\/:*?"<>|\s]+/g, '_').slice(0, 40) || '賽程表';

const app = createApp({
  data() {
    return {
      step: 'setup',
      form: {
        title: '',
        namesText: '',
        seededCount: 0,
        scoring: false,
        thirdPlace: false
      },
      // 使用者是否手動調整過種子人數；沒調整過時，隨名單人數自動給建議值
      isSeedTouched: false,
      tournament: null,
      isEditingNames: false,
      copied: false,
      notice: null,
      minParticipants: MIN_PARTICIPANTS,
      maxParticipants: MAX_PARTICIPANTS,
      maxNameLength: MAX_NAME_LENGTH,
      maxTitleLength: MAX_TITLE_LENGTH
    };
  },
  watch: {
    tournament(value) {
      saveTournament(value);
    },
    participantCount(count) {
      if (!this.isSeedTouched) {
        this.form.seededCount = getDefaultSeededCount(count);
      }
      if (count < 4) {
        this.form.thirdPlace = false;
      }
    }
  },
  computed: {
    names() {
      return parseNames(this.form.namesText);
    },
    participantCount() {
      return this.names.length;
    },
    bracketSize() {
      return getBracketSize(this.participantCount);
    },
    byeCount() {
      return this.participantCount >= this.minParticipants ? this.bracketSize - this.participantCount : 0;
    },
    roundNameOfSize() {
      return getRoundName(0, this.bracketSize);
    },
    seededCount() {
      return Math.max(0, Math.min(this.participantCount, this.form.seededCount));
    },
    /** 說明種子的分區效果，讓使用者知道 #1、#2 會在哪裡才相遇 */
    seedHint() {
      const seeds = this.seededCount;
      if (seeds < 2 || this.bracketSize < 4) {
        return '';
      }
      if (seeds < 4 || this.bracketSize < 8) {
        return '種子 1、2 分在不同半區，最快在決賽相遇。';
      }
      return '種子 1、2 最快在決賽相遇，種子 1～4 分在四個不同區，最快在準決賽相遇。';
    },
    canGenerate() {
      return this.participantCount >= this.minParticipants && this.participantCount <= this.maxParticipants;
    },
    view() {
      return this.tournament ? buildView(this.tournament) : null;
    },
    progressPercent() {
      if (!this.view || this.view.progress.total === 0) {
        return 0;
      }
      return Math.round((this.view.progress.done / this.view.progress.total) * 100);
    }
  },
  mounted() {
    const saved = loadSaved();
    if (saved) {
      this.tournament = saved;
      this.fillFormFrom(saved);
      this.step = 'bracket';
    }
  },
  methods: {
    /**
     * 填入範例名單。
     * @param {number} count - 人數
     */
    fillSample(count) {
      this.form.namesText = Array.from({ length: count }, (_, i) => `選手 ${i + 1}`).join('\n');
      this.isSeedTouched = false;
      this.form.seededCount = getDefaultSeededCount(count);
    },
    /**
     * 設定種子人數。
     * @param {string|number} value - 輸入值
     */
    setSeededCount(value) {
      const parsed = Math.floor(Number(value));
      this.isSeedTouched = true;
      this.form.seededCount = Number.isFinite(parsed) ? Math.max(0, Math.min(this.participantCount, parsed)) : 0;
    },
    /**
     * 把現有賽程的設定帶回設定表單（調整設定時使用）。
     * @param {Object} tournament - 賽程狀態
     */
    fillFormFrom(tournament) {
      const ordered = [...tournament.participants].sort((x, y) => (x.seed ?? Infinity) - (y.seed ?? Infinity));
      // 先標記為手動設定，名單人數改變時 watcher 就不會把種子人數蓋回預設值
      this.isSeedTouched = true;
      this.form.seededCount = tournament.participants.filter((p) => p.seed !== null).length;
      this.form.title = tournament.title;
      this.form.namesText = ordered.map((p) => p.name).join('\n');
      this.form.scoring = tournament.scoring;
      this.form.thirdPlace = tournament.thirdPlace;
    },
    /**
     * 是否已有登錄的賽果（覆蓋前需要確認）。
     * @returns {boolean} 有無賽果
     */
    hasResults() {
      return Boolean(this.tournament) && Object.keys(this.tournament.results).length > 0;
    },
    generate() {
      if (!this.canGenerate) {
        return;
      }
      if (this.hasResults() && !window.confirm('重新產生賽程會清除目前已登錄的賽果，確定要繼續嗎？')) {
        return;
      }
      this.tournament = createTournament({
        title: this.form.title,
        names: this.names,
        seededCount: this.seededCount,
        scoring: this.form.scoring,
        thirdPlace: this.form.thirdPlace
      });
      this.isEditingNames = false;
      this.notice = null;
      this.step = 'bracket';
    },
    redraw() {
      if (this.tournament.participants.every((p) => p.seed !== null)) {
        this.notice = { text: '所有參賽者都是種子，籤位固定，不需要重新抽籤。', isError: false };
        return;
      }
      if (this.hasResults() && !window.confirm('重新抽籤會清除目前已登錄的賽果，確定要繼續嗎？')) {
        return;
      }
      this.tournament = redrawTournament(this.tournament);
    },
    editSettings() {
      this.fillFormFrom(this.tournament);
      this.step = 'setup';
    },
    discard() {
      if (!window.confirm('確定要清除這個賽程嗎？清除後無法復原（已下載的進度檔不受影響）。')) {
        return;
      }
      this.tournament = null;
      this.step = 'setup';
    },
    /**
     * 點選勝者；再點一次取消。
     * @param {string} key - 賽果 key
     * @param {string} id - 參賽者 id
     */
    pickWinner(key, id) {
      this.tournament = setWinner(this.tournament, key, id);
    },
    /**
     * 登錄比分。
     * @param {string} key - 賽果 key
     * @param {'a'|'b'} side - 上方或下方
     * @param {string} value - 輸入值
     */
    updateScore(key, side, value) {
      const next = setScore(this.tournament, key, side, value);
      const result = next.results[key];
      // 兩邊比分都清空、也沒指定勝者時，整場視為未登錄
      const isEmpty = result && result.scoreA === null && result.scoreB === null && result.winnerId === null;
      this.tournament = isEmpty ? clearResult(next, key) : next;
    },
    /**
     * 修改參賽者名稱；清空時還原為原名稱（名稱不可為空）。
     * @param {string} id - 參賽者 id
     * @param {HTMLInputElement} input - 名稱輸入框
     */
    rename(id, input) {
      const trimmed = input.value.trim();
      if (!trimmed) {
        input.value = this.tournament.participants.find((p) => p.id === id)?.name ?? '';
        return;
      }
      this.tournament = renameParticipant(this.tournament, id, trimmed);
    },
    downloadHtml() {
      downloadText(exportHtml(this.tournament), `${toFileName(this.tournament.title)}.html`, 'text/html');
    },
    async copyHtmlAsync() {
      try {
        await navigator.clipboard.writeText(exportHtml(this.tournament));
        this.copied = true;
        setTimeout(() => {
          this.copied = false;
        }, 2000);
      } catch {
        this.notice = { text: '無法存取剪貼簿，請改用「下載 HTML」。', isError: true };
      }
    },
    downloadJson() {
      downloadText(JSON.stringify(this.tournament, null, 2), `${toFileName(this.tournament.title)}.json`, 'application/json');
    },
    /**
     * 載入之前下載的進度檔。
     * @param {Event} event - 檔案選擇事件
     */
    async importJsonAsync(event) {
      const input = event.target;
      const [file] = input.files;
      input.value = '';
      if (!file) {
        return;
      }
      if (this.tournament && !window.confirm('載入進度檔會取代目前的賽程，確定要繼續嗎？')) {
        return;
      }
      try {
        this.tournament = parseTournament(JSON.parse(await file.text()));
        this.isEditingNames = false;
        this.notice = { text: `已載入「${this.tournament.title || '賽程表'}」。`, isError: false };
        this.step = 'bracket';
      } catch (error) {
        this.notice = { text: `無法載入：${error instanceof SyntaxError ? '檔案不是有效的 JSON。' : error.message}`, isError: true };
      }
    }
  }
});

app.mount('#bracket-builder');
