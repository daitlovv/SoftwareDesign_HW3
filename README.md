## Система проверки работ на плагиат

Микросервисная система для проверки студенческих работ на плагиат.

### Запуск, остановка, проверка статуса

Сборка и запуск:
docker compose build
docker compose up -d

Проверка статуса:
docker compose ps
docker compose logs -f

Остановка:
docker compose down
docker compose down -v (с удалением данных)


### Сервисы и порты
- **Gateway API** - http://localhost:6000 (Swagger: http://localhost:6000/swagger)
- **FileStorage** - http://localhost:5001 (Swagger: http://localhost:5001/swagger)
- **FileAnalysis** - http://localhost:5003 (Swagger: http://localhost:5003/swagger)

### Команды для работы

### Проверка здоровья

curl http://localhost:6000/health
curl http://localhost:5001/health  
curl http://localhost:5003/health


### Загрузка работы студента

Вариант 1: curl с параметрами
curl -X POST "http://localhost:6000/works/upload?studentName=Ivan&workId=homework1" \
  -F "file=@/path/to/document.txt"

Вариант 2: с указанием Content-Type
curl -X POST "http://localhost:6000/works/upload?studentName=Maria&workId=lab2" \
  -H "Content-Type: multipart/form-data" \
  -F "file=@/path/to/file.txt;type=text/plain"

Вариант 3: PowerShell
curl -Method POST `
  -Uri "http://localhost:6000/works/upload?studentName=Alex&workId=hw3" `
  -Form @{file = Get-Item "C:\work.txt"}


### Получение отчетов

Все отчеты по работе:
curl "http://localhost:6000/works/homework1/reports"

С форматированием JSON:
curl "http://localhost:6000/works/lab2/reports" | python -m json.tool
curl "http://localhost:6000/works/hw3/reports" | jq .

### Получение напрямую из FileAnalysis
curl "http://localhost:5003/api/reports/homework1"


### Генерация облака слов

Получить URL облака слов:
curl "http://localhost:5003/api/reports/{report-id}/wordcloud"

Пример с реальным reportId:
curl "http://localhost:5003/api/reports/a1b2c3d4-5678-90ab-cdef-1234567890ab/wordcloud"


### Работа с файлами напрямую

curl "http://localhost:5001/api/files/{file-id}"

Содержимое файла как текст:
curl "http://localhost:5001/api/files/{file-id}/text"


### Алгоритм проверки на плагиат

Подготовка текста:

