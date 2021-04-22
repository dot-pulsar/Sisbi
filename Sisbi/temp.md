# Sisbi.ru

## Защита

- [ ] SSL
- [ ] JWT
- [ ] Хэширование пароля SHA-512 + соль
- [ ] Блокировка, после 10 неверных попыток входа в аккаунт
- [ ] 2FA

## База данных

### Profile

| Name              | Type    | NOT NULL | Default            |
| ----------------- | ------- | -------- | ------------------ |
| id                | uuid    | &check;  | uuid_generate_v4() |
| first_name        | text    | &cross;  | NULL               |
| second_name       | text    | &cross;  | NULL               |
| middle_name       | text    | &cross;  | NULL               |
| phone             | text    | &cross;  | NULL               |
| email             | text    | &cross;  | NULL               |
| password          | text    | &check;  | NULL               |
| salt              | text    | &check;  | NULL               |
| date_of_birth     | bigint  | &cross;  | NULL               |
| address           | text    | &cross;  | NULL               |
| registration_date | bigint  | &check;  | NULL               |
| opt               | integer | &cross;  | NULL               |
| email_confirmed   | boolean | &cross;  | false              |
| phone_confirmed   | boolean | &cross;  | false              |

### Access

| Role          | GET     |  POST    | PUT     | DELETE     |
|:--------------|:--------|:---------|:--------|:-----------|
| Worker        | &check; | &cross;  | &cross; | &cross;    |
| Employer      | &check; | &cross;  | &cross; | &cross;    |
| Moderator     | &check; | &cross;  | &cross; | &cross;    |
| Administrator | &check; | &cross;  | &cross; | &cross;    |

1) Индикация обработки вакансии/резюме
2) Добавить место работы
3) Поиск вакансий/резюме. `.../resumes?name=&city_id=&work_experience=&salary_min=&salary_max=`
4) Избранное
5) Отклики
6) Получать контакты
7) Добавить OrderBy `(Skip/Take)`

- api/v1/account/otp/send
- api/v1/account/otp/confirm
- api/v1/account/password/change
- api/v1/account/password/restore
- api/v1/account/signup
- api/v1/account/signin

- api/v1/city
- api/v1/worker/places_of_work
- api/v1/worker/resumes
- api/v1/employer/resumes

~~~ html
<meta property="og:title" content="Пример заголовка статьи">
<meta property="og:site_name" content="название сайта">
<meta property="og:type" content="article">
<meta property="og:url" content="http://example.com/пример-заголовка-статьи">
<meta property="og:image" content="http://example.com/картинка_статьи.jpg">
<meta property="og:description" content="Краткое описание статьи.">
~~~

~~~ c#
#region External Login

[HttpGet, Route("login")]
public async Task<IActionResult> LoginEL()
{
    return Ok(await _vkontakteService.Get());
}

[HttpGet, Route("response")]
public async Task<IActionResult> ResponseEL(string code = null)
{
    var clientId = "7799405";
    var clientSecret = "a8x3WZSTxIvU1lHknRQr";
    var redirectUri = "https://localhost:5001/account/response";

    var uri =
        $"https://oauth.vk.com/access_token?client_id={clientId}&client_secret={clientSecret}&redirect_uri={redirectUri}&code={code}";
    return Redirect(uri);
}

[Route("google-login")]
public IActionResult GoogleLogin()
{
    var properties = new AuthenticationProperties {RedirectUri = Url.Action("GoogleResponse")};
    return Challenge(properties, GoogleDefaults.AuthenticationScheme);
}

[Route("google-response")]
public async Task<IActionResult> GoogleResponse()
{
    var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

    if (result.Principal != null)
    {
        var claims = result
            .Principal
            .Identities
            .FirstOrDefault()
            ?.Claims.Select(claim => new
            {
                claim.Issuer,
                claim.OriginalIssuer,
                claim.Type,
                claim.Value
            });

        return Ok(claims);
    }

    return BadRequest(new
    {
        success = false,
        description = "You are not logged in with google."
    });
}

[Route("vk-login")]
public IActionResult VkLogin()
{
    var properties = new AuthenticationProperties {RedirectUri = Url.Action("VkResponse")};

    return Challenge(properties, VkontakteAuthenticationDefaults.AuthenticationScheme);
}

[Route("vk-response")]
public async Task<IActionResult> VkResponse()
{
    var result = await HttpContext.AuthenticateAsync(VkontakteAuthenticationDefaults.AuthenticationScheme);

    if (result.Principal != null)
    {
        var claims = result
            .Principal
            .Identities
            .FirstOrDefault()
            ?.Claims.Select(claim => new
            {
                claim.Issuer,
                claim.OriginalIssuer,
                claim.Type,
                claim.Value
            });

        return Ok(claims);
    }

    return BadRequest(new
    {
        success = false,
        description = "You are not logged in with google."
    });
}

#endregion
~~~