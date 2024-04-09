В папке IPJournal содержится решение и главный файл проекта AddressReader.cs
Также конфигурационный файл appsettings.json
в конфигурационный файле прописаны пути по умолчанию для   "file-log": "C:\\Projects\\Effective Mobile\\IPJournal\\IPJournal.log",и "file-output": "C:\\Projects\\Effective Mobile\\IPJournal\\IPJournal.txt"
Если файлы у вас будут находиться в другой директории, то нужно поменять пути
Эти пути и другие параметры берутся из конфигурационного файла, если они не указаны в параметрах запуска программы.
Примерный скрипт запуска программы из командной строки:
dotnet run --project "C:\Projects\Effective Mobile\IPJournal\IPJournal.csproj" -- --file-log "C:\Projects\Effective Mobile\IPJournal\IPJournal.log" --file-output "C:\Projects\Effective Mobile\IPJournal\IPJournal.txt" --address-start "192.168.1.1" --address-mask "24" --time-start "05.04.2024" --time-end "06.04.2024"

Также в папке IPJournal содержится файл IPJournal.log с адресами и временем доступа.

В папке IPJournal.Test находятся тесты по проверке логики фильтрации IP-адресов и с валидацией граничных значений