1. Удаление HTML/XML тегов: <[^>]+>
2. Удаление специальных символов: [^\p{L}\p{N}\s-']
3. Нормализация пробелов: \s+ → " "

Извлечение слов:
1. Разделение по пробелам, переносам, знакам препинания
2. Приведение к нижнему регистру
3. Фильтрация:
   - Длина слова ≥ 3 символа
   - Не только цифры
   - Не стоп-слова

Сравнение работ:

Для каждой новой работы:
1. Получить все предыдущие работы по тому же workId
2. Извлечь слова из текущей работы: words_current
3. Для каждой предыдущей работы:
   - Извлечь слова: words_previous
   - Рассчитать сходство


Расчет сходства

similarity = (common_words / max(total_words_current, total_words_previous)) * 100%

где:
- common_words = words_current [пересечение] words_previous
- total_words = уникальные слова в работе

Определение плагиата
Порог сходства: 70%

if similarity ≥ 70%:
    Плагиат обнаружен
    Отметить обе работы
    Записать имена студентов с процентом сходства
else:
    Плагиат не обнаружен
    Записать максимальное сходство


При обнаружении плагиата в новой работе:
1. Все предыдущие работы с similarity ≥ 70% помечаются как плагиат
2. В их отчет добавляется: "Обнаружен взаимный плагиат с работой студента {имя}"
3. Similarity обновляется до max значения


### Облако слов

Используется QuickChart API:
https://quickchart.io/wordcloud?text={текст}&width=600&height=400

Обработка для облака:
1. Подсчет частотности слов
2. Выбор топ-30 самых частых слов
3. Повторение слов по частоте (до 5 раз)
4. Кодирование URL для передачи в API

### Базы данных

### FileStorage (filestorage.db)
sql:
CREATE TABLE StoredFiles (
    FileId GUID PRIMARY KEY,
    OriginalName TEXT,
    StoragePath TEXT,
    Checksum TEXT,  -- SHA256
    Size INTEGER,
    UploadedAt DATETIME
)


### FileAnalysis (fileanalysis.db)
sql:
CREATE TABLE Reports (
    ReportId GUID PRIMARY KEY,
    WorkId TEXT,
    FileId GUID,
    StudentName TEXT,
    Result TEXT,
    HasPlagiarism BOOLEAN,
    Similarity REAL,
    CreatedAt DATETIME
)


### Мониторинг

Логи:
docker compose logs gateway
docker compose logs file-storage
docker compose logs file-analysis

Использование ресурсов:
docker stats


### Пример полного рабочего процесса

Загружаем первую работу:
curl -X POST "http://localhost:6000/works/upload?studentName=Ivan&workId=hw1" \
  -F "file=@work1.txt"

Загружаем вторую работу (с похожим содержанием):
curl -X POST "http://localhost:6000/works/upload?studentName=Maria&workId=hw1" \
  -F "file=@work2.txt"

Проверяем отчеты, увидим плагиат:
curl "http://localhost:6000/works/hw1/reports" | jq .

Генерируем облако слов для отчета:
curl "http://localhost:5003/api/reports/{reportId}/wordcloud"

Смотрим информацию о файлах:
curl "http://localhost:5001/api/files" | jq .


### Работа со Swagger

Получить Swagger UI HTML:
curl http://localhost:6000/swagger/index.html
curl http://localhost:5001/swagger/index.html
curl http://localhost:5003/swagger/index.html

Получение спецификаций API
curl http://localhost:6000/swagger/v1/swagger.json
curl http://localhost:5001/swagger/v1/swagger.json
curl http://localhost:5003/swagger/v1/swagger.json

### Хороший пример проверки плагиата между работами (с созданием файла), конкретный:

Создаем тестовые файлы:
echo "Программирование на C#. Основные концепции ООП: наследование, инкапсуляция, полиморфизм." > student1.txt
echo "Код на C#. Основные принципы ООП: Основные концепции ООП: наследование, инкапсуляция, полиморфизм." > student2.txt
echo "Программирование. Говорим не про ООП. Алгоритмы сортировки: пузырьковая, быстрая, слиянием. Сложность O(n log n)." > student3.txt
echo "Математический анализ: производные, интегралы, пределы функций." > student4.txt

Запускаем систему:
docker compose up -d

Проверим здоровье:
curl -s http://localhost:6000/health | grep -o '"status":"[^"]*"'

1я работа:
Загрузка работы Ивана
curl -s -X POST "http://localhost:6000/works/upload?studentName=Ivan&workId=hw1" -F "file=@student1.txt"

2я работа (похожая):
Загрузка работы Марии
curl -s -X POST "http://localhost:6000/works/upload?studentName=Maria&workId=hw1" -F "file=@student2.txt"

3я (минимум схожести с 1й и 2й)
Загрузка работы Алексея
curl -s -X POST "http://localhost:6000/works/upload?studentName=Alex&workId=hw1" -F "file=@student3.txt"

4я работа (совсем другая):
Загрузка работы Анны
curl -s -X POST "http://localhost:6000/works/upload?studentName=Anna&workId=hw2" -F "file=@student4.txt"


Отчеты по hw1:
curl -s "http://localhost:6000/works/hw1/reports"

Отчеты по hw2:
curl -s "http://localhost:6000/works/hw2/reports"

Проверка файлов в хранилище:
curl -s "http://localhost:5001/api/files" | grep -o '"FileId":"[^"]*"' | head -3

docker compose logs --tail=5

docker compose down

### Покажу еще отдельно, как тестировать облако слов:

Запускаем:
docker compose up -d

Загружаем работу:
echo "Программирование на C#. Основные концепции ООП. Наследование. Инкапсуляция. Полиморфизм. Абстракция. Классы. Объекты. Методы. Свойства." > cloudtest.txt
curl -X POST "http://localhost:6000/works/upload?studentName=CloudTest&workId=wordcloud" -F "file=@cloudtest.txt"

Получаем reportId простым способом:
curl "http://localhost:6000/works/wordcloud/reports"

Копируем ID из вывода и тестируем вручную:
curl "http://localhost:5003/api/reports/[ВСТАВЬ_СЮДА_ID]/wordcloud"
