using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using Spectre.Console;

namespace RenewableEnergyBillingSystem
{
    public abstract class EnergySource
    {
        public abstract double CostPerKWh { get; }
        public abstract double CarbonEmissionsPerKWh { get; }

        public double CalculateCost(double consumption) => consumption * CostPerKWh;
        public double CalculateCarbonEmissions(double consumption) => consumption * CarbonEmissionsPerKWh;
        public abstract string GetInfo();
    }

    public class SolarEnergy : EnergySource
    {
        public override double CostPerKWh => 0.07;
        public override double CarbonEmissionsPerKWh => 0.005;
        public override string GetInfo() => "Solar Energy \nCost per kWh = P0.07 \nCarbon emissions = 0.005 kg/kWh";
    }

    public class WindEnergy : EnergySource
    {
        public override double CostPerKWh => 0.05;
        public override double CarbonEmissionsPerKWh => 0.002;
        public override string GetInfo() => "Wind Energy \nCost per kWh = P0.05 \nCarbon emissions = 0.002 kg/kWh";
    }

    public class GeothermalEnergy : EnergySource
    {
        public override double CostPerKWh => 0.06;
        public override double CarbonEmissionsPerKWh => 0.003;
        public override string GetInfo() => "Geothermal Energy \nCost per kWh = P0.06 \nCarbon emissions = 0.003 kg/kWh";
    }

    public class User
    {
        private const string UserDataFilePath = @"C:\Users\Asus\source\repos\ProjectDonque\ProjectDonque\File\Data.txt"; // Update this path as needed

        public string FullName { get; set; }
        public string AccountType { get; set; }
        public EnergySource EnergySource { get; set; }
        public Dictionary<string, double> Devices { get; set; }
        public Dictionary<string, double> DevicePrices { get; set; }
        public Dictionary<string, double> DailyUsage { get; set; }
        public Dictionary<string, double> WeeklyUsage { get; set; }
        public Dictionary<string, double> MonthlyUsage { get; set; }
        public int UsageUpdateCount { get; set; }

        // Constructor
        public User(string fullName, string accountType, EnergySource energySource)
        {
            FullName = fullName;
            AccountType = accountType;
            EnergySource = energySource;
            Devices = new Dictionary<string, double>();
            DevicePrices = new Dictionary<string, double>();
            DailyUsage = new Dictionary<string, double>();
            WeeklyUsage = new Dictionary<string, double>();
            MonthlyUsage = new Dictionary<string, double>();
            UsageUpdateCount = 0; // Initialize count
        }

        // Update usage logs based on devices used
        public void UpdateUsageLogs()
        {
            double totalDailyConsumption = Devices.Values.Sum();
            string today = DateTime.Now.ToString("dddd");
            DailyUsage[today] = totalDailyConsumption;

            // Update weekly usage
            string currentWeek = $"Week {CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(DateTime.Now, CalendarWeekRule.FirstDay, DayOfWeek.Monday)}";
            if (WeeklyUsage.ContainsKey(currentWeek))
            {
                WeeklyUsage[currentWeek] += totalDailyConsumption;
            }
            else
            {
                WeeklyUsage[currentWeek] = totalDailyConsumption;
            }

            // Update monthly usage
            string currentMonth = DateTime.Now.ToString("MMMM");
            if (MonthlyUsage.ContainsKey(currentMonth))
            {
                MonthlyUsage[currentMonth] += totalDailyConsumption;
            }
            else
            {
                MonthlyUsage[currentMonth] = totalDailyConsumption;
            }

            UsageUpdateCount++; // Increment the usage update count

            Console.WriteLine($"Usage logs updated: {totalDailyConsumption}kWh consumed today. \nTotal updates: {UsageUpdateCount}");
        }

        // Serialize user data to a string
        public string Serialize()
        {
            var devicesData = string.Join(";", Devices.Select(d => $"{d.Key}:{d.Value}"));
            var pricesData = string.Join(";", DevicePrices.Select(p => $"{p.Key}:{p.Value}"));
            return $"{FullName}|{AccountType}|{EnergySource.GetType().Name}|{devicesData}|{pricesData}|{UsageUpdateCount}";
        }

