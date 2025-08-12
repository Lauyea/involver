Razor Pages �{���X������n (Code Style Guide)

1. �ɮ׻P��Ƨ����c

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
  * �b���޲z�G�b`Areas/Identity`��Ƨ���
  * �@�ε��ϡG�@�Ϊ������϶��]�p�����C�B�����^����m�b `Pages/Shared` ��Ƨ����C
  * �j���O�ҫ��G�j���O�ҫ�����b `Models` ��Ƨ����A�P�����ҫ� `PageModel` �Ϥ��}�ӡC
  * ViewModels ��Ƨ��G�i�H�إߤ@�� `Models/ViewModels` ��Ƨ��A�M���s�񬰯S�w View �ҳ]�p����Ƽҫ��C�o���U��Ϥ���Ʈw����ҫ� (`Models`) �M�º鬰 UI ��ܪA�Ȫ��ҫ� (`ViewModels`)�C

2. C# �{���X����

  * �R�W�G
      * ���O�W�١]PageModel�BModel�^�G�ϥ� PascalCase�A�Ҧp `ArticleModel`�C
      * ��k�W�١]`OnGet`�B`OnPostAsync`�^�G�ϥ� PascalCase�C
      * �p���ܼơG�ϥ� `_` �}�Y�� camelCase�A�Ҧp `_context`�C
      * �����ݩʡG�ϥ� PascalCase�A�Ҧp `Article`�C
  * ��k�G
      * `OnGet` �� `OnPost` ��k���O���ɥi���²�A�D�n�Ω�B�z�������B�j�w�M���ɦV�C
      * �Ҧ��D�P�B��k�����ӥH `Async` �����C
      * �קK�b `.cshtml` ���]�t�������~���޿�A������ PageModel �αN��e�U���A�ȼh (Service Layer)�C
  * �ݩʡG
      * �ϥ� `[BindProperty]` �ݩʨӸj�w��洣�檺��ơC
      * �ϥ� `[TempData]` �Ӧb���P�ШD���ǻ���ơC
      * ���קK�ϥ� `ViewData` �M `ViewBag`�A�u���ϥαj���O�ҫ��C
  * ���ѡG�T�O�Ҧ��s���禡�B���O�B��k�H��Model parameters�������ѡC

3. Razor �аO (�b `.cshtml` �ɮפ�)

  * �����ҫ��G�C�ӭ������������w�q�j���O�ҫ��G`@model involver.Pages.Articles.IndexModel`�C
  * �{���X�϶��G�ϥ� `@` �Ÿ��ӼаO�{���X�C
      * �椺��F���G`@DateTime.Now`
      * �{���X�϶��G
        ```csharp
        @{
            var title = "Hello World";
        }
        ```
  * ����P�j��G
      * `if` �y�y�G
        ```csharp
        @if (Model.Articles.Count > 0)
        {
            <p>���峹</p>
        }
        ```
      * `foreach` �j��G
        ```csharp
        @foreach (var article in Model.Articles)
        {
            <li>@article.Title</li>
        }
        ```
  * ���Ҩ�U�� (Tag Helpers)�G
      * �u���ϥμ��Ҩ�U���Ӵ��N�ǲΪ� HTML ���U��k�C
      * �d�ҡG
        ```html
        <form method="post">
            <input asp-for="Article.Title" />
            <span asp-validation-for="Article.Title"></span>
            <button type="submit">�e�X</button>
        </form>
        ```
  * �������� (Partial Views)�G��󭫽ƥX�{�� UI ����]�p�p���d���B�@��²���϶��^�A���إߦ��������ϨӴ������ΩʡC

4. UI/UX ����

  * CSS�G
      * �ϥ� BEM (Block, Element, Modifier) ���R�W�W�d�Ӳ�´ CSS ���O�C
      * �ɶq�ϥ� CSS ���O�ӫD ID �ӿ�ܤ����C
  * JavaScript�G
      * �����M�ݪ� JavaScript ����b���������A�H�קK���׭�����V�C

5. ��Ʀs��

  * �ϥ� `async/await` �f�t `CancellationToken`�C
  * EF Core �d�ߡG
  
    * �ϥ� `AsNoTracking()` Ū�����ݰl�ܪ���ơC
    * �d�߻y�y�������A���ɥiŪ�ʡC
    * �ҡG
  
      ```csharp
      var items = await _context.Posts
          .Where(p => p.IsPublished)
          .OrderByDescending(p => p.CreatedAt)
          .ToListAsync(cancellationToken);
      ```

6. �ۮe��

  * �H .Net 8 �W�漶�g C# �{���X�C
  * Bootstrap ������ v4.3.1�C

7. �̩ۨ�
  * ���D���沈�n�A�_�h�קK�ޤJ�s���~���̩ۨʡC
  * �p�G�ݭn�s���̩ۨʡA�л�����]�C

8. API�]�p
  * RESTful ����GAPI ���I����` RESTful �]�p��h�A�ϥ� `GET`, `POST`, `PUT`, `DELETE` �� HTTP ��k�ӹ����귽���ާ@�C
