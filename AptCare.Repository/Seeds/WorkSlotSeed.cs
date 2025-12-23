using AptCare.Repository.Entities;
using AptCare.Repository.Enum;
using AptCare.Repository.Enum.AccountUserEnum;
using Microsoft.EntityFrameworkCore;

namespace AptCare.Repository.Seeds
{
    public static class WorkSlotSeed
    {
        public static async Task SeedAsync(AptCareSystemDBContext context)
        {
            if (context.WorkSlots.Any()) return;

            var technicians = await context.Users
                .Include(u => u.Account)
                .Where(u => u.Account.Role == AccountRole.Technician)
                .ToListAsync();

            var slots = await context.Slots.ToListAsync();
            if (!technicians.Any() || !slots.Any()) return;

            var morningSlot = slots.FirstOrDefault(s => s.SlotName == "Ca sáng");
            var eveningSlot = slots.FirstOrDefault(s => s.SlotName == "Ca tối");
            var nightSlot = slots.FirstOrDefault(s => s.SlotName == "Ca đêm");

            if (morningSlot == null || eveningSlot == null || nightSlot == null) return;

            var workSlots = new List<WorkSlot>();
            var random = new Random(42);

            var today = DateOnly.FromDateTime(DateTime.Now);

            var sixMonthsAgo = today.AddMonths(-6);
            var jan2025 = new DateOnly(2025, 1, 1);
            var startDate = sixMonthsAgo > jan2025 ? sixMonthsAgo : jan2025;

            var endDate = today.AddMonths(3);

            var previousDaySlots = new Dictionary<int, int>();

            var incompatibleNextSlots = new Dictionary<int, int>
            {
                { nightSlot.SlotId, morningSlot.SlotId },
                { eveningSlot.SlotId, nightSlot.SlotId }
            };

            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                int morningCount = GetSlotCount(date, "morning", random);
                int eveningCount = GetSlotCount(date, "evening", random);
                int nightCount = GetSlotCount(date, "night", random);

                var availableTechs = technicians.OrderBy(_ => random.Next()).ToList();
                var todayAssignments = new Dictionary<int, int>();

                bool CanAssignSlot(User tech, int slotId)
                {
                    if (todayAssignments.ContainsKey(tech.UserId))
                        return false;

                    if (previousDaySlots.TryGetValue(tech.UserId, out var yesterdaySlotId))
                    {
                        if (incompatibleNextSlots.TryGetValue(yesterdaySlotId, out var incompatibleSlotId))
                        {
                            if (slotId == incompatibleSlotId)
                                return false;
                        }
                    }

                    return true;
                }

                WorkSlotStatus GetStatus(DateOnly workDate)
                {
                    if (workDate < today)
                        return WorkSlotStatus.Completed;
                    else if (workDate == today)
                        return WorkSlotStatus.Working;
                    else
                        return WorkSlotStatus.NotStarted;
                }

                var assignedMorning = 0;
                foreach (var tech in availableTechs.Where(t => CanAssignSlot(t, morningSlot.SlotId)))
                {
                    if (assignedMorning >= morningCount) break;

                    workSlots.Add(new WorkSlot
                    {
                        TechnicianId = tech.UserId,
                        SlotId = morningSlot.SlotId,
                        Date = date,
                        Status = GetStatus(date)
                    });
                    todayAssignments[tech.UserId] = morningSlot.SlotId;
                    assignedMorning++;
                }

                var assignedEvening = 0;
                foreach (var tech in availableTechs.Where(t => CanAssignSlot(t, eveningSlot.SlotId)))
                {
                    if (assignedEvening >= eveningCount) break;

                    workSlots.Add(new WorkSlot
                    {
                        TechnicianId = tech.UserId,
                        SlotId = eveningSlot.SlotId,
                        Date = date,
                        Status = GetStatus(date)
                    });
                    todayAssignments[tech.UserId] = eveningSlot.SlotId;
                    assignedEvening++;
                }

                var assignedNight = 0;
                foreach (var tech in availableTechs.Where(t => CanAssignSlot(t, nightSlot.SlotId)))
                {
                    if (assignedNight >= nightCount) break;

                    workSlots.Add(new WorkSlot
                    {
                        TechnicianId = tech.UserId,
                        SlotId = nightSlot.SlotId,
                        Date = date,
                        Status = GetStatus(date)
                    });
                    todayAssignments[tech.UserId] = nightSlot.SlotId;
                    assignedNight++;
                }

                previousDaySlots = new Dictionary<int, int>(todayAssignments);
            }

            context.WorkSlots.AddRange(workSlots);
            await context.SaveChangesAsync();
        }

        private static int GetSlotCount(DateOnly date, string slotType, Random random)
        {
            var dayOfWeek = date.DayOfWeek;
            var isWeekend = dayOfWeek == DayOfWeek.Saturday || dayOfWeek == DayOfWeek.Sunday;

            return slotType switch
            {
                "morning" => isWeekend ? random.Next(2, 4) : random.Next(3, 5),
                "evening" => isWeekend ? random.Next(2, 3) : random.Next(2, 4),
                "night" => random.Next(1, 3),
                _ => 2
            };
        }
    }
}