        // Deserialize user data from a string
        public static User Deserialize(string data)
        {
            var parts = data.Split('|');
            var fullName = parts[0];
            var accountType = parts[1];
            var energySourceType = parts[2];

            EnergySource energySource;

            if (energySourceType == nameof(SolarEnergy))
                energySource = new SolarEnergy();
            else if (energySourceType == nameof(WindEnergy))
                energySource = new WindEnergy();
            else if (energySourceType == nameof(GeothermalEnergy))
                energySource = new GeothermalEnergy();
            else
                throw new InvalidOperationException("Invalid energy source.");

            var devicesData = parts[3].Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            var devices = devicesData.ToDictionary(d => d.Split(':')[0], d => double.Parse(d.Split(':')[1]));

            var pricesData = parts[4].Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            var devicePrices = pricesData.ToDictionary(p => p.Split(':')[0], p => double.Parse(p.Split(':')[1]));

            var usageUpdateCount = int.Parse(parts[5]); // Deserialize usage update count

            return new User(fullName, accountType, energySource)
            { Devices = devices, DevicePrices = devicePrices, UsageUpdateCount = usageUpdateCount };
        }
    }

    class Program
    {
        private static List<User> users = new List<User>();

        static void Main(string[] args)
        {
            LoadData(); // Load users from file at startup

            while (true)
            {
                Console.Clear();
                Console.WriteLine("\n--- Main Menu ---");
                Console.WriteLine("1. Admin Menu");
                Console.WriteLine("2. User Menu");
                Console.WriteLine("3. Exit");
                Console.Write("Enter your Choice: ");

                string choice = Console.ReadLine();

                if (choice == "3")
                {
                    Console.WriteLine("Saving Data and Exiting...");
                    Thread.Sleep(1000); // Simulate processing delay
                    SaveData(); // Save users to file before exiting
                    break;
                }

                switch (choice)
                {
                    case "1":
                        AdminMenu();
                        break;
                    case "2":
                        UserMenu();
                        break;
                    default:
                        Console.WriteLine("Invalid choice. Please try again.");
                        Thread.Sleep(1000);
                        break;
                }
            }
        }
        static bool ValidateAdminPassword()
        {
            const string adminPassword = "ADMIN6408"; // Replace with a secure password
            bool isValid = false;

            Thread validationThread = new Thread(() =>
            {
                Console.Write("Enter admin password: ");
                string enteredPassword = Console.ReadLine();
                isValid = enteredPassword == adminPassword;
            });

            validationThread.Start();
            validationThread.Join(); // Wait for the thread to complete
            return isValid;
        }

        static void AdminMenu()
        {
            Thread adminThread = new Thread(() =>
            {
                if (!ValidateAdminPassword())
                {
                    Console.WriteLine("Invalid Password. Access denied.");
                    Thread.Sleep(1000); // Simulate a delay for security
                    return;
                }
                Console.Clear();
                while (true)
                {
                    Console.WriteLine("\n--- Admin Menu ---");
                    Console.WriteLine("1. Create New Consumer");
                    Console.WriteLine("2. Add Device to Consumer");
                    Console.WriteLine("3. View Devices of Consumer");
                    Console.WriteLine("4. Remove Device from Consumer");
                    Console.WriteLine("5. View All Consumers");
                    Console.WriteLine("6. Update and View Usage Logs");
                    Console.WriteLine("7. Search for Consumer and Update Devices");
                    Console.WriteLine("8. Back to Main Menu");

                    Console.Write("Enter your Choice: ");
                    string choice = Console.ReadLine();

                    if (choice == "8") break;

                    switch (choice)
                    {
                        case "1":
                            Console.Clear();
                            CreateNewUser();
                            break;
                        case "2":
                            Console.Clear();
                            AddDeviceToUser();
                            break;
                        case "3":
                            Console.Clear();
                            ViewDevicesOfUser();
                            break;
                        case "4":
                            Console.Clear();
                            RemoveDeviceFromUser();
                            break;
                        case "5":
                            Console.Clear();
                            ViewAllUsers();
                            break;
                        case "6":
                            Console.Clear();
                            UpdateAndViewUsageLogs();
                            break;
                        case "7":
                            Console.Clear();
                            SearchAndModifyUser();
                            break;
                        case "8": // Back to Main Menu
                            Thread mainMenuThread = new Thread(() =>
                            {
                                Console.WriteLine("Returning to Main Menu...");
                                Thread.Sleep(1000);
                            });
                            mainMenuThread.Start();
                            mainMenuThread.Join();
                            break;
                        default:
                            Console.WriteLine("Invalid choice. Please try again.");
                            Thread.Sleep(1000);
                            break;
                    }
                }
            });

            adminThread.Start();
            adminThread.Join(); // Ensure the thread completes before returning
        }


