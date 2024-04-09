using System.Net;


namespace IPJournal.Test
{
    public class LogProcessorTests
    {
        [Fact]
        /// <summary>
        /// ��������� ��������� ���������� ������ ����. ���������, ��� ��� ��������� ������ � �������� IP-�������
        /// ������� ������������� �� ������� ��� ����� IP � ������� ipCounts.
        /// </summary>
        public void ProcessLogLine_ValidLine_IncrementsCount()
        {
            // Arrange
            var ipCounts = new Dictionary<string, int>();
            var filterParams = new AddressReader.FilterParams
            {
                StartAddress = IPAddress.Parse("192.168.1.1"),
                Mask = 0xFFFFFF00, // 255.255.255.0
                StartDate = new DateTime(2020, 1, 1),
                EndDate = new DateTime(2020, 12, 31)
            };
            string logLine = "192.168.1.1: 01.01.2020 12:34:56 Some log message";

            // Act
            AddressReader.ProcessLogLine(logLine, ipCounts, filterParams);

            // Assert
            Assert.True(ipCounts.ContainsKey("192.168.1.1"));
            Assert.Equal(1, ipCounts["192.168.1.1"]);
        }

        [Fact]
        /// <summary>
        /// ���� ���������, ��� ������ ���� � ����������� IP-�������� �� �������� � ���������� ��������.
        /// </summary>
        public void ProcessLogLine_InvalidIP_DoesNotIncrementCount()
        {
            // Arrange
            var ipCounts = new Dictionary<string, int>();
            var filterParams = new IPJournal.AddressReader.FilterParams
            {
                // ������������, ��� ������ �������� �� ������������ �������� IP-�������
            };
            string logLine = "invalid_ip: 01.01.2020 12:34:56 Some log message";

            // Act
            AddressReader.ProcessLogLine(logLine, ipCounts, filterParams);

            // Assert
            Assert.DoesNotContain("invalid_ip", ipCounts.Keys);
        }

        [Fact]
        /// <summary>
        /// ���� ���������, ��� ������ ���� � ����������� ������� ������� �� �������� � ���������� ��������.
        /// </summary>
        public void ProcessLogLine_InvalidMask_ThrowsArgumentException()
        {
            // Arrange
            var ipCounts = new Dictionary<string, int>();
            var filterParams = new IPJournal.AddressReader.FilterParams
            {
                StartAddress = IPAddress.Parse("192.168.1.1"),
                Mask = 0, // ���������� �������� ��� ����� �������
                MaskLength = 0
            };
            string logLine = "192.168.1.1: 01.01.2020 12:34:56 Some log message";

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => AddressReader.ProcessLogLine(logLine, ipCounts, filterParams));
            Assert.Contains("������������ ����� ����� �������", exception.Message);
        }

        [Fact]
        /// <summary>
        /// ���� ���������, ��� ������ ����, �� ���������� � ������������� ��������� ���������, �� �������� � ���������� ��������.
        /// </summary>
        public void ProcessLogLine_InvalidTimeIntervals_DoesNotIncrementCount()
        {
            // Arrange
            var ipCounts = new Dictionary<string, int>();
            var filterParams = new IPJournal.AddressReader.FilterParams
            {
                StartDate = new DateTime(2020, 1, 2), // ��������� ���������� ��������� ���, ��� ��� �� �������� � ����
                EndDate = new DateTime(2020, 1, 3)
            };
            string logLine = "192.168.1.1: 01.01.2020 12:34:56 Some log message";

            // Act
            AddressReader.ProcessLogLine(logLine, ipCounts, filterParams);

            // Assert
            Assert.DoesNotContain("192.168.1.1", ipCounts.Keys);
        }
        [Fact]
        /// <summary>
        /// ���� ���������, ��� ����, ����� ������� ����� ��������� � ������� � ������
        /// �������������� ���������� ���������, ��������� �������������� � ����������� � ��������.
        /// </summary>
        public void ProcessLogLine_TimeOnIntervalEdge_IncrementsCount()
        {
            // Arrange
            var ipCounts = new Dictionary<string, int>();
            var filterParams = new IPJournal.AddressReader.FilterParams
            {
                StartAddress = IPAddress.Parse("192.168.1.1"),
                Mask = 0xFFFFFF00, // 255.255.255.0
                MaskLength = 24,
                StartDate = new DateTime(2020, 1, 1),
                EndDate = new DateTime(2020, 1, 1, 23, 59, 59)
            };
            string logLineAtStart = "192.168.1.1: 01.01.2020 00:00:00 Some log message";
            string logLineAtEnd = "192.168.1.1: 01.01.2020 23:59:59 Some log message";

            // Act
            AddressReader.ProcessLogLine(logLineAtStart, ipCounts, filterParams);
            AddressReader.ProcessLogLine(logLineAtEnd, ipCounts, filterParams);

            // Assert
            Assert.Equal(2, ipCounts["192.168.1.1"]);
        }

        [Fact]
        /// <summary>
        /// ���� ���������, ��� ���� � IP-��������, ������������ �� �������� ��������� �������,
        /// ��������� �������������� � ����������� � ��������.
        /// </summary>
        public void ProcessLogLine_IPOnSubnetEdge_IncrementsCount()
        {
            // Arrange
            var ipCounts = new Dictionary<string, int>();
            var filterParams = new IPJournal.AddressReader.FilterParams
            {
                StartAddress = IPAddress.Parse("192.168.1.0"),
                Mask = 0xFFFFFF00, // ����� ����� ������� � ����� (255.255.255.0)
                MaskLength = 24,
                StartDate = new DateTime(2020, 1, 1),
                EndDate = new DateTime(2020, 12, 31)
            };
            string logLineAtStart = "192.168.1.0: 01.01.2020 12:34:56 Some log message";
            string logLineAtEnd = "192.168.1.255: 01.01.2020 12:34:56 Some log message";

            // Act
            AddressReader.ProcessLogLine(logLineAtStart, ipCounts, filterParams);
            AddressReader.ProcessLogLine(logLineAtEnd, ipCounts, filterParams);

            // Assert
            ipCounts.TryGetValue("192.168.1.0", out int countStart);
            ipCounts.TryGetValue("192.168.1.255", out int countEnd);
            Assert.Equal(1, countStart);
            Assert.Equal(1, countEnd);
        }
    }
}

