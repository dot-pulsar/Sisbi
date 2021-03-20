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


~~~ html
<meta property="og:title" content="Пример заголовка статьи">
<meta property="og:site_name" content="название сайта">
<meta property="og:type" content="article">
<meta property="og:url" content="http://example.com/пример-заголовка-статьи">
<meta property="og:image" content="http://example.com/картинка_статьи.jpg">
<meta property="og:description" content="Краткое описание статьи.">
~~~