        static void UpdateAndViewUsageLogs()
        {
            Console.Write("\nEnter the Full Name of the Consumer: ");
            string fullName = Console.ReadLine();
            User currentUser = users.FirstOrDefault(user => user.FullName.Equals(fullName, StringComparison.OrdinalIgnoreCase));

            if (currentUser == null)
            {
                Console.WriteLine("Consumer not found.");
                return;
            }

            currentUser.UpdateUsageLogs(); // Update usage logs for the user
            DisplayUsageLogs(currentUser); // Display usage logs for the user
        }

        static void DisplayUsageLogs(User user)
        {
            Console.WriteLine("\n--- Usage Logs ---");

            // Daily Usage
            Console.WriteLine("\nDaily Usage:");
            foreach (var log in user.DailyUsage)
            {
                Console.WriteLine($"{log.Key}: {log.Value} kWh");
            }
            DisplayGraph("Daily Usage", user.DailyUsage);

            // Weekly Usage
            Console.WriteLine("\nWeekly Usage:");
            foreach (var log in user.WeeklyUsage)
            {
                Console.WriteLine($"{log.Key}: {log.Value} kWh");
            }
            DisplayGraph("Weekly Usage", user.WeeklyUsage);

            // Monthly Usage
            Console.WriteLine("\nMonthly Usage:");
            foreach (var log in user.MonthlyUsage)
            {
                Console.WriteLine($"{log.Key}: {log.Value} kWh");
            }
            DisplayGraph("Monthly Usage", user.MonthlyUsage);

            // Display frequency of updates
            Console.WriteLine($"\nTotal Usage Updates: {user.UsageUpdateCount}");
        }


        static void UserMenu()
        {
            Thread userThread = new Thread(() =>
            {
                Console.Clear();
                Console.Write("\nEnter your Full Name: ");
                string fullName = Console.ReadLine();

                User currentUser = users.FirstOrDefault(user => user.FullName.Equals(fullName, StringComparison.OrdinalIgnoreCase));

                if (currentUser == null)
                {
                    Console.WriteLine("Consumer not Found.");
                    Thread.Sleep(1500);
                    return;
                }

                while (true)
                {
                    Console.WriteLine("\n--- Customer Menu ---");
                    Console.WriteLine("1. View Account Information");
                    Console.WriteLine("2. Back to Main Menu");

                    Console.Write("Enter your Choice: ");
                    string choice = Console.ReadLine();

                    if (choice == "2") break;

                    switch (choice)
                    {
                        case "1":
                            ViewAccountInformation(currentUser);
                            Thread.Sleep(2000);
                            break;
                        case "2": // Back to Main Menu
                            Thread mainMenuThread = new Thread(() =>
                            {
                                Console.WriteLine("Returning to Main Menu...");
                                Thread.Sleep(1000);
                            });
                            mainMenuThread.Start();
                            mainMenuThread.Join();
                            break;
                        default:
                            Console.WriteLine("Invalid choice. Please try again.");
                            Thread.Sleep(1000);
                            break;
                    }
                }
            });

            userThread.Start();
            userThread.Join(); // Wait for the thread to complete
        }
        private static void ViewAccountInformation(User user)
        {
            Console.WriteLine($"\n--- Information Details for {user.FullName} ---");
            Console.WriteLine($"Account Type: {user.AccountType}");
            Console.WriteLine($"Energy Source: {user.EnergySource.GetInfo()}");
            Console.WriteLine($"\nUsage Updates: {user.UsageUpdateCount} times"); // Display frequency

            if (user.Devices.Count == 0)
            {
                Console.WriteLine("\nNo devices have been added for this account.");
                return;
            }

            // Display devices
            var table = new Table();
            table.AddColumn("Device Name");
            table.AddColumn("Power Consumption (kWh)");
            table.AddColumn("Price (P)");

            foreach (var device in user.Devices)
            {
                var price = user.DevicePrices.TryGetValue(device.Key, out var devicePrice) ? devicePrice : 0;
                table.AddRow(device.Key, device.Value.ToString("F2"), price.ToString("F2"));
            }
            AnsiConsole.Write(table);

            // Calculate total usage across all time periods
            double totalDailyUsage = user.DailyUsage.Values.Sum();
            double totalWeeklyUsage = user.WeeklyUsage.Values.Sum();
            double totalMonthlyUsage = user.MonthlyUsage.Values.Sum();

            // Calculate costs and carbon emissions
            double totalCost = user.EnergySource.CalculateCost(totalMonthlyUsage);
            double totalCarbonEmissions = user.EnergySource.CalculateCarbonEmissions(totalMonthlyUsage);

            ViewUsageDetails(user);

            Console.WriteLine($"\nTotal Carbon Emissions for the Month: {totalCarbonEmissions:F2} kg");
        }

