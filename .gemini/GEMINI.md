# Involver.tw �{���X����P�}�o���n

�ؼСG �T�O�{���X�M���B��Ū�B�@�P�A���ɹζ���@�Ĳv�A�çQ�� AI �٦�]�p Copilot�BGemini CLI�^�ǲ߻P��`�C

## 1. �ɮ׻P��Ƨ����c

  * �����ɮסG�C�ӭ������� `.cshtml` �M������ `.cshtml.cs` �ɮײզ��C
  * �������աG�̾ڥ\��η~���޿�N������´�b `Pages` ��Ƨ��U���l��Ƨ����C
      * �d�ҡG
        ```
        /Pages
        �u�w�w Articles
        �x   �u�w�w Create.cshtml
        �x   �u�w�w Create.cshtml.cs
        �x   �u�w�w Index.cshtml
        �x   �|�w�w Index.cshtml.cs
        �|�w�w Novels
             �u�w�w Create.cshtml
             �u�w�w Create.cshtml.cs
             �u�w�w Index.cshtml
             �|�w�w Index.cshtml.cs
        ```
  * �b���޲z�G������������b `Areas/Identity` ��Ƨ����C
  * �@�ε��ϡG�@�Ϊ������϶��]�p�����C�B�����^����m�b `Pages/Shared` ��Ƨ����C
  * �ҫ� (Models)�G
      * �j���O�ҫ��G����b `Models` ��Ƨ����A�P�����ҫ� `PageModel` �Ϥ��}�ӡC
      * ViewModels�G�i�H�إߤ@�� `Models/ViewModels` ��Ƨ��A�M���s�񬰯S�w View �ҳ]�p����Ƽҫ��C�o���U��Ϥ���Ʈw����ҫ��M�º鬰 UI ��ܪA�Ȫ��ҫ��C

## 2. C# �{���X���� (����޿�)

  * �֤߭�h�G��ƳB�z�u���b C# �����C�ɶq�N�Ҧ���ƪ��d�ߡB�z��B�ƧǡB�p�ⵥ�޿��b PageModel ���B�z�C�קK�b Razor �� JavaScript ������������~���޿�C

  * �R�W�G

      * ���O�W�� (PageModel, Model)�G�ϥ� `PascalCase`�A�Ҧp `ArticleModel`�C
      * ��k�W�١G�ϥ� `PascalCase`�A�Ҧp `OnGetAsync`�C
      * �p���ܼơG�ϥ� `_` �}�Y�� `camelCase`�A�Ҧp `_context`�C
      * �����ݩʡG�ϥ� `PascalCase`�A�Ҧp `public Article Article { get; set; }`�C

  * ��k�G

      * `OnGet` �� `OnPost` ��k���O����²�A�D�n�Ω�B�z�������B�j�w�M���ɦV�C
      * �Ҧ��D�P�B��k�����ӥH `Async` �����C

  * �ݩʡG

      * �ϥ� `[BindProperty]` �ݩʨӸj�w��洣�檺��ơC
      * �ϥ� `[TempData]` �Ӧb���P�ШD���ǻ��{�ɸ�ơC
      * ���קK�ϥ� `ViewData` �M `ViewBag`�A�u���ϥαj���O�ҫ��C

  * ���ѡG�T�O�Ҧ��s���禡�B���O�B��k�H�� Model �ѼƳ����M���� XML ���ѡC

## 3. Razor �аO (�b `.cshtml` �ɮפ�)

  * �����ҫ��G�C�ӭ������������w�q�j���O�ҫ��G`@model involver.Pages.Articles.IndexModel`�C
  * �{���X�϶��G�ϥ� `@` �Ÿ��ӼаO�{���X�C�ɶq�O�� Razor �ɮת�²��A�קK������ C# �޿�C
  * ���Ҩ�U�� (Tag Helpers)�G�u���ϥμ��Ҩ�U���Ӵ��N�ǲΪ� HTML ���U��k�A�H�W�[�{���X���iŪ�ʡC
      * �d�ҡG
        ```html
        <form method="post">
            <input asp-for="Article.Title" />
            <span asp-validation-for="Article.Title"></span>
            <button type="submit">�e�X</button>
        </form>
        ```
  * �������� (Partial Views)�G��󭫽ƥX�{�� UI ����]�p�p���d���B�@��²���϶��^�A���إߦ��������ϨӴ������ΩʡC

## 4. ��Ʀs��

  * �D�P�B�ާ@�G�@�ߨϥ� `async/await` �f�t `CancellationToken`�C
  * EF Core �d�ߡG
      * ����Ū�ާ@�A�ϥ� `AsNoTracking()` �H���ɮį�C
      * �d�߻y�y���������A���ɥiŪ�ʡC
      * �d�ҡG
        ```csharp
        var items = await _context.Posts
            .Where(p => p.IsPublished)
            .OrderByDescending(p => p.CreatedAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        ```
  * �}�o�Ҧ��G�ϥ� Code First ���覡�i��}�o�C

## 5. API �]�p

  * RESTful ����GAPI ���I����` RESTful �]�p��h�A�ϥ� `GET`, `POST`, `PUT`, `DELETE` �� HTTP ��k�ӹ����귽���ާ@�C

