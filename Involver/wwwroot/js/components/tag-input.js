/**
 * 小說、文章 Create/Edit 共用的標籤輸入框（Tagify）
 *
 * 由 Pages/Shared/_TagInputPartial.cshtml 產生的 .tag-input 容器讀取設定：
 * - data-tag-type：novel 或 article
 * - data-max-tags：標籤上限
 * - data-can-create：true 時可以直接新增標籤（特約作家、管理員）；
 *   false 時只能從現有標籤選擇（enforceWhitelist），新標籤要按「申請新標籤」
 *
 * 新增標籤前會呼叫相似標籤 API：只差結尾數字或符號的不能新增，相似的提示作者改用現有標籤。
 * 後端送出時會再檢查一次，這裡只是提示。
 */

const TAGS_API = '/api/v1/tags';
const SIMILAR_SUGGESTION_LIMIT = 3;

/**
 * 取得建議清單（Tagify whitelist）
 * @param {string} type - 作品類型（novel 或 article）
 * @returns {Promise<Array<{value: string}>>} 依使用次數排序的標籤
 */
const getTagSuggestionsAsync = async (type) => {
  const response = await fetch(`${TAGS_API}?type=${encodeURIComponent(type)}`);
  if (!response.ok) {
    throw new Error(`無法載入標籤建議清單（${response.status}）`);
  }

  return response.json();
};

/**
 * 取得與名稱相似的現有標籤
 * @param {string} name - 作者輸入的名稱
 * @param {string} type - 作品類型（novel 或 article）
 * @returns {Promise<Object>} { name, errorMessage, existing, duplicate, similar[] }
 */
const getSimilarTagsAsync = async (name, type) => {
  const query = `name=${encodeURIComponent(name)}&type=${encodeURIComponent(type)}`;
  const response = await fetch(`${TAGS_API}/similar?${query}`);
  if (!response.ok) {
    throw new Error(`無法檢查相似標籤（${response.status}）`);
  }

  return response.json();
};

/**
 * 粗略的比對用名稱，只用來判斷是否在 whitelist 裡；正式的格式統一由後端處理
 * @param {string} value - 標籤名稱
 * @returns {string} 去空白、轉小寫並做 NFKC 的名稱
 */
const toCompareKey = (value) => (value ?? '').normalize('NFKC').replace(/\s+/g, '').toLowerCase();

/**
 * 解析 input 現有的值（Tagify JSON 或逗號分隔）
 * @param {string} raw - input 的值
 * @returns {string[]} 標籤名稱
 */
const parseInitialValues = (raw) => {
  if (!raw) {
    return [];
  }

  try {
    const parsed = JSON.parse(raw);
    return Array.isArray(parsed) ? parsed.map((t) => t?.value).filter(Boolean) : [];
  } catch {
    return raw.split(',').map((t) => t.trim()).filter(Boolean);
  }
};

/**
 * 單一標籤輸入框
 */
class TagInput {
  /**
   * @param {HTMLElement} container - .tag-input 容器
   */
  constructor(container) {
    this.container = container;
    this.input = container.querySelector('.tag-input__field');
    this.hint = container.querySelector('.tag-input__hint');
    this.requestName = container.querySelector('.tag-input__request-name');
    this.requestButton = container.querySelector('.tag-input__request-button');
    this.type = container.dataset.tagType;
    this.maxTags = Number.parseInt(container.dataset.maxTags, 10);
    this.canCreate = container.dataset.canCreate === 'true';
    this.tagify = null;
    // 程式自己加入的標籤不再做相似檢查
    this.skipCheck = new Set();
  }

