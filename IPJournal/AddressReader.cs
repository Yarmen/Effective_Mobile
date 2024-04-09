using Microsoft.Extensions.Configuration;
using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;


namespace IPJournal
{
    public class AddressReader
    {
        public static IConfiguration? Configuration { get; set; }

        /// <summary>
        /// Строит конфигурацию из файла appsettings.json и аргументов командной строки.
        /// </summary>
        /// <param name="args">Аргументы командной строки.</param>
        /// <returns>Объект конфигурации.</returns>
        static IConfiguration BuildConfiguration(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddCommandLine(args)
                .Build();

            return configuration;
        }

        static void Main(string[] args)
        {
            try
            {
                Configuration = BuildConfiguration(args);

                string logFilePath = Configuration["file-log"] ?? throw new ArgumentException("Parameter --file-log is required.");
                string outputFilePath = Configuration["file-output"] ?? throw new ArgumentException("Parameter --file-output is required.");
                string? addressStart = Configuration["address-start"];
                string? addressMask = Configuration["address-mask"];
                string? timeStart = Configuration["time-start"];
                string? timeEnd = Configuration["time-end"];

                var ipCounts = new Dictionary<string, int>();
                ReadLogFile(logFilePath, ipCounts, addressStart, addressMask, timeStart, timeEnd);
                WriteResults(outputFilePath, ipCounts);
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An unexpected error occurred: {ex.Message}");
            }
        }