-----

## 6. �e�ݶ}�o�W�d

  * CSS�G

      * �ϥ� BEM (Block, Element, Modifier) ���R�W�W�d�Ӳ�´ CSS ���O�C
      * �ɶq�ϥ� CSS ���O�ӫD ID �ӿ�ܤ����A�H�Q��˦��мg�P���ΡC

  * JavaScript (�q�γW�d)�G

      * �y�k�G
          * �ϥ� ES6+ �y�k (`let`, `const`, `class`, `arrow function`, `async/await`)�C
          * �u���ϥ� `const`�A���D�ܼƻݭn�Q���s��Ȥ~�� `let`�C�Y�T�ϥ� `var`�C
          * �u���ϥνb�Y�禡 `=>`�A�S�O�O�b�^�I��Ƥ��C
          * �ϥμҪO�r���q (`` ` ``) �i��r������C
          * �ϥ��Y��۵��B��l `===` / `!==`�C
      * �榡�G
          * �ϥ� 2 �ӪŮ� �i���Y�ơA���ϥ� Tab�C
          * �C�泯�z�����������ϥΤ��� `;`�C
          * �r��@�ߨϥγ�޸� `'`�C
      * �R�W�G
          * �ܼƻP�禡�G�ϥ� `camelCase` (�Ҧp: `novelCount`)�C
          * ���O�P�غc�禡�G�ϥ� `PascalCase` (�Ҧp: `NovelService`)�C
          * �`�ơG�ϥ� `UPPER_SNAKE_CASE` (�Ҧp: `MAX_LIMIT`)�C
      * ���ѡG
          * ���}����ƩM���O�ݨϥ� JSDoc �榡���ѡA������γ~�B�Ѽ� (`@param`) �M��^�� (`@returns`)�C
            ```javascript
            /
             * �ھڤp��ID���o�p���ԲӸ�T
             * @param {string} novelId - �p�����ߤ@ID
             * @returns {Promise<Object>} �]�t�p�����D�M���e������
             */
            async function fetchNovelDetails(novelId) {
              // ...
            }
            ```
      * �ҲդơG
          * �@�ߨϥ� ES module (`import`/`export`)�C
      * ��m�G
          * �����M�ݪ� JavaScript ����b���������A�H�קK���׭�����V�C

  * Vue.js (���ӾɤJ�W�d)�G

      * �ɮ׵��c�G
          * �����ɮ�����m�b `Components` ��Ƨ����A�è̥\��έ����i������C
          * �C�Ӥ��������@�� `.vue` ��@�ɮפ��� (Single File Component)�C
      * �R�W�G
          * �����ɦW�G�ϥ� `PascalCase` (�Ҧp: `NovelCard.vue`)�C
          * ����b�ҪO���ϥΡG�ϥ� `<kebab-case>` (�Ҧp: `<novel-card>`)�C
      * �}�o��h�G
          * ��V�ƾڬy�G�Y���u `props` down, `events` up ����h�A�l�����������ק������ǤJ�� `props`�C
          * ����¾�d�G�O�����󪺳�@¾�d�A�קK�إ߹L���e�j�B����������C
          * ���A�޲z�G����h�Ӥ���@�ɪ����A�A���Ҽ{�ϥ� Pinia �����������A�޲z�u��C

## 7. �ۮe�ʻP�̩ۨ�

  * .NET �����G�H .Net 8 �W�漶�g C# �{���X�C
  * Bootstrap �����Gv4.3.1�C
  * �~���̩ۨʡG���D���沈�n�A�_�h�קK�ޤJ�s���~���̩ۨʡC�Y���ݭn�A�����b�ζ������X�Q�רû�����]�C
