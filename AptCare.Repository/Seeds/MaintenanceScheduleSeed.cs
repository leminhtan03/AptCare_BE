using AptCare.Repository.Entities;
using AptCare.Repository.Enum;
using Microsoft.EntityFrameworkCore;

namespace AptCare.Repository.Seeds
{
    public static class MaintenanceScheduleSeed
    {
        public static async Task SeedAsync(AptCareSystemDBContext context)
        {
            if (context.MaintenanceSchedules.Any()) return;

            var commonAreaObjects = await context.CommonAreaObjects
                .Include(cao => cao.CommonAreaObjectType)
                    .ThenInclude(t => t.MaintenanceTasks)
                .Include(cao => cao.CommonArea)
                .Where(cao => cao.Status == ActiveStatus.Active)
                .ToListAsync();

            var techniques = await context.Techniques.ToListAsync();

            if (!commonAreaObjects.Any() || !techniques.Any()) return;

            var schedules = new List<MaintenanceSchedule>();
            var random = new Random(42);
            var today = DateOnly.FromDateTime(DateTime.Now);

            // Map ObjectType -> Technique phù h?p
            var typeToTechnique = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Thang máy", "C? khí - C?a - Khóa" },
                { "?èn chi?u sáng", "?i?n" },
                { "Camera an ninh", "Internet - H? th?ng m?ng" },
                { "C?m bi?n báo cháy", "?i?n" },
                { "T? ?i?n", "?i?n" },
                { "Qu?t thông gió", "?i?u hòa - Thông gió" },
                { "B?n n??c", "N??c" },
                { "Máy l?c n??c", "N??c" },
                { "C?m bi?n CO", "Môi tr??ng - V? sinh" }
            };

            // Chu k? b?o trì theo lo?i thi?t b? (ngày)
            var typeToFrequency = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                { "Thang máy", 30 },           // Hàng tháng
                { "?èn chi?u sáng", 90 },      // 3 tháng
                { "Camera an ninh", 60 },       // 2 tháng
                { "C?m bi?n báo cháy", 180 },  // 6 tháng
                { "T? ?i?n", 90 },             // 3 tháng
                { "Qu?t thông gió", 60 },      // 2 tháng
                { "B?n n??c", 180 },           // 6 tháng
                { "Máy l?c n??c", 30 },        // Hàng tháng
                { "C?m bi?n CO", 90 }          // 3 tháng
            };

