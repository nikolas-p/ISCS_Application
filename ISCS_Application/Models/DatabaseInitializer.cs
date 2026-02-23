using ISCS_Application.Models;
using Microsoft.EntityFrameworkCore;

public static class DatabaseInitializer
{

    public static void Initialize(OfficeDbContext context)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("=== НАЧАЛО ИНИЦИАЛИЗАЦИИ БД ===");

            // Проверяем подключение
            if (!context.Database.CanConnect())
            {
                System.Diagnostics.Debug.WriteLine("НЕТ ПОДКЛЮЧЕНИЯ К БД!");
                return;
            }
            System.Diagnostics.Debug.WriteLine("Подключение к БД успешно");

            // Создаем базу если её нет
            context.Database.EnsureCreated();

            // ОЧИЩАЕМ существующие данные через SQL (обходим EF)
            System.Diagnostics.Debug.WriteLine("Очистка существующих данных...");

            // Отключаем проверку внешних ключей временно
            context.Database.ExecuteSqlRaw("EXEC sp_MSforeachtable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL'");

            // Очищаем таблицы в правильном порядке
            context.Database.ExecuteSqlRaw("DELETE FROM Equipment");
            context.Database.ExecuteSqlRaw("DELETE FROM Place");
            context.Database.ExecuteSqlRaw("DELETE FROM Worker");
            context.Database.ExecuteSqlRaw("DELETE FROM Office");
            context.Database.ExecuteSqlRaw("DELETE FROM Position");

            // Сбрасываем счетчики идентификаторов
            context.Database.ExecuteSqlRaw("DBCC CHECKIDENT ('Equipment', RESEED, 0)");
            context.Database.ExecuteSqlRaw("DBCC CHECKIDENT ('Place', RESEED, 0)");
            context.Database.ExecuteSqlRaw("DBCC CHECKIDENT ('Worker', RESEED, 0)");
            context.Database.ExecuteSqlRaw("DBCC CHECKIDENT ('Office', RESEED, 0)");
            context.Database.ExecuteSqlRaw("DBCC CHECKIDENT ('Position', RESEED, 0)");

            // Включаем проверку внешних ключей обратно
            context.Database.ExecuteSqlRaw("EXEC sp_MSforeachtable 'ALTER TABLE ? WITH CHECK CHECK CONSTRAINT ALL'");

            System.Diagnostics.Debug.WriteLine("Данные очищены через SQL");

            // Заполняем новыми данными
            System.Diagnostics.Debug.WriteLine("Начинаем заполнение данными...");
            SeedData(context);

            // Финальная проверка
            var finalPositions = context.Positions.Count();
            var finalOffices = context.Offices.Count();
            var finalWorkers = context.Workers.Count();
            var finalPlaces = context.Places.Count();
            var finalEquipment = context.Equipment.Count();