  /**
   * 載入建議清單並初始化 Tagify
   * @returns {Promise<void>}
   */
  async initAsync() {
    let whitelist = [];
    try {
      whitelist = await getTagSuggestionsAsync(this.type);
    } catch (error) {
      console.error(error);
    }

    // 編輯時作品原有的標籤（可能是待審標籤）不在建議清單裡，也要能保留
    const known = new Set(whitelist.map((t) => toCompareKey(t.value)));
    parseInitialValues(this.input.value).forEach((value) => {
      if (!known.has(toCompareKey(value))) {
        whitelist.push({ value });
        known.add(toCompareKey(value));
      }
    });

    this.tagify = new window.Tagify(this.input, {
      whitelist,
      maxTags: this.maxTags,
      enforceWhitelist: !this.canCreate,
      dropdown: {
        maxItems: 20,
        classname: 'tags-look',
        enabled: 0,
        closeOnSelect: false,
      },
    });

    this.tagify.on('add', (e) => {
      // 被擋下的無效標籤（不在清單裡、超過上限）Tagify 也會觸發 add，這種不檢查；
      // 一般作者只能從清單選，新標籤走「申請新標籤」，也不在這裡檢查
      if (!this.canCreate || e.detail.data.__isValid !== true) {
        return;
      }

      this.onAddAsync(e.detail.data.value);
    });
    this.tagify.on('invalid', (e) => {
      this.onInvalid(e.detail);
    });

    this.requestButton?.addEventListener('click', () => {
      this.requestNewTagAsync();
    });
    this.requestName?.addEventListener('keydown', (e) => {
      if (e.key === 'Enter') {
        // 避免送出整個表單
        e.preventDefault();
        this.requestNewTagAsync();
      }
    });
  }

  /**
   * 是否已在建議清單裡
   * @param {string} value - 標籤名稱
   * @returns {boolean}
   */
  isKnown(value) {
    return this.findKnown(value) !== null;
  }

  /**
   * 找出建議清單裡同一個標籤的寫法
   * @param {string} value - 標籤名稱
   * @returns {string|null} 清單裡的寫法；不在清單裡時為 null
   */
  findKnown(value) {
    const key = toCompareKey(value);
    const found = this.tagify.settings.whitelist
      .map((t) => (typeof t === 'string' ? t : t.value))
      .find((t) => toCompareKey(t) === key);
    return found ?? null;
  }

  /**
   * 加入標籤（不做相似檢查）。enforceWhitelist 時要先放進建議清單才加得進去。
   * @param {string} value - 標籤名稱
   */
  addTag(value) {
    if (!this.isKnown(value)) {
      this.tagify.settings.whitelist.push({ value });
    }

    this.skipCheck.add(toCompareKey(value));
    this.tagify.addTags([value]);
  }

  /**
   * 移除標籤
   * @param {string} value - 標籤名稱
   */
  removeTag(value) {
    const key = toCompareKey(value);
    const tagElm = this.tagify.getTagElms().find((elm) => toCompareKey(elm.__tagifyTagData?.value) === key);
    if (tagElm) {
      this.tagify.removeTags(tagElm);
    }
  }

  /**
   * 特約作家、管理員新增了不在建議清單裡的標籤：檢查相似標籤
   * @param {string} value - 標籤名稱
   * @returns {Promise<void>}
   */
  async onAddAsync(value) {
    const key = toCompareKey(value);
    if (this.skipCheck.has(key)) {
      this.skipCheck.delete(key);
      return;
    }

    // 已在清單裡（例如輸入「ｆａｔｅ」對到「Fate」）：換成清單裡的寫法
    const known = this.findKnown(value);
    if (known !== null) {
      if (known !== value) {
        this.removeTag(value);
        this.addTag(known);
      }
      return;
    }

    let result;
    try {
      result = await getSimilarTagsAsync(value, this.type);
    } catch (error) {
      console.error(error);
      return;
    }

    if (result.errorMessage) {
      this.removeTag(value);
      this.showMessage('danger', result.errorMessage);
      return;
    }

    if (result.existing) {
      if (result.existing.name !== value) {
        this.removeTag(value);
        this.addTag(result.existing.name);
      }
      return;
    }

    if (result.duplicate) {
      this.removeTag(value);
      this.showMessage(
        'danger',
        `「${result.name}」和現有標籤「${result.duplicate.name}」只差結尾的數字或符號，不能新增。`,
        [{ label: `改用「${result.duplicate.name}」`, onClick: () => this.addTag(result.duplicate.name) }],
      );
      return;
    }

    if (result.similar.length > 0) {
      this.showSimilar(result, value);
      return;
    }

    this.clearMessage();
  }

  /**
   * enforceWhitelist 擋下了不在建議清單裡的標籤：提示改按「申請新標籤」
   * @param {Object} detail - Tagify invalid 事件的 detail
   */
  onInvalid(detail) {
    const value = detail?.data?.value;
    if (!value || this.canCreate || !this.requestName) {
      return;
    }

    this.requestName.value = value;
    this.showMessage('warning', `「${value}」不是現有標籤，可以按「申請新標籤」送出申請。`);
  }