        private static void ViewUsageDetails(User user)
        {
            DisplayBillDetails(user);
        }

        private static void DisplayBillDetails(User user)
        {
            // Daily Usage and Bill
            CalculateAndDisplayBill(user.DailyUsage, "Daily", user);
            DisplayGraph("Daily Usage", user.DailyUsage);

            // Weekly Usage and Bill
            CalculateAndDisplayBill(user.WeeklyUsage, "Weekly", user);
            DisplayGraph("Weekly Usage", user.WeeklyUsage);

            // Monthly Usage and Bill
            CalculateAndDisplayBill(user.MonthlyUsage, "Monthly", user);
            DisplayGraph("Monthly Usage", user.MonthlyUsage);
        }

        private static void CalculateAndDisplayBill(Dictionary<string, double> usageData, string period, User user)
        {
            double totalBill = 0;

            Console.WriteLine($"\n--- {period} Usage and Bill ---");

            foreach (var usage in usageData)
            {
                double billAmount = user.EnergySource.CalculateCost(usage.Value);
                totalBill += billAmount;

                // Display each period's usage and corresponding bill amount.
                Console.WriteLine($"{usage.Key}: {usage.Value} kWh, Bill: P{billAmount:F2}");
            }

            // Display overall total bill for the specified period.
            Console.WriteLine($"Overall Total Bill for {period}: P{totalBill:F2}");

        }
        static void DisplayGraph(string title, Dictionary<string, double> usageData)
        {
            var chart = new BarChart()
                .Width(60)
                .Label($"[bold blue]{title}[/]")
                .CenterLabel();

            foreach (var log in usageData)
            {
                chart.AddItem(log.Key, (float)log.Value, Spectre.Console.Color.Green);
            }

            AnsiConsole.Write(chart);
        }