            System.Diagnostics.Debug.WriteLine("=== ИТОГ ИНИЦИАЛИЗАЦИИ ===");
            System.Diagnostics.Debug.WriteLine($"Позиций: {finalPositions} (должно быть 7)");
            System.Diagnostics.Debug.WriteLine($"Офисов: {finalOffices} (должно быть 8)");
            System.Diagnostics.Debug.WriteLine($"Сотрудников: {finalWorkers} (должно быть 24)");
            System.Diagnostics.Debug.WriteLine($"Мест: {finalPlaces} (должно быть 13)");
            System.Diagnostics.Debug.WriteLine($"Оборудования: {finalEquipment} (должно быть 12)");
            System.Diagnostics.Debug.WriteLine("==========================");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"!!! ОШИБКА: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"!!! Inner: {ex.InnerException?.Message}");
            System.Diagnostics.Debug.WriteLine($"!!! StackTrace: {ex.StackTrace}");
            throw;
        }
    }
    private static void SeedData(OfficeDbContext context)
    {
        // 1. ПОЗИЦИИ
        System.Diagnostics.Debug.WriteLine("Добавление позиций...");
        SeedPositions(context);
        int saved = context.SaveChanges();
        System.Diagnostics.Debug.WriteLine($"Сохранено позиций: {saved}");

        // 2. ОФИСЫ (пока без GeneralWorkerId)
        System.Diagnostics.Debug.WriteLine("Добавление офисов...");
        SeedOffices(context);
        saved = context.SaveChanges();
        System.Diagnostics.Debug.WriteLine($"Сохранено офисов: {saved}");

        // 3. СОТРУДНИКИ
        System.Diagnostics.Debug.WriteLine("Добавление сотрудников...");
        SeedWorkers(context);
        saved = context.SaveChanges();
        System.Diagnostics.Debug.WriteLine($"Сохранено сотрудников: {saved}");

      

        // 5. МЕСТА
        System.Diagnostics.Debug.WriteLine("Добавление мест...");
        SeedPlaces(context);
        saved = context.SaveChanges();
        System.Diagnostics.Debug.WriteLine($"Сохранено мест: {saved}");

        // 6. ОБОРУДОВАНИЕ
        System.Diagnostics.Debug.WriteLine("Добавление оборудования...");
        SeedEquipment(context);
        saved = context.SaveChanges();
        System.Diagnostics.Debug.WriteLine($"Сохранено оборудования: {saved}");
    }

    private static void SeedPositions(OfficeDbContext context)
    {
        var positions = new[]
        {
            new Position { Name = "заведующий лабораторией", Salary = 120000m },
            new Position { Name = "техник", Salary = 40000m },
            new Position { Name = "лаборант", Salary = 15000m },
            new Position { Name = "инженер", Salary = 55000m },
            new Position { Name = "менеджер по персоналу", Salary = 100000m },
            new Position { Name = "администратор бд", Salary = 150000m },
            new Position { Name = "заведующий складом", Salary = 120000m }
        };

        foreach (var position in positions)
        {
            // Проверяем, нет ли уже такой должности
            if (!context.Positions.Any(p => p.Name == position.Name))
            {
                context.Positions.Add(position);
            }
        }
    }

    private static void SeedOffices(OfficeDbContext context)
    {
        var offices = new[]
        {
            new Office { FullName = "Лаборатория разработки и анализа селективных материалов и материалов для 3D печати", ShortName = "Лаборатория селективных материалов", Floor = 1, GeneralWorkerId = null },
            new Office { FullName = "засекречено", ShortName = "Лаборатория №6", Floor = null, GeneralWorkerId = null },
            new Office { FullName = "Первый Передовой Опытно-Конструкторский Отдел", ShortName = "ППОКО", Floor = 3, GeneralWorkerId = null },
            new Office { FullName = "Отделение машин и механизмов", ShortName = "Отделение машин и механизмов", Floor = 7, GeneralWorkerId = null },
            new Office { FullName = "Склад", ShortName = "Склад", Floor = -2, GeneralWorkerId = null },
            new Office { FullName = "Лаборатория нелинейной, геометрической оптики, спектрометрии", ShortName = "Лаборатория оптики", Floor = 2, GeneralWorkerId = null },
            new Office { FullName = "Департамент управления человеческими ресурсами", ShortName = "Отдел кадров", Floor = 1, GeneralWorkerId = null },
            new Office { FullName = "Общая для всех столовая", ShortName = "Столовая", Floor = 1, GeneralWorkerId = null }
        };

        foreach (var office in offices)
        {
            // Проверяем по уникальному ShortName или FullName
            if (!context.Offices.Any(o => o.ShortName == office.ShortName))
            {
                context.Offices.Add(office);
            }
        }
    }

    private static void SeedWorkers(OfficeDbContext context)
    {
        // Получаем ID должностей по названиям
        var positionIds = new Dictionary<string, int>
        {
            ["заведующий лабораторией"] = context.Positions.First(p => p.Name == "заведующий лабораторией").Id,
            ["техник"] = context.Positions.First(p => p.Name == "техник").Id,
            ["лаборант"] = context.Positions.First(p => p.Name == "лаборант").Id,
            ["инженер"] = context.Positions.First(p => p.Name == "инженер").Id,
            ["менеджер по персоналу"] = context.Positions.First(p => p.Name == "менеджер по персоналу").Id,
            ["администратор бд"] = context.Positions.First(p => p.Name == "администратор бд").Id,
            ["заведующий складом"] = context.Positions.First(p => p.Name == "заведующий складом").Id
        };

        // Получаем ID офисов по названиям
        var officeIds = new Dictionary<string, int?>
        {
            ["Лаборатория селективных материалов"] = context.Offices.First(o => o.ShortName == "Лаборатория селективных материалов").Id,
            ["Лаборатория №6"] = context.Offices.First(o => o.ShortName == "Лаборатория №6").Id,
            ["ППОКО"] = context.Offices.First(o => o.ShortName == "ППОКО").Id,
            ["Отделение машин и механизмов"] = context.Offices.First(o => o.ShortName == "Отделение машин и механизмов").Id,
            ["Склад"] = context.Offices.First(o => o.ShortName == "Склад").Id,
            ["Лаборатория оптики"] = context.Offices.First(o => o.ShortName == "Лаборатория оптики").Id,
            ["Отдел кадров"] = context.Offices.First(o => o.ShortName == "Отдел кадров").Id,
            ["Столовая"] = context.Offices.First(o => o.ShortName == "Столовая").Id
        };

        var workers = new[]
        {
            new Worker { Firstname = "Смирнов", Lastname = "Александр", Surname = "Владимирович",
                PositionId = positionIds["заведующий лабораторией"], BdYear = 1975, OfficeId = officeIds["Лаборатория селективных материалов"].Value,
                Login = "login1", Password = "password1" },
            new Worker { Firstname = "Иванова", Lastname = "Мария", Surname = "Сергеевна",
                PositionId = positionIds["техник"], BdYear = 1982, OfficeId = officeIds["Лаборатория селективных материалов"].Value,
                Login = "login2", Password = "password2" },
            new Worker { Firstname = "Козлов", Lastname = "Дмитрий", Surname = "Игоревич",
                PositionId = positionIds["лаборант"], BdYear = 1990, OfficeId = officeIds["Лаборатория селективных материалов"].Value,
                Login = "login3", Password = "password3" },
            new Worker { Firstname = "Петрова", Lastname = "Елена", Surname = "Александровна",
                PositionId = positionIds["инженер"], BdYear = 1988, OfficeId = officeIds["Лаборатория селективных материалов"].Value,
                Login = "login4", Password = "password4" },
            new Worker { Firstname = "Ивлев", Lastname = "Павел", Surname = "Николаевич",
                PositionId = positionIds["техник"], BdYear = 1995, OfficeId = officeIds["Лаборатория селективных материалов"].Value,
                Login = "login5", Password = "password5" },
            new Worker { Firstname = "Волков", Lastname = "Игорь", Surname = "Николаевич",
                PositionId = positionIds["заведующий лабораторией"], BdYear = 1970, OfficeId = officeIds["Лаборатория оптики"].Value,
                Login = "login6", Password = "password6" },
            new Worker { Firstname = "Никитина", Lastname = "Ольга", Surname = "Дмитриевна",
                PositionId = positionIds["техник"], BdYear = 1978, OfficeId = officeIds["Лаборатория оптики"].Value,
                Login = "login7", Password = "password7" },
            new Worker { Firstname = "Фёдоров", Lastname = "Максим", Surname = "Алексеевич",
                PositionId = positionIds["лаборант"], BdYear = 1985, OfficeId = officeIds["Лаборатория оптики"].Value,
                Login = "login8", Password = "password8" },
            new Worker { Firstname = "Павлова", Lastname = "Анна", Surname = "Викторовна",
                PositionId = positionIds["техник"], BdYear = 1980, OfficeId = officeIds["Столовая"].Value,
                Login = "login9", Password = "password9" },
            new Worker { Firstname = "Лебедев", Lastname = "Алексей", Surname = "Олегович",
                PositionId = positionIds["инженер"], BdYear = 1991, OfficeId = officeIds["Склад"].Value,
                Login = "login10", Password = "password10" },
            new Worker { Firstname = "Орлова", Lastname = "Ксения", Surname = "Романовна",
                PositionId = positionIds["лаборант"], BdYear = 1998, OfficeId = officeIds["Склад"].Value,
                Login = "login11", Password = "password11" },
            new Worker { Firstname = "Новиков", Lastname = "Сергей", Surname = "Валерьевич",
                PositionId = positionIds["заведующий лабораторией"], BdYear = 1972, OfficeId = officeIds["Отделение машин и механизмов"].Value,
                Login = "login12", Password = "password12" },
            new Worker { Firstname = "Морозова", Lastname = "Татьяна", Surname = "Ильинична",
                PositionId = positionIds["инженер"], BdYear = 1984, OfficeId = officeIds["Отделение машин и механизмов"].Value,
                Login = "login13", Password = "password13" },
            new Worker { Firstname = "Захаров", Lastname = "Павел", Surname = "Витальевич",
                PositionId = positionIds["инженер"], BdYear = 1993, OfficeId = officeIds["Отделение машин и механизмов"].Value,
                Login = "login14", Password = "password14" },
            new Worker { Firstname = "Борисова", Lastname = "Наталья", Surname = "Геннадьевна",
                PositionId = positionIds["заведующий лабораторией"], BdYear = 1977, OfficeId = officeIds["ППОКО"].Value,
                Login = "login15", Password = "password15" },
            new Worker { Firstname = "Кузнецов", Lastname = "Артём", Surname = "Юрьевич",
                PositionId = positionIds["техник"], BdYear = 1987, OfficeId = officeIds["ППОКО"].Value,
                Login = "login16", Password = "password16" },
            new Worker { Firstname = "Васнецова", Lastname = "Ирина", Surname = "Станиславовна",
                PositionId = positionIds["техник"], BdYear = 1996, OfficeId = officeIds["ППОКО"].Value,
                Login = "login17", Password = "password17" },
            new Worker { Firstname = "Тимофеев", Lastname = "Григорий", Surname = "Борисович",
                PositionId = positionIds["заведующий лабораторией"], BdYear = 1969, OfficeId = officeIds["Лаборатория №6"].Value,
                Login = "login18", Password = "password18" },
            new Worker { Firstname = "Соловьёва", Lastname = "Вероника", Surname = "Андреевна",
                PositionId = positionIds["инженер"], BdYear = 1981, OfficeId = officeIds["Лаборатория №6"].Value,
                Login = "login19", Password = "password19" },
            new Worker { Firstname = "Григорьев", Lastname = "Константин", Surname = "Львович",
                PositionId = positionIds["лаборант"], BdYear = 1992, OfficeId = officeIds["Лаборатория №6"].Value,
                Login = "login20", Password = "password20" },
            new Worker { Firstname = "Иванов", Lastname = "Иван", Surname = "Иванович",
                PositionId = positionIds["менеджер по персоналу"], BdYear = 1990, OfficeId = officeIds["Отдел кадров"].Value,
                Login = "login21", Password = "password21" },
            new Worker { Firstname = "Сидоров", Lastname = "Иван", Surname = "Иванович",
                PositionId = positionIds["техник"], BdYear = 1990, OfficeId = officeIds["Отдел кадров"].Value,
                Login = "login22", Password = "password22" },
            new Worker { Firstname = "Адимов", Lastname = "Вадмин", Surname = "Админович",
                PositionId = positionIds["администратор бд"], BdYear = 1980, OfficeId = officeIds["ППОКО"].Value,
                Login = "login23", Password = "password23" },
            new Worker { Firstname = "Складихина", Lastname = "Ксения", Surname = "Накопительнова",
                PositionId = positionIds["заведующий складом"], BdYear = 1998, OfficeId = officeIds["Склад"].Value,
                Login = "login24", Password = "password24" }
        };

        foreach (var worker in workers)
        {
            // Проверяем по логину
            if (!context.Workers.Any(w => w.Login == worker.Login))
            {
                context.Workers.Add(worker);
            }
        }
    }

  

    private static void SeedPlaces(OfficeDbContext context)
    {
        var officeIds = new Dictionary<string, int?>
        {
            ["Лаборатория селективных материалов"] = context.Offices.First(o => o.ShortName == "Лаборатория селективных материалов").Id,
            ["ППОКО"] = context.Offices.First(o => o.ShortName == "ППОКО").Id,
            ["Отделение машин и механизмов"] = context.Offices.First(o => o.ShortName == "Отделение машин и механизмов").Id,
            ["Лаборатория оптики"] = context.Offices.First(o => o.ShortName == "Лаборатория оптики").Id,
            ["Отдел кадров"] = context.Offices.First(o => o.ShortName == "Отдел кадров").Id,
            ["Столовая"] = context.Offices.First(o => o.ShortName == "Столовая").Id
        };

        var places = new[]
        {
            new Place { OfficeId = officeIds["Лаборатория селективных материалов"], Name = "191" },
            new Place { OfficeId = officeIds["Лаборатория селективных материалов"], Name = "101" },
            new Place { OfficeId = officeIds["ППОКО"], Name = "303" },
            new Place { OfficeId = officeIds["ППОКО"], Name = "309" },
            new Place { OfficeId = officeIds["ППОКО"], Name = "300" },
            new Place { OfficeId = officeIds["Отделение машин и механизмов"], Name = "702" },
            new Place { OfficeId = officeIds["Лаборатория оптики"], Name = "228" },
            new Place { OfficeId = officeIds["Лаборатория оптики"], Name = "229" },
            new Place { OfficeId = officeIds["Отдел кадров"], Name = "110" },
            new Place { OfficeId = officeIds["Столовая"], Name = "111" },
            new Place { OfficeId = null, Name = "засекречено" },
            new Place { OfficeId = null, Name = "склад" },
            new Place { OfficeId = null, Name = "в общем коридоре на 2 этаже" }
        };

        foreach (var place in places)
        {
            // Проверяем по уникальности (OfficeId + Name)
            if (!context.Places.Any(p => p.OfficeId == place.OfficeId && p.Name == place.Name))
            {
                context.Places.Add(place);
            }
        }
    }

    private static void SeedEquipment(OfficeDbContext context)
    {
        var places = context.Places.ToList();

        // Создаем словарь для поиска ID мест по их характеристикам
        var placeIds = new Dictionary<string, int>
        {
            ["191"] = places.First(p => p.Name == "191" && p.Office?.ShortName == "Лаборатория селективных материалов").Id,
            ["101"] = places.First(p => p.Name == "101" && p.Office?.ShortName == "Лаборатория селективных материалов").Id,
            ["303"] = places.First(p => p.Name == "303" && p.Office?.ShortName == "ППОКО").Id,
            ["702"] = places.First(p => p.Name == "702" && p.Office?.ShortName == "Отделение машин и механизмов").Id,
            ["228"] = places.First(p => p.Name == "228" && p.Office?.ShortName == "Лаборатория оптики").Id,
            ["229"] = places.First(p => p.Name == "229" && p.Office?.ShortName == "Лаборатория оптики").Id,
            ["110"] = places.First(p => p.Name == "110" && p.Office?.ShortName == "Отдел кадров").Id,
            ["111"] = places.First(p => p.Name == "111" && p.Office?.ShortName == "Столовая").Id,
            ["засекречено"] = places.First(p => p.Name == "засекречено").Id,
            ["склад"] = places.First(p => p.Name == "склад").Id,
            ["в общем коридоре на 2 этаже"] = places.First(p => p.Name == "в общем коридоре на 2 этаже").Id
        };

        var equipment = new[]
        {
            new Equipment { InventarNumber = "INV-MAT-191-001", Name = "Оборудование для 3D-печати", Description = "принтер, печатает штучки", Weight = 45, PhotoPath = null, ServiceStart = new DateOnly(2019, 1, 1), ServiceLife = 15, PlaceId = placeIds["191"] },
            new Equipment { InventarNumber = "INV-MAT-101-005", Name = "Аналитический спектрометр", Description = "советский!", Weight = 120, PhotoPath = null, ServiceStart = new DateOnly(1990, 1, 2), ServiceLife = 25, PlaceId = placeIds["101"] },
            new Equipment { InventarNumber = "INV-SEC-XX-001", Name = "Устройство для шифрования", Description = "з@$екЯече|-|0", Weight = 300, PhotoPath = null, ServiceStart = new DateOnly(1996, 1, 3), ServiceLife = 100, PlaceId = placeIds["засекречено"] },
            new Equipment { InventarNumber = "INV-PPO-303-012", Name = "Испытательный стенд для компонентов", Description = "трясет образцы до достижения резонанса", Weight = 850, PhotoPath = null, ServiceStart = new DateOnly(1980, 8, 6), ServiceLife = 13, PlaceId = placeIds["303"] },
            new Equipment { InventarNumber = "INV-MM-702-007", Name = "Станок с ЧПУ", Description = "точит детали", Weight = 1200, PhotoPath = null, ServiceStart = new DateOnly(2004, 1, 5), ServiceLife = 30, PlaceId = placeIds["702"] },
            new Equipment { InventarNumber = "INV-SKL-002-101", Name = "Стеллаж паллетный", Description = "просто стойка", Weight = 80, PhotoPath = null, ServiceStart = new DateOnly(2004, 1, 6), ServiceLife = 50, PlaceId = placeIds["склад"] },
            new Equipment { InventarNumber = "INV-OPT-228-003", Name = "Оптический стол (виброизолированный)", Description = "тяжелый, но надежный", Weight = 200, PhotoPath = "table.jpg", ServiceStart = new DateOnly(2004, 1, 7), ServiceLife = 40, PlaceId = placeIds["228"] },
            new Equipment { InventarNumber = "INV-OPT-229-001", Name = "Монохроматор высокого разрешения", Description = "full HD", Weight = 65, PhotoPath = null, ServiceStart = new DateOnly(2014, 7, 8), ServiceLife = 30, PlaceId = placeIds["229"] },
            new Equipment { InventarNumber = "INV-HR-110-001", Name = "Шкаф для документов (сейф)", Description = "толщина стенок 55мм", Weight = 150, PhotoPath = null, ServiceStart = new DateOnly(2020, 1, 9), ServiceLife = 100, PlaceId = placeIds["110"] },
            new Equipment { InventarNumber = "INV-010", Name = "Кофемашина", Description = "делает горький кофэ", Weight = 15, PhotoPath = null, ServiceStart = new DateOnly(2021, 11, 10), ServiceLife = 5, PlaceId = placeIds["в общем коридоре на 2 этаже"] },
            new Equipment { InventarNumber = "INV-011", Name = "Кофемашина", Description = "Старая, но надежная", Weight = 20, PhotoPath = null, ServiceStart = new DateOnly(2004, 1, 11), ServiceLife = 25, PlaceId = placeIds["111"] },
            new Equipment { InventarNumber = "INV-012", Name = "Кофемашина", Description = "делает не горький кофэ и даже латте", Weight = 5, PhotoPath = "coffee.jpg", ServiceStart = new DateOnly(2024, 7, 5), ServiceLife = 5, PlaceId = placeIds["в общем коридоре на 2 этаже"] }
        };

        foreach (var item in equipment)
        {
            // Проверяем по инвентарному номеру
            if (!context.Equipment.Any(e => e.InventarNumber == item.InventarNumber))
            {
                context.Equipment.Add(item);
            }
        }
    }
}