            // S? k? thu?t viên c?n theo lo?i
            var typeToTechnicians = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                { "Thang máy", 2 },
                { "?èn chi?u sáng", 1 },
                { "Camera an ninh", 1 },
                { "C?m bi?n báo cháy", 1 },
                { "T? ?i?n", 2 },
                { "Qu?t thông gió", 1 },
                { "B?n n??c", 2 },
                { "Máy l?c n??c", 1 },
                { "C?m bi?n CO", 1 }
            };

            // Th?i gian ?u tiên b?o trì (sáng s?m ho?c t?i ?? ít ?nh h??ng c? dân)
            var preferredTimes = new[]
            {
                new TimeSpan(7, 0, 0),   // 7:00
                new TimeSpan(8, 0, 0),   // 8:00
                new TimeSpan(9, 0, 0),   // 9:00
                new TimeSpan(14, 0, 0),  // 14:00
                new TimeSpan(15, 0, 0),  // 15:00
            };

            foreach (var cao in commonAreaObjects)
            {
                var typeName = cao.CommonAreaObjectType.TypeName;

                // Tính EstimatedDuration t? MaintenanceTasks
                var tasks = cao.CommonAreaObjectType.MaintenanceTasks ?? new List<MaintenanceTask>();
                var estimatedDurationHours = tasks.Any()
                    ? tasks.Sum(t => t.EstimatedDurationMinutes) / 60.0
                    : 1.0;

                // L?y technique phù h?p
                Technique? requiredTechnique = null;
                if (typeToTechnique.TryGetValue(typeName, out var techniqueName))
                {
                    requiredTechnique = techniques.FirstOrDefault(t =>
                        t.Name.Contains(techniqueName, StringComparison.OrdinalIgnoreCase));
                }

                // L?y frequency
                var frequency = typeToFrequency.TryGetValue(typeName, out var freq) ? freq : 60;

                // L?y s? k? thu?t viên
                var requiredTechs = typeToTechnicians.TryGetValue(typeName, out var techs) ? techs : 1;

                // Tính ngày b?o trì ti?p theo (random trong 1-frequency ngày t?i)
                var daysUntilNext = random.Next(1, frequency + 1);
                var nextScheduledDate = today.AddDays(daysUntilNext);

                // Tính ngày b?o trì g?n nh?t (n?u có - gi? l?p ?ã b?o trì trong quá kh?)
                DateOnly? lastMaintenanceDate = null;
                if (random.Next(100) < 70) // 70% ?ã có l?n b?o trì tr??c
                {
                    var daysSinceLast = random.Next(frequency / 2, frequency * 2);
                    lastMaintenanceDate = today.AddDays(-daysSinceLast);
                }

                // Ch?n th?i gian ?u tiên
                var timePreference = preferredTimes[random.Next(preferredTimes.Length)];

                // T?o description
                var description = GenerateDescription(typeName, cao.Name, frequency);

                schedules.Add(new MaintenanceSchedule
                {
                    CommonAreaObjectId = cao.CommonAreaObjectId,
                    Description = description,
                    FrequencyInDays = frequency,
                    NextScheduledDate = nextScheduledDate,
                    LastMaintenanceDate = lastMaintenanceDate,
                    TimePreference = timePreference,
                    RequiredTechniqueId = requiredTechnique?.TechniqueId,
                    RequiredTechnicians = requiredTechs,
                    EstimatedDuration = estimatedDurationHours,
                    CreatedAt = DateTime.Now.AddMonths(-random.Next(1, 6)), // T?o trong 1-6 tháng tr??c
                    Status = ActiveStatus.Active
                });
            }

            context.MaintenanceSchedules.AddRange(schedules);
            await context.SaveChangesAsync();
        }

        private static string GenerateDescription(string typeName, string objectName, int frequency)
        {
            var frequencyText = frequency switch
            {
                30 => "hàng tháng",
                60 => "2 tháng/l?n",
                90 => "3 tháng/l?n",
                180 => "6 tháng/l?n",
                365 => "hàng n?m",
                _ => $"{frequency} ngày/l?n"
            };

            return typeName switch
            {
                "Thang máy" => $"B?o trì ??nh k? {frequencyText} cho {objectName}. Ki?m tra dây cáp, phanh, c?a cabin và tra d?u ??ng c?.",
                "?èn chi?u sáng" => $"Ki?m tra và b?o trì h? th?ng chi?u sáng {frequencyText}. Thay bóng h?ng, v? sinh ch?p ?èn.",
                "Camera an ninh" => $"B?o trì camera {frequencyText}. Ki?m tra góc quay, v? sinh ?ng kính, test k?t n?i m?ng.",
                "C?m bi?n báo cháy" => $"Ki?m tra h? th?ng PCCC {frequencyText}. Test c?m bi?n khói, v? sinh, ki?m tra pin backup.",
                "T? ?i?n" => $"B?o trì t? ?i?n {frequencyText}. Ki?m tra CB, si?t ??u n?i, ?o nhi?t ?? phát hi?n ?i?m nóng.",
                "Qu?t thông gió" => $"B?o trì qu?t thông gió {frequencyText}. V? sinh cánh qu?t, ki?m tra motor, tra d?u ? tr?c.",
                "B?n n??c" => $"V? sinh và b?o trì b?n n??c {frequencyText}. C? r?a, kh? trùng, ki?m tra phao n??c.",
                "Máy l?c n??c" => $"B?o trì máy l?c n??c {frequencyText}. Thay lõi l?c, ki?m tra áp su?t, v? sinh b?m.",
                "C?m bi?n CO" => $"Ki?m tra c?m bi?n CO {frequencyText}. Test ?? nh?y, hi?u chu?n ng??ng báo ??ng.",
                _ => $"B?o trì ??nh k? {frequencyText} cho {objectName}."
            };
        }
    }
}