        private static void CreateNewUser()
        {
            Console.Write("Enter Full Name: ");
            string fullName = Console.ReadLine();
            Console.Clear();
            string[] choices = { "Residential", "Commercial" };
            int index = 0;
            ConsoleKeyInfo key;
            string accountType = String.Empty;
            while (true)
            {
                Console.WriteLine("Enter Account Type (Residential or Commercial): ");
                for (int i = 0; i < choices.Length; i++)
                {
                    if (index == i)
                    {
                        Console.Write("-> ");
                        Console.ForegroundColor = ConsoleColor.Green;
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                    }
                    Console.WriteLine(choices[i]);
                    Console.ResetColor();
                }
                key = Console.ReadKey(true);
                Console.Clear();
                if (key.KeyChar == 's' || key.Key == ConsoleKey.DownArrow)
                {
                    index = (index + 1) % choices.Length;
                }
                else if (key.KeyChar == 'w' || key.Key == ConsoleKey.UpArrow)
                {
                    index = (index - 1 + choices.Length) % choices.Length;
                }
                else if (key.Key == ConsoleKey.Enter)
                {
                    if (index == 0)
                    {
                        accountType = "residential";
                        accountType = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(accountType.ToLower());
                        break;
                    }
                    else if (index == 1)
                    {
                        accountType = "commercial";
                        accountType = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(accountType.ToLower());
                        break;
                    }
                }
                else
                {
                    Console.Clear();
                }
            }

            EnergySource energySource;

            while (true)
            {
                try
                {
                    Console.WriteLine("\nChoose energy source:");
                    Console.WriteLine("1. Solar");
                    Console.WriteLine("2. Wind");
                    Console.WriteLine("3. Geothermal");

                    string energyChoice = Console.ReadLine();

                    if (energyChoice == "1")
                        energySource = new SolarEnergy();
                    else if (energyChoice == "2")
                        energySource = new WindEnergy();
                    else if (energyChoice == "3")
                        energySource = new GeothermalEnergy();
                    else
                        throw new InvalidOperationException("Invalid energy source.");

                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            User newUser = new User(fullName, accountType, energySource);
            users.Add(newUser);

            // Save data after completing user creation.
            SaveData();
            Console.WriteLine("Consumer created Successfully.");
        }

        private static void AddDeviceToUser()
        {
            Console.Write("\nEnter the Full Name of the Consumer: ");
            var fullName = Console.ReadLine();

            User currentUser = users.FirstOrDefault(user => user.FullName.Equals(fullName, StringComparison.OrdinalIgnoreCase));

            if (currentUser == null)
            {
                Console.WriteLine("Consumer not Found.");
                return;
            }

            bool addAnotherDevice = true;

            while (addAnotherDevice)
            {
                Console.Write("Enter Device Name: ");
                var deviceName = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(deviceName))
                {
                    Console.WriteLine("Device name cannot be empty. Please try again.");
                    continue;
                }

                double powerConsumption;

                while (true)
                {
                    try
                    {
                        Console.Write("Enter Device Power Consumption(kWh): ");
                        if (!double.TryParse(Console.ReadLine(), out powerConsumption))
                            throw new FormatException();

                        break;
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("Invalid input for power consumption. Please enter a number.");
                    }
                }

                double devicePrice;

                while (true)
                {
                    try
                    {
                        Console.Write("Enter Price for this Device: ");
                        if (!double.TryParse(Console.ReadLine(), out devicePrice))
                            throw new FormatException();

                        break;
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("Invalid input for device price. Please enter a number.");
                    }
                }

                currentUser.Devices[deviceName] = powerConsumption;
                currentUser.DevicePrices[deviceName] = devicePrice;

                Console.WriteLine("Device added Successfully.");

                Console.Write("Do you want to add another device? (y/n): ");
                var response = Console.ReadLine().ToLower();
                addAnotherDevice = response == "y";
            }
            // Save data after all devices have been added.
            SaveData();
            Console.WriteLine("Data saved Successfully.");
        }

        static void ViewDevicesOfUser()
        {
            Console.Write("\nEnter the Full Name of the Consumer: ");
            string fullName = Console.ReadLine();

            User currentUser = users.FirstOrDefault(user => user.FullName.Equals(fullName, StringComparison.OrdinalIgnoreCase));

            if (currentUser == null)
            {
                Console.WriteLine("Consumer not Found.");
                return;
            }

            if (currentUser.Devices.Count == 0)
            {
                Console.WriteLine($"\nConsumer {currentUser.FullName} has no Devices added.");
                return;
            }

            var table = new Table();
            table.AddColumn("Device Name");
            table.AddColumn("Power Consumption (kWh)");
            table.AddColumn("Price (P)");

            foreach (var device in currentUser.Devices)
            {
                var price = currentUser.DevicePrices.TryGetValue(device.Key, out var devicePrice) ? devicePrice : 0;
                table.AddRow(device.Key, device.Value.ToString("F2"), price.ToString("F2"));
            }

            AnsiConsole.Write(table);
        }

        static void RemoveDeviceFromUser()
        {
            Console.Write("\nEnter the Full Name of the Consumer: ");
            var fullname = Console.ReadLine();

            var currentuser = users.FirstOrDefault(user => user.FullName.Equals(fullname, StringComparison.OrdinalIgnoreCase));

            if (currentuser == null)
            {
                Console.WriteLine("Consumer not Found.");
                return;
            }

            Console.Write("\nEnter the Device Name to Remove: ");
            var devicename = Console.ReadLine();

            if (currentuser.Devices.ContainsKey(devicename))
            {
                currentuser.Devices.Remove(devicename);
                currentuser.DevicePrices.Remove(devicename);
                Console.WriteLine("Device removed Successfully.");

                SaveData();  // Save after removing a device.
            }
            else
            {
                Console.WriteLine("Device not Found.");
            }
        }

        static void ViewAllUsers()
        {
            var table = new Table();
            table.AddColumn("Full Name");
            table.AddColumn("Account Type (Residential or Commercial)");

            foreach (var user in users)
            {
                table.AddRow(user.FullName, user.AccountType);
            }

            AnsiConsole.Write(table);
        }

        private static void SearchAndModifyUser()
        {
            Console.Write("\nEnter the Full Name of the Consumer: ");
            string fullName = Console.ReadLine();

            User currentUser = users.FirstOrDefault(user => user.FullName.Equals(fullName, StringComparison.OrdinalIgnoreCase));

            if (currentUser == null)
            {
                Console.WriteLine("Consumer not found.");
                return;
            }

            Console.WriteLine($"\n--- Consumer Found: {currentUser.FullName} ---");
            Console.WriteLine($"Account Type: {currentUser.AccountType}");
            Console.WriteLine($"Energy Source: {currentUser.EnergySource.GetInfo()}");
            Console.WriteLine("\nDevices:");

            var table = new Table();
            table.AddColumn("Device Name");
            table.AddColumn("Power Consumption (kWh)");
            table.AddColumn("Price (P)");

            foreach (var device in currentUser.Devices)
            {
                var price = currentUser.DevicePrices.TryGetValue(device.Key, out var devicePrice) ? devicePrice : 0;
                table.AddRow(device.Key, device.Value.ToString("F2"), price.ToString("F2"));
            }
            AnsiConsole.Write(table);

            bool continueModifications = true;

            while (continueModifications)
            {
                Console.WriteLine("\n--- Update Devices ---");
                Console.WriteLine("1. Change Device Power Consumption");
                Console.WriteLine("2. Change Device Price");
                Console.WriteLine("3. Back to Admin Menu");
                Console.Write("Enter your choice: ");
                string choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        ChangeDeviceConsumption(currentUser);
                        break;
                    case "2":
                        ChangeDevicePrice(currentUser);
                        break;
                    case "3":
                        continueModifications = false;
                        break;
                    default:
                        Console.WriteLine("Invalid choice. Please try again.");
                        break;
                }
            }
        }

