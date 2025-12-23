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

            // Map ObjectType -> Technique phu hop
            var typeToTechnique = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Thang may", "Co khi - Cua - Khoa" },
                { "Den chieu sang", "Dien" },
                { "Camera an ninh", "Internet - He thong mang" },
                { "Cam bien bao chay", "Dien" },
                { "Tu dien", "Dien" },
                { "Quat thong gio", "Dieu hoa - Thong gio" },
                { "Bon nuoc", "Nuoc" },
                { "May loc nuoc", "Nuoc" },
                { "Cam bien CO", "Moi truong - Ve sinh" }
            };

            // Chu ky bao tri theo loai thiet bi (ngay)
            var typeToFrequency = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                { "Thang may", 30 },
                { "Den chieu sang", 90 },
                { "Camera an ninh", 60 },
                { "Cam bien bao chay", 180 },
                { "Tu dien", 90 },
                { "Quat thong gio", 60 },
                { "Bon nuoc", 180 },
                { "May loc nuoc", 30 },
                { "Cam bien CO", 90 }
            };

            // So ky thuat vien can theo loai
            var typeToTechnicians = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                { "Thang may", 2 },
                { "Den chieu sang", 1 },
                { "Camera an ninh", 1 },
                { "Cam bien bao chay", 1 },
                { "Tu dien", 2 },
                { "Quat thong gio", 1 },
                { "Bon nuoc", 2 },
                { "May loc nuoc", 1 },
                { "Cam bien CO", 1 }
            };

            // Thoi gian uu tien bao tri
            var preferredTimes = new[]
            {
                new TimeSpan(7, 0, 0),
                new TimeSpan(8, 0, 0),
                new TimeSpan(9, 0, 0),
                new TimeSpan(14, 0, 0),
                new TimeSpan(15, 0, 0),
            };

            int objectIndex = 0;
            foreach (var cao in commonAreaObjects)
            {
                var typeName = cao.CommonAreaObjectType.TypeName;

                // Tinh EstimatedDuration tu MaintenanceTasks
                var tasks = cao.CommonAreaObjectType.MaintenanceTasks ?? new List<MaintenanceTask>();
                var estimatedDurationHours = tasks.Any()
                    ? tasks.Sum(t => t.EstimatedDurationMinutes) / 60.0
                    : 1.0;

                // Lay technique phu hop
                Technique? requiredTechnique = null;
                foreach (var kvp in typeToTechnique)
                {
                    if (typeName.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase) ||
                        RemoveDiacritics(typeName).Contains(kvp.Key, StringComparison.OrdinalIgnoreCase))
                    {
                        requiredTechnique = techniques.FirstOrDefault(t =>
                            t.Name.Contains(kvp.Value, StringComparison.OrdinalIgnoreCase) ||
                            RemoveDiacritics(t.Name).Contains(kvp.Value, StringComparison.OrdinalIgnoreCase));
                        break;
                    }
                }

                // Lay frequency
                int frequency = 60;
                foreach (var kvp in typeToFrequency)
                {
                    if (typeName.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase) ||
                        RemoveDiacritics(typeName).Contains(kvp.Key, StringComparison.OrdinalIgnoreCase))
                    {
                        frequency = kvp.Value;
                        break;
                    }
                }

                // Lay so ky thuat vien
                int requiredTechs = 1;
                foreach (var kvp in typeToTechnicians)
                {
                    if (typeName.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase) ||
                        RemoveDiacritics(typeName).Contains(kvp.Key, StringComparison.OrdinalIgnoreCase))
                    {
                        requiredTechs = kvp.Value;
                        break;
                    }
                }

                var daysUntilNext = 4 + (objectIndex * 3) + random.Next(1, 5);
                var nextScheduledDate = today.AddDays(daysUntilNext);

                DateOnly? lastMaintenanceDate = null;
                if (random.Next(100) < 70)
                {
                    var daysSinceLast = random.Next(frequency / 2, frequency);
                    lastMaintenanceDate = today.AddDays(-daysSinceLast);
                }

                var timePreference = preferredTimes[objectIndex % preferredTimes.Length];

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
                    CreatedAt = DateTime.Now.AddMonths(-random.Next(1, 6)),
                    Status = ActiveStatus.Active
                });

                objectIndex++;
            }

            context.MaintenanceSchedules.AddRange(schedules);
            await context.SaveChangesAsync();
        }

        private static string RemoveDiacritics(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            var normalizedString = text.Normalize(System.Text.NormalizationForm.FormD);
            var stringBuilder = new System.Text.StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != System.Globalization.UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(System.Text.NormalizationForm.FormC);
        }

        private static string GenerateDescription(string typeName, string objectName, int frequency)
        {
            var frequencyText = frequency switch
            {
                30 => "hang thang",
                60 => "2 thang/lan",
                90 => "3 thang/lan",
                180 => "6 thang/lan",
                365 => "hang nam",
                _ => $"{frequency} ngay/lan"
            };

            var typeNameNormalized = RemoveDiacritics(typeName).ToLower();

            if (typeNameNormalized.Contains("thang may"))
                return $"Bao tri dinh ky {frequencyText} cho {objectName}. Kiem tra day cap, phanh, cua cabin va tra dau dong co.";

            if (typeNameNormalized.Contains("den") || typeNameNormalized.Contains("chieu sang"))
                return $"Kiem tra va bao tri he thong chieu sang {frequencyText}. Thay bong hong, ve sinh chup den.";

            if (typeNameNormalized.Contains("camera"))
                return $"Bao tri camera {frequencyText}. Kiem tra goc quay, ve sinh ong kinh, test ket noi mang.";

            if (typeNameNormalized.Contains("cam bien") && typeNameNormalized.Contains("chay"))
                return $"Kiem tra he thong PCCC {frequencyText}. Test cam bien khoi, ve sinh, kiem tra pin backup.";

            if (typeNameNormalized.Contains("tu dien"))
                return $"Bao tri tu dien {frequencyText}. Kiem tra CB, siet dau noi, do nhiet do phat hien diem nong.";

            if (typeNameNormalized.Contains("quat") || typeNameNormalized.Contains("thong gio"))
                return $"Bao tri quat thong gio {frequencyText}. Ve sinh canh quat, kiem tra motor, tra dau o truc.";

            if (typeNameNormalized.Contains("bon nuoc"))
                return $"Ve sinh va bao tri bon nuoc {frequencyText}. Co rua, khu trung, kiem tra phao nuoc.";

            if (typeNameNormalized.Contains("may loc") || typeNameNormalized.Contains("loc nuoc"))
                return $"Bao tri may loc nuoc {frequencyText}. Thay loi loc, kiem tra ap suat, ve sinh bom.";

            if (typeNameNormalized.Contains("cam bien") && typeNameNormalized.Contains("co"))
                return $"Kiem tra cam bien CO {frequencyText}. Test do nhay, hieu chuan nguong bao dong.";

            return $"Bao tri dinh ky {frequencyText} cho {objectName}.";
        }
    }
}
