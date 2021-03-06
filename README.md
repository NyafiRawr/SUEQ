# SUEQ | Server Universal Electronic Queue

<p>
<img src="https://img.shields.io/github/package-json/v/NyafiRawr/SUEQ" alt="GitHub package.json version" />
<a href="https://github.com/NyafiRawr/SUEQ/pulls"><img src="https://img.shields.io/github/issues-pr/NyafiRawr/SUEQ" alt="GitHub pull requests" /></a>
<p/>

REST API сервер на основе NodeJS: Express + Sequelize + nodemailer + Socket.IO
+ Защита HTTP-заголовков (helmet)  
+ Защита от спам-запросов: ограничение скорости (express-slow-down) и временная блокировка (express-rate-limit)  
+ Красивое и удобное логгирование происходящего (pino)
+ Аутентификация по почтовому адресу и паролю в котором используется хэширование с солью (bcrypt)
+ Авторизация основана на токенах доступа и обновления JWT (токен обновления хранится в базе данных)  
+ Реализованы функции активации аккаунта, восстановления пароля и восстановления аккаунта, если он был удален 
+ Реализована функция связи в реальном времени через Socket.IO с помощью этой библиотеки реализована система уведомлений о изменениях, таким образом интервальный опрос сервера клиентом не нужен и это снижает нагрузку на полосу соединения  
+ GUI настроек и запуск в виде службы для ОС Windows
+ Вы можете переделать сервер под себя - нужно только создать необходимые модели таблиц, контроллеры и исправить настройки (.env -> src/config.js)  

# Дополнительная информация

<!--ts-->
-   [Как запустить?](https://github.com/NyafiRawr/SUEQ/wiki/%D0%9A%D0%B0%D0%BA-%D0%B7%D0%B0%D0%BF%D1%83%D1%81%D1%82%D0%B8%D1%82%D1%8C%3F)
-   [Как создать базу данных и её пользователя в MySQL?](https://github.com/NyafiRawr/SUEQ/wiki/%D0%9A%D0%B0%D0%BA-%D1%81%D0%BE%D0%B7%D0%B4%D0%B0%D1%82%D1%8C-%D0%B1%D0%B0%D0%B7%D1%83-%D0%B4%D0%B0%D0%BD%D0%BD%D1%8B%D1%85-%D0%B8-%D0%B5%D1%91-%D0%BF%D0%BE%D0%BB%D1%8C%D0%B7%D0%BE%D0%B2%D0%B0%D1%82%D0%B5%D0%BB%D1%8F-%D0%B2-MySQL%3F)
-   [Маршруты по которым можно общаться с сервером](https://github.com/NyafiRawr/SUEQ/wiki/%D0%9C%D0%B0%D1%80%D1%88%D1%80%D1%83%D1%82%D1%8B-%D0%BF%D0%BE-%D0%BA%D0%BE%D1%82%D0%BE%D1%80%D1%8B%D0%BC-%D0%BC%D0%BE%D0%B6%D0%BD%D0%BE-%D0%BE%D0%B1%D1%89%D0%B0%D1%82%D1%8C%D1%81%D1%8F-%D1%81-%D1%81%D0%B5%D1%80%D0%B2%D0%B5%D1%80%D0%BE%D0%BC)
-   [Как протестировать маршруты?](https://github.com/NyafiRawr/SUEQ/wiki/%D0%9A%D0%B0%D0%BA-%D0%BF%D1%80%D0%BE%D1%82%D0%B5%D1%81%D1%82%D0%B8%D1%80%D0%BE%D0%B2%D0%B0%D1%82%D1%8C-%D0%BC%D0%B0%D1%80%D1%88%D1%80%D1%83%D1%82%D1%8B%3F)
<!--te-->