        private static void ChangeDeviceConsumption(User user)
        {
            Console.Write("\nEnter the Name of the Device to Update: ");
            string deviceName = Console.ReadLine();

            if (user.Devices.ContainsKey(deviceName))
            {
                double newConsumption;
                while (true)
                {
                    Console.Write("Enter the New Power Consumption(kWh): ");
                    if (double.TryParse(Console.ReadLine(), out newConsumption) && newConsumption >= 0)
                    {
                        user.Devices[deviceName] = newConsumption;
                        Console.WriteLine($"Power Consumption for '{deviceName}' updated to {newConsumption} kWh.");
                        SaveData(); // Save changes
                        break;
                    }
                    else
                    {
                        Console.WriteLine("Invalid input. Please enter a valid number.");
                    }
                }
            }
            else
            {
                Console.WriteLine("Device not Found.");
            }
        }
        private static void ChangeDevicePrice(User user)
        {
            Console.Write("\nEnter the Name of the Device to Update: ");
            string deviceName = Console.ReadLine();

            if (user.DevicePrices.ContainsKey(deviceName))
            {
                double newPrice;
                while (true)
                {
                    Console.Write("Enter the New Price(P): ");
                    if (double.TryParse(Console.ReadLine(), out newPrice) && newPrice >= 0)
                    {
                        user.DevicePrices[deviceName] = newPrice;
                        Console.WriteLine($"Price for '{deviceName}' updated to P{newPrice:F2}.");
                        SaveData(); // Save changes
                        break;
                    }
                    else
                    {
                        Console.WriteLine("Invalid input. Please enter a valid number.");
                    }
                }
            }
            else
            {
                Console.WriteLine("Device not Found.");
            }
        }