  /**
   * 一般作者申請新標籤
   * @returns {Promise<void>}
   */
  async requestNewTagAsync() {
    const value = this.requestName?.value.trim();
    if (!value) {
      this.showMessage('warning', '請輸入新標籤名稱。');
      return;
    }

    if (this.tagify.value.length >= this.maxTags) {
      this.showMessage('warning', `最多只能設定 ${this.maxTags} 個標籤。`);
      return;
    }

    let result;
    try {
      result = await getSimilarTagsAsync(value, this.type);
    } catch (error) {
      console.error(error);
      this.showMessage('danger', '無法檢查相似標籤，請稍後再試。');
      return;
    }

    if (result.errorMessage) {
      this.showMessage('danger', result.errorMessage);
      return;
    }

    if (result.existing) {
      this.addTag(result.existing.name);
      this.requestName.value = '';
      this.showMessage('info', `已經有「${result.existing.name}」這個標籤，已直接加入。`);
      return;
    }

    if (result.duplicate) {
      this.showMessage(
        'danger',
        `「${result.name}」和現有標籤「${result.duplicate.name}」只差結尾的數字或符號，不能新增。`,
        [{ label: `改用「${result.duplicate.name}」`, onClick: () => this.useExisting(result.duplicate.name) }],
      );
      return;
    }

    if (result.similar.length > 0) {
      this.showSimilar(result, null);
      return;
    }

    this.addPendingTag(result.name);
  }

  /**
   * 加入申請中的新標籤
   * @param {string} value - 標籤名稱
   */
  addPendingTag(value) {
    this.addTag(value);
    if (this.requestName) {
      this.requestName.value = '';
    }

    this.showMessage('info', `已加入「${value}」。送出後會成為待審標籤。`);
  }

  /**
   * 改用現有標籤
   * @param {string} value - 現有標籤名稱
   */
  useExisting(value) {
    this.addTag(value);
    if (this.requestName) {
      this.requestName.value = '';
    }

    this.clearMessage();
  }

  /**
   * 顯示相似標籤的提示
   * @param {Object} result - 相似標籤 API 的結果
   * @param {string|null} addedValue - 已加入 Tagify 的值（特約作家）；null 表示還沒加入（一般作者申請中）
   */
  showSimilar(result, addedValue) {
    const suggestions = result.similar.slice(0, SIMILAR_SUGGESTION_LIMIT);
    const names = suggestions.map((s) => `「${s.name}」`).join('、');
    const actions = suggestions.map((s) => ({
      label: `改用「${s.name}」`,
      onClick: () => {
        if (addedValue) {
          this.removeTag(addedValue);
        }
        this.useExisting(s.name);
      },
    }));

    if (addedValue) {
      actions.push({ label: `保留「${result.name}」`, onClick: () => this.clearMessage() });
    } else {
      actions.push({ label: `仍要申請「${result.name}」`, onClick: () => this.addPendingTag(result.name) });
    }

    this.showMessage('warning', `你是不是要用${names}？`, actions);
  }

  /**
   * 在輸入框下方顯示提示
   * @param {string} level - Bootstrap alert 樣式（info、warning、danger）
   * @param {string} text - 提示文字
   * @param {Array<{label: string, onClick: Function}>} [actions] - 按鈕
   */
  showMessage(level, text, actions = []) {
    this.clearMessage();

    const alert = document.createElement('div');
    alert.className = `alert alert-${level} py-2 mb-0 d-flex flex-wrap align-items-center gap-2`;
    alert.setAttribute('role', 'alert');

    const message = document.createElement('span');
    message.textContent = text;
    alert.appendChild(message);

    actions.forEach((action) => {
      const button = document.createElement('button');
      button.type = 'button';
      button.className = 'btn btn-sm btn-outline-primary';
      button.textContent = action.label;
      button.addEventListener('click', () => {
        action.onClick();
      });
      alert.appendChild(button);
    });

    this.hint.appendChild(alert);
  }

  /**
   * 清除提示
   */
  clearMessage() {
    this.hint.replaceChildren();
  }
}

document.querySelectorAll('.tag-input').forEach((container) => {
  new TagInput(container).initAsync();
});
