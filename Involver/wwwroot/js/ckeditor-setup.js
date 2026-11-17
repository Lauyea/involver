/**
 * Involver 共用的 CKEditor 5 建立邏輯。
 *
 * CKEditor 由 LibMan 安裝到 wwwroot/lib/ckeditor5，並透過 _Layout.cshtml 的 import map
 * 對應到 'ckeditor5' 與 'ckeditor5/translations/zh.js'，不需要打包工具。
 * 文章表單（_ClassicEditorPartial）與留言區（comments.js）都從這裡建立編輯器，
 * 確保兩邊的工具列與外掛一致。
 */
import {
  Alignment,
  AutoImage,
  AutoLink,
  BlockQuote,
  Bold,
  ClassicEditor,
  Essentials,
  FindAndReplace,
  FontBackgroundColor,
  FontColor,
  Heading,
  HorizontalLine,
  Image,
  ImageCaption,
  ImageInsertViaUrl,
  ImageResize,
  ImageStyle,
  ImageTextAlternative,
  ImageToolbar,
  Indent,
  Italic,
  Link,
  List,
  MediaEmbed,
  Paragraph,
  PastePlainText,
  RemoveFormat,
  SourceEditing,
  Strikethrough,
  Table,
  TableCaption,
  TableToolbar,
  TextTransformation,
  Underline,
  WordCount
} from 'ckeditor5';
import zhTranslations from 'ckeditor5/translations/zh.js';

/**
 * 編輯器載入的外掛。
 * 對應舊版 Online Builder 打包的外掛組合；圖片只開放以網址插入，上傳功能尚未串接。
 */
const PLUGINS = [
  Alignment,
  AutoImage,
  AutoLink,
  BlockQuote,
  Bold,
  Essentials,
  FindAndReplace,
  FontBackgroundColor,
  FontColor,
  Heading,
  HorizontalLine,
  Image,
  ImageCaption,
  ImageInsertViaUrl,
  ImageResize,
  ImageStyle,
  ImageTextAlternative,
  ImageToolbar,
  Indent,
  Italic,
  Link,
  List,
  MediaEmbed,
  Paragraph,
  PastePlainText,
  RemoveFormat,
  SourceEditing,
  Strikethrough,
  Table,
  TableCaption,
  TableToolbar,
  TextTransformation,
  Underline,
  WordCount
];

/** 主工具列按鈕 */
const TOOLBAR_ITEMS = [
  'heading',
  '|',
  'bold',
  'italic',
  'underline',
  'strikethrough',
  'fontBackgroundColor',
  'fontColor',
  'removeFormat',
  'findAndReplace',
  '|',
  'alignment',
  'bulletedList',
  'numberedList',
  'outdent',
  'indent',
  'horizontalLine',
  '|',
  'link',
  'insertImage',
  'blockQuote',
  'insertTable',
  'mediaEmbed',
  'sourceEditing',
  'undo',
  'redo'
];

/**
 * 取得頂端 sticky 導覽列 (#TopHead) 的高度，讓編輯器的 sticky 工具列不會被遮住。
 * @returns {number} 導覽列高度 (px)
 */
const getStickyHeaderHeight = () => document.querySelector('#TopHead')?.offsetHeight ?? 0;

/**
 * 以 Involver 的共用設定建立 ClassicEditor。
 * @param {HTMLElement} element - 要轉換成編輯器的 textarea 或元素
 * @param {Object} [options] - 額外選項
 * @param {number} [options.maxLength] - 字元數上限；超過時會呼叫 onLimitExceeded
 * @param {(characters: number) => void} [options.onLimitExceeded] - 字元數超過上限時的回呼
 * @param {HTMLElement|null} [options.wordCountContainer] - 要放置字數統計的容器
 * @returns {Promise<ClassicEditor>} 建立完成的編輯器實體
 */
export async function createClassicEditorAsync(element, { maxLength, onLimitExceeded, wordCountContainer } = {}) {
  const editor = await ClassicEditor.create(element, {
    licenseKey: 'GPL',
    language: 'zh',
    translations: [zhTranslations],
    plugins: PLUGINS,
    toolbar: {
      items: TOOLBAR_ITEMS,
      shouldNotGroupWhenFull: false
    },
    ui: {
      viewportOffset: {
        top: getStickyHeaderHeight()
      }
    },
    link: {
      addTargetToExternalLinks: true
    },
    image: {
      toolbar: [
        'imageTextAlternative',
        'imageStyle:inline',
        'imageStyle:block',
        'imageStyle:side'
      ]
    },
    table: {
      contentToolbar: [
        'tableColumn',
        'tableRow',
        'mergeTableCells'
      ]
    },
    mediaEmbed: {
      previewsInData: true
    },
    wordCount: {
      displayWords: false,
      onUpdate: stats => {
        if (maxLength && stats.characters > maxLength && onLimitExceeded) {
          onLimitExceeded(stats.characters);
        }
      }
    }
  });

  if (wordCountContainer) {
    wordCountContainer.appendChild(editor.plugins.get('WordCount').wordCountContainer);
  }

  return editor;
}