        private static void LoadData()
        {
            users.Clear(); // Clear existing data

            string userDataDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Users");

            if (!Directory.Exists(userDataDirectory))
            {
                Console.WriteLine("No Consumer data directory found. Starting with empty data.");
                return;
            }

            foreach (var folderPath in Directory.GetDirectories(userDataDirectory))
            {
                string userFile = Path.Combine(folderPath, "C:\\Users\\Asus\\source\\repos\\ProjectDonque\\ProjectDonque\\File\\Data.txt");

                if (File.Exists(userFile))
                {
                    try
                    {
                        using (StreamReader reader = new StreamReader(userFile))
                        {
                            string fullName = reader.ReadLine()?.Split(":")[1]?.Trim();
                            string accountType = reader.ReadLine()?.Split(":")[1]?.Trim();
                            string energySourceType = reader.ReadLine()?.Split(":")[1]?.Trim();

                            EnergySource energySource = energySourceType switch
                            {
                                nameof(SolarEnergy) => new SolarEnergy(),
                                nameof(WindEnergy) => new WindEnergy(),
                                nameof(GeothermalEnergy) => new GeothermalEnergy(),
                                _ => throw new InvalidOperationException("Invalid energy source.")
                            };

                            User user = new User(fullName, accountType, energySource);

                            while (!reader.EndOfStream)
                            {
                                var line = reader.ReadLine();

                                if (line.StartsWith("Device: "))
                                {
                                    string deviceName = line.Split(":")[1]?.Trim();
                                    double consumption = double.Parse(reader.ReadLine()?.Split(":")[1]?.Trim() ?? "0");
                                    double price = double.Parse(reader.ReadLine()?.Split(":")[1]?.Trim() ?? "0");

                                    user.Devices[deviceName] = consumption;
                                    user.DevicePrices[deviceName] = price;
                                }
                                else if (line.StartsWith("Daily Usage: "))
                                {
                                    foreach (var part in line.Split(":")[1]?.Trim().Split(","))
                                    {
                                        var keyValue = part.Split("=");
                                        user.DailyUsage[keyValue[0]] = double.Parse(keyValue[1]);
                                    }
                                }
                                else if (line.StartsWith("Weekly Usage: "))
                                {
                                    foreach (var part in line.Split(":")[1]?.Trim().Split(","))
                                    {
                                        var keyValue = part.Split("=");
                                        user.WeeklyUsage[keyValue[0]] = double.Parse(keyValue[1]);
                                    }
                                }
                                else if (line.StartsWith("Monthly Usage: "))
                                {
                                    foreach (var part in line.Split(":")[1]?.Trim().Split(","))
                                    {
                                        var keyValue = part.Split("=");
                                        user.MonthlyUsage[keyValue[0]] = double.Parse(keyValue[1]);
                                    }
                                }
                                else if (line.StartsWith("Usage Update Count: "))
                                {
                                    user.UsageUpdateCount = int.Parse(line.Split(":")[1]?.Trim());
                                }
                            }

                            users.Add(user);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error loading Data for File {userFile}: {ex.Message}");
                    }
                }
            }

            Console.WriteLine($"{users.Count} Consumer Loaded Successfully.");
        }


        private static void SaveData()
        {
            string userDataDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Users");

            if (!Directory.Exists(userDataDirectory))
            {
                Directory.CreateDirectory(userDataDirectory);
            }

            foreach (var user in users)
            {
                string userFolder = Path.Combine(userDataDirectory, user.FullName.Replace(" ", "_"));
                Directory.CreateDirectory(userFolder);

                string userFile = Path.Combine(userFolder, "C:\\Users\\Asus\\source\\repos\\ProjectDonque\\ProjectDonque\\File\\Data.txt");

                try
                {
                    using (StreamWriter writer = new StreamWriter(userFile, false))
                    {
                        writer.WriteLine($"Full Name: {user.FullName}");
                        writer.WriteLine($"Account Type: {user.AccountType}");
                        writer.WriteLine($"Energy Source: {user.EnergySource.GetType().Name}");
                        foreach (var device in user.Devices)
                        {
                            writer.WriteLine($"Device: {device.Key}");
                            writer.WriteLine($"Consumption: {device.Value}");
                            writer.WriteLine($"Price: {user.DevicePrices[device.Key]}");
                        }
                        writer.WriteLine($"Daily Usage: {string.Join(",", user.DailyUsage.Select(d => $"{d.Key} = {d.Value}"))}");
                        writer.WriteLine($"Weekly Usage: {string.Join(",", user.WeeklyUsage.Select(w => $"{w.Key} = {w.Value}"))}");
                        writer.WriteLine($"Monthly Usage: {string.Join(",", user.MonthlyUsage.Select(m => $"{m.Key} = {m.Value}"))}");
                        writer.WriteLine($"Usage Update Count: {user.UsageUpdateCount}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error saving Data for {user.FullName}: {ex.Message}");
                }
            }
            Console.WriteLine("All Data saved Successfully."); // Log message moved outside the loop.
        }
    }
}