        /// <summary>
        /// Читает файл журнала и подсчитывает количество вхождений IP-адресов.
        /// </summary>
        /// <param name="filePath">Путь к файлу журнала.</param>
        /// <param name="ipCounts">Словарь для хранения количества IP-адресов.</param>
        /// <param name="addressStart">Начальный IP-адрес для фильтрации.</param>
        /// <param name="addressMask">Сетевая маска для фильтрации.</param>
        /// <param name="timeStart">Начальное время для фильтрации.</param>
        /// <param name="timeEnd">Конечное время для фильтрации.</param>
        static void ReadLogFile(string filePath, Dictionary<string, int> ipCounts, string? addressStart, string? addressMask, string? timeStart, string? timeEnd)
        {
            // Инициализация параметров фильтрации
            var filterParams = InitializeFilterParams(addressStart, addressMask, timeStart, timeEnd);

            try
            {
                using (var reader = new StreamReader(filePath))
                {
                    string? line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        ProcessLogLine(line, ipCounts, filterParams);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Произошла ошибка при чтении файла лога: {e.Message}");
            }
        }

        /// <summary>
        /// Инициализирует параметры фильтрации на основе входных данных.
        /// </summary>
        /// <param name="addressStart">Начальный IP-адрес для фильтрации.</param>
        /// <param name="addressMask">Сетевая маска для фильтрации.</param>
        /// <param name="timeStart">Начальное время для фильтрации.</param>
        /// <param name="timeEnd">Конечное время для фильтрации.</param>
        /// <returns>Структура с параметрами фильтрации.</returns>
        public static FilterParams InitializeFilterParams(string? addressStart, string? addressMask, string? timeStart, string? timeEnd)
        {
            var filterParams = new FilterParams();

            if (!string.IsNullOrEmpty(addressStart))
            {
                if (IPAddress.TryParse(addressStart, out var startAddress))
                {
                    filterParams.StartAddress = startAddress;
                }
                else
                {
                    throw new ArgumentException("Некорректный формат начального IP-адреса.", nameof(addressStart));
                }
            }

            if (!string.IsNullOrEmpty(addressMask))
            {
                if (int.TryParse(addressMask, out int maskLength) && maskLength > 0 && maskLength <= 32)
                {
                    filterParams.Mask = ~(0xFFFFFFFF >> maskLength);
                    filterParams.MaskLength = maskLength;
                }
                else
                {
                    throw new ArgumentException("Некорректная длина маски подсети.", nameof(addressMask));
                }
            }



            if (!string.IsNullOrEmpty(timeStart))
            {
                if (DateTime.TryParseExact(timeStart, new[] { "dd.MM.yyyy", "dd.MM.yyyy HH:mm:ss" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out var startDate))
                {
                    filterParams.StartDate = startDate;
                }
                else
                {
                    throw new ArgumentException("Некорректный формат начального времени.", nameof(timeStart));
                }
            }

            if (!string.IsNullOrEmpty(timeEnd))
            {
                if (DateTime.TryParseExact(timeEnd, new[] { "dd.MM.yyyy", "dd.MM.yyyy HH:mm:ss" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out var endDate))
                {
                    filterParams.EndDate = endDate;
                }
                else
                {
                    throw new ArgumentException("Некорректный формат конечного времени.", nameof(timeEnd));
                }
            }

            return filterParams;
        }

        public struct FilterParams
        {
            public IPAddress? StartAddress;
            public uint? Mask;
            public int? MaskLength;
            public DateTime? StartDate;
            public DateTime? EndDate;
        }


        /// <summary>
        /// Обрабатывает одну строку лога, проверяя соответствие параметрам фильтрации и учитывая количество IP-адресов.
        /// </summary>
        /// <param name="line">Строка лога для обработки.</param>
        /// <param name="ipCounts">Словарь для подсчета количества вхождений IP-адресов.</param>
        /// <param name="filterParams">Параметры фильтрации.</param>
        public static void ProcessLogLine(string line, Dictionary<string, int> ipCounts, FilterParams filterParams)
        {
            string pattern = @"(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})[\s:]+(\d{2}\.\d{2}\.\d{4} \d{2}:\d{2}:\d{2})";
            var match = Regex.Match(line, pattern);
            if (match.Success)
            {
                string ip = match.Groups[1].Value;
                string dateString = match.Groups[2].Value;
                DateTime timestamp;

                if (DateTime.TryParseExact(dateString, new[] { "dd.MM.yyyy", "dd.MM.yyyy HH:mm:ss" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out timestamp))
                {
                    if ((filterParams.StartDate.HasValue && timestamp < filterParams.StartDate.Value) ||
                        (filterParams.EndDate.HasValue && timestamp > filterParams.EndDate.Value))
                    {
                        return; // Пропускаем строку, если она не соответствует временным параметрам
                    }
                    if (filterParams.StartAddress != null && filterParams.Mask.HasValue)
                    {
                        if (filterParams.MaskLength <= 0 || filterParams.MaskLength > 32)
                        {
                            throw new ArgumentException("Некорректная длина маски подсети.", nameof(filterParams.Mask));
                        }
                        uint ipNumeric = IpToUint(IPAddress.Parse(ip));
                        uint startIpNumeric = IpToUint(filterParams.StartAddress);
                        if ((ipNumeric & filterParams.Mask.Value) != (startIpNumeric & filterParams.Mask.Value))
                        {
                            return; // Пропускаем строку, если IP не соответствует маске подсети
                        }
                    }

                    if (!ipCounts.ContainsKey(ip))
                    {
                        ipCounts[ip] = 0;
                    }
                    ipCounts[ip]++;
                }
                else
                {
                    Console.WriteLine($"Неверный формат даты в логе: {dateString}");
                }
            }
            else
            {
                Console.WriteLine($"Строка лога не соответствует шаблону: {line}");
            }
        }

        /// <summary>
        /// Преобразует IP-адрес в числовое представление типа uint.
        /// </summary>
        /// <param name="ipAddress">IP-адрес для преобразования.</param>
        /// <returns>Числовое представление IP-адреса в виде беззнакового целого числа.</returns>
        static uint IpToUint(IPAddress ipAddress)
        {
            byte[] bytes = ipAddress.GetAddressBytes();
            Array.Reverse(bytes); // Convert to little-endian
            return BitConverter.ToUInt32(bytes, 0);
        }

        /// <summary>
        /// Записывает количество IP-адресов в указанный выходной файл.
        /// </summary>
        /// <param name="outputFilePath">Путь к выходному файлу.</param>
        /// <param name="ipCounts">Словарь, содержащий количество IP-адресов.</param>
        static void WriteResults(string outputFilePath, Dictionary<string, int> ipCounts)
        {
            try
            {
                Console.WriteLine($"Запись в выходной файл: {outputFilePath}");
                using (var writer = new StreamWriter(outputFilePath))
                {
                    foreach (var entry in ipCounts)
                    {
                        writer.WriteLine($"{entry.Key}: {entry.Value}");
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"An error occurred while writing the output file: {e.Message}");
            }
        }
    }
}