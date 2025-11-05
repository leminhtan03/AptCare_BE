using AptCare.Repository.Entities;
using AptCare.Repository.Enum;
using AptCare.Repository.Enum.AccountUserEnum;
using AptCare.Repository.Enum.Apartment;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AptCare.Repository.Seeds
{
    public class Seed
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<AptCareSystemDBContext>();

            await SeedFloors(context);
            await SeedApartments(context);
            await SeedTechniques(context);
            await SeedIssues(context);
            await SeedUsersAndAccounts(context);
            await SeedUserApartments(context);
            await SeedCommonAreas(context);
            await SeedCommonAreaObjects(context);
            await SeedSlots(context);
            await SeedAccessories(context);
        }

        private static async Task SeedFloors(AptCareSystemDBContext context)
        {
            if (!context.Floors.Any())
            {
                var floors = new List<Floor>();
                for (int i = 1; i <= 10; i++)
                {
                    floors.Add(new Floor
                    {
                        FloorNumber = i,
                        Status = ActiveStatus.Active,
                        Description = $"Tầng {i}"
                    });
                }
                context.Floors.AddRange(floors);
                await context.SaveChangesAsync();
            }
        }

        private static async Task SeedApartments(AptCareSystemDBContext context)
        {
            if (!context.Apartments.Any())
            {
                var apartments = new List<Apartment>();
                var floors = context.Floors.ToList();
                foreach (var floor in floors)
                {
                    for (int p = 1; p <= 10; p++)
                    {
                        string roomNumber = $"P{floor.FloorNumber}{p:D2}";

                        // Chia loại căn hộ: 1-5 là loại A (4 người), 6-10 là loại B (6 người)
                        bool isLarge = p > 5;

                        apartments.Add(new Apartment
                        {
                            Room = roomNumber,
                            FloorId = floor.FloorId,
                            Area = isLarge ? 110 : 70,
                            Limit = isLarge ? 6 : 4,
                            Status = ApartmentStatus.Active,
                            Description = $"Căn hộ {roomNumber} thuộc tầng {floor.FloorNumber}, loại {(isLarge ? "lớn" : "nhỏ")}"
                        });
                    }
                }
                context.Apartments.AddRange(apartments);
                await context.SaveChangesAsync();
            }
        }

        private static async Task SeedTechniques(AptCareSystemDBContext context)
        {
            if (!context.Techniques.Any())
            {
                var techniques = new List<Technique>
                {
                    new Technique { Name = "Điện", Description = "Sửa chữa, bảo trì hệ thống điện trong căn hộ và khu chung cư" },
                    new Technique { Name = "Nước", Description = "Xử lý sự cố rò rỉ, tắc nghẽn, áp lực nước" },
                    new Technique { Name = "Điều hòa - Thông gió", Description = "Vệ sinh, bảo trì máy lạnh, quạt thông gió" },
                    new Technique { Name = "Cơ khí - Cửa - Khóa", Description = "Sửa cửa, bản lề, ổ khóa, lan can" },
                    new Technique { Name = "Chiếu sáng công cộng", Description = "Thay bóng đèn, kiểm tra tủ điện tầng" },
                    new Technique { Name = "Môi trường - Vệ sinh", Description = "Khử mùi, vệ sinh kỹ thuật, bảo trì bồn chứa" },
                    new Technique { Name = "Internet - Hệ thống mạng", Description = "Xử lý sự cố mạng, camera, intercom" },
                    new Technique { Name = "Thang máy", Description = "Theo dõi, báo lỗi, bảo trì hệ thống thang máy" },
                    new Technique { Name = "Kết cấu - Xây dựng", Description = "Vá tường, sơn sửa, lát gạch, xử lý thấm" }
                };

                context.Techniques.AddRange(techniques);
                await context.SaveChangesAsync();
            }
        }

        private static async Task SeedIssues(AptCareSystemDBContext context)
        {
            if (!context.Issues.Any())
            {
                var issues = new List<Issue>
                {
                    // 🔌 Điện
                    new Issue { TechniqueId = 1, Name = "Mất điện một phần", Description = "CB hoặc dây điện trong phòng bị lỗi", IsEmergency = true, RequiredTechnician = 1, EstimatedDuration = 2, Status = ActiveStatus.Active },
                    new Issue { TechniqueId = 1, Name = "Ổ cắm không hoạt động", Description = "Lỏng dây hoặc cháy tiếp điểm", IsEmergency = false, RequiredTechnician = 1, EstimatedDuration = 1, Status = ActiveStatus.Active },
                    new Issue { TechniqueId = 1, Name = "Đèn trần không sáng", Description = "Thay bóng đèn hoặc công tắc", IsEmergency = false, RequiredTechnician = 1, EstimatedDuration = 1, Status = ActiveStatus.Active },
                    new Issue { TechniqueId = 1, Name = "CB nhảy liên tục", Description = "Quá tải hoặc chập mạch", IsEmergency = true, RequiredTechnician = 1, EstimatedDuration = 2, Status = ActiveStatus.Active },
                    new Issue { TechniqueId = 1, Name = "Quạt trần bị kêu", Description = "Lỏng trục hoặc cánh quạt", IsEmergency = false, RequiredTechnician = 1, EstimatedDuration = 1, Status = ActiveStatus.Active },

                    // 💧 Nước
                    new Issue { TechniqueId = 2, Name = "Vòi nước rò rỉ", Description = "Rò tại đầu nối hoặc ron cao su", IsEmergency = false, RequiredTechnician = 1, EstimatedDuration = 1, Status = ActiveStatus.Active },
                    new Issue { TechniqueId = 2, Name = "Bồn cầu không xả", Description = "Van xả hoặc phao nước hỏng", IsEmergency = true, RequiredTechnician = 1, EstimatedDuration = 2, Status = ActiveStatus.Active },
                    new Issue { TechniqueId = 2, Name = "Bồn rửa tắc nghẽn", Description = "Thức ăn hoặc tóc kẹt trong ống", IsEmergency = false, RequiredTechnician = 1, EstimatedDuration = 1, Status = ActiveStatus.Active },
                    new Issue { TechniqueId = 2, Name = "Rò nước âm tường", Description = "Ống nước vỡ hoặc rò trong tường", IsEmergency = true, RequiredTechnician = 2, EstimatedDuration = 3, Status = ActiveStatus.Active },
                    new Issue { TechniqueId = 2, Name = "Máy nước nóng không hoạt động", Description = "Lỗi nguồn hoặc cảm biến nhiệt", IsEmergency = false, RequiredTechnician = 1, EstimatedDuration = 2, Status = ActiveStatus.Active },

                    // ❄️ Điều hòa
                    new Issue { TechniqueId = 3, Name = "Máy lạnh không lạnh", Description = "Thiếu gas hoặc hỏng block", IsEmergency = false, RequiredTechnician = 1, EstimatedDuration = 2, Status = ActiveStatus.Active },
                    new Issue { TechniqueId = 3, Name = "Máy lạnh chảy nước", Description = "Ống xả ngưng tụ tắc", IsEmergency = false, RequiredTechnician = 1, EstimatedDuration = 1, Status = ActiveStatus.Active },
                    new Issue { TechniqueId = 3, Name = "Có mùi hôi khi bật máy", Description = "Cần vệ sinh dàn lạnh", IsEmergency = false, RequiredTechnician = 1, EstimatedDuration = 1, Status = ActiveStatus.Active },
                    new Issue { TechniqueId = 3, Name = "Quạt thông gió không chạy", Description = "Lỗi motor hoặc kẹt bụi", IsEmergency = false, RequiredTechnician = 1, EstimatedDuration = 1, Status = ActiveStatus.Active },

                    // 🚪 Cửa - Khóa
                    new Issue { TechniqueId = 4, Name = "Cửa bị kẹt", Description = "Lệch bản lề hoặc ray trượt", IsEmergency = false, RequiredTechnician = 1, EstimatedDuration = 1, Status = ActiveStatus.Active },
                    new Issue { TechniqueId = 4, Name = "Khóa cửa bị kẹt", Description = "Tra dầu hoặc thay lõi khóa", IsEmergency = true, RequiredTechnician = 1, EstimatedDuration = 1, Status = ActiveStatus.Active },
                    new Issue { TechniqueId = 4, Name = "Cửa kính bị lệch ray", Description = "Điều chỉnh lại khung trượt", IsEmergency = false, RequiredTechnician = 1, EstimatedDuration = 1, Status = ActiveStatus.Active },
                    new Issue { TechniqueId = 4, Name = "Tay nắm cửa lỏng", Description = "Siết lại ốc hoặc thay mới", IsEmergency = false, RequiredTechnician = 1, EstimatedDuration = 1, Status = ActiveStatus.Active },

                    // 💡 Chiếu sáng
                    new Issue { TechniqueId = 5, Name = "Đèn phòng khách không sáng", Description = "Bóng hỏng hoặc lỗi dây điện", IsEmergency = false, RequiredTechnician = 1, EstimatedDuration = 1, Status = ActiveStatus.Active },
                    new Issue { TechniqueId = 5, Name = "Công tắc không hoạt động", Description = "Tiếp điểm hỏng hoặc lỏng dây", IsEmergency = false, RequiredTechnician = 1, EstimatedDuration = 1, Status = ActiveStatus.Active },
                    new Issue { TechniqueId = 5, Name = "Đèn nhấp nháy", Description = "Điện áp không ổn định hoặc hỏng ballast", IsEmergency = false, RequiredTechnician = 1, EstimatedDuration = 1, Status = ActiveStatus.Active },

                    // 🧼 Vệ sinh - Môi trường
                    new Issue { TechniqueId = 6, Name = "Mùi hôi từ cống", Description = "Xi-phông khô hoặc tắc", IsEmergency = false, RequiredTechnician = 1, EstimatedDuration = 1, Status = ActiveStatus.Active },
                    new Issue { TechniqueId = 6, Name = "Sàn nhà tắm đọng nước", Description = "Ống thoát sàn nghẽn", IsEmergency = false, RequiredTechnician = 1, EstimatedDuration = 1, Status = ActiveStatus.Active },
                    new Issue { TechniqueId = 6, Name = "Nước tràn từ máy giặt", Description = "Ống xả đặt sai hoặc tắc nghẽn", IsEmergency = true, RequiredTechnician = 1, EstimatedDuration = 2, Status = ActiveStatus.Active },

                    // 🌐 Internet - SmartHome
                    new Issue { TechniqueId = 7, Name = "Mất kết nối Wi-Fi", Description = "Router lỗi hoặc đứt dây mạng", IsEmergency = false, RequiredTechnician = 1, EstimatedDuration = 1, Status = ActiveStatus.Active },
                    new Issue { TechniqueId = 7, Name = "Thiết bị smart home không phản hồi", Description = "Kiểm tra kết nối trung tâm", IsEmergency = false, RequiredTechnician = 1, EstimatedDuration = 1, Status = ActiveStatus.Active },
                    new Issue { TechniqueId = 7, Name = "Ổ cắm thông minh không hoạt động", Description = "Lỗi nguồn hoặc cấu hình sai", IsEmergency = false, RequiredTechnician = 1, EstimatedDuration = 1, Status = ActiveStatus.Active },

                    // 🔥 PCCC trong căn hộ
                    new Issue { TechniqueId = 8, Name = "Đầu báo khói kêu liên tục", Description = "Bụi bẩn hoặc pin yếu", IsEmergency = false, RequiredTechnician = 1, EstimatedDuration = 1, Status = ActiveStatus.Active },
                    new Issue { TechniqueId = 8, Name = "Báo cháy giả trong phòng", Description = "Nhạy quá mức, cần hiệu chỉnh", IsEmergency = false, RequiredTechnician = 1, EstimatedDuration = 1, Status = ActiveStatus.Active },

                    // 🧱 Xây dựng - Kết cấu
                    new Issue { TechniqueId = 9, Name = "Tường bị thấm nước", Description = "Cần xử lý chống thấm", IsEmergency = false, RequiredTechnician = 2, EstimatedDuration = 3, Status = ActiveStatus.Active },
                    new Issue { TechniqueId = 9, Name = "Sơn bong tróc", Description = "Cạo và sơn lại bề mặt", IsEmergency = false, RequiredTechnician = 1, EstimatedDuration = 2, Status = ActiveStatus.Active },
                    new Issue { TechniqueId = 9, Name = "Nứt trần", Description = "Trám vá và sơn lại khu vực nứt", IsEmergency = false, RequiredTechnician = 2, EstimatedDuration = 2, Status = ActiveStatus.Active },
                    new Issue { TechniqueId = 9, Name = "Gạch nền bị rộp", Description = "Thay gạch hoặc trám vữa lại", IsEmergency = false, RequiredTechnician = 2, EstimatedDuration = 3, Status = ActiveStatus.Active }
                };

                context.Issues.AddRange(issues);
                await context.SaveChangesAsync();
            }
        }

        private static async Task SeedUsersAndAccounts(AptCareSystemDBContext context)
        {
            if (!context.Users.Any())
            {
                var passwordHasher = new PasswordHasher<Account>();
                var users = new List<User>();
                var accounts = new List<Account>();
                var technicianTechniques = new List<TechnicianTechnique>();

                // Base index cho từng nhóm
                int cccdAdmin = 90000000;
                int phoneAdmin = 890000000;

                int cccdManager = 100000000;
                int phoneManager = 900000000;

                int cccdTechLead = 110000000;
                int phoneTechLead = 901000000;

                int cccdReceptionist = 120000000;
                int phoneReceptionist = 902000000;

                int cccdTechnician = 130000000;
                int phoneTechnician = 903000000;

                int cccdResident = 200000000;
                int phoneResident = 910000000;

                // ===== 0. Admin =====
                var adminUser = new User
                {
                    FirstName = "Ban",
                    LastName = "Quản Trị",
                    Email = "BanQuanTri@aptcare.vn",
                    PhoneNumber = $"0{phoneAdmin:D9}",
                    CitizenshipIdentity = $"{cccdAdmin}",
                    Status = ActiveStatus.Active
                };
                var adminAccount = new Account
                {
                    Username = "BanQuanTri@aptcare.vn",
                    Role = AccountRole.Admin,
                    EmailConfirmed = true,
                    LockoutEnabled = false,
                    MustChangePassword = false,
                    User = adminUser
                };
                adminAccount.PasswordHash = passwordHasher.HashPassword(adminAccount, "string");
                users.Add(adminUser);
                accounts.Add(adminAccount);

                // ===== 1. Manager =====
                var managerUser = new User
                {
                    FirstName = "Ban",
                    LastName = "Quản Lí",
                    Email = "BanQuanLi@aptcare.vn",
                    PhoneNumber = $"0{phoneManager:D9}",
                    CitizenshipIdentity = $"{cccdManager}",
                    Status = ActiveStatus.Active
                };
                var managerAccount = new Account
                {
                    Username = "BanQuanLi@aptcare.vn",
                    Role = AccountRole.Manager,
                    EmailConfirmed = true,
                    LockoutEnabled = false,
                    MustChangePassword = false,
                    User = managerUser
                };
                managerAccount.PasswordHash = passwordHasher.HashPassword(managerAccount, "string");
                users.Add(managerUser);
                accounts.Add(managerAccount);

                // ===== 2. Technician Lead =====
                var techLeadUser = new User
                {
                    FirstName = "Kỹ Thuật Viên",
                    LastName = "Trưởng",
                    Email = "techlead@aptcare.vn",
                    PhoneNumber = $"0{phoneTechLead:D9}",
                    CitizenshipIdentity = $"{cccdTechLead}",
                    Status = ActiveStatus.Active
                };
                var techLeadAccount = new Account
                {
                    Username = "techlead@aptcare.vn",
                    Role = AccountRole.TechnicianLead,
                    EmailConfirmed = true,
                    LockoutEnabled = false,
                    MustChangePassword = false,
                    User = techLeadUser
                };
                techLeadAccount.PasswordHash = passwordHasher.HashPassword(techLeadAccount, "string");
                users.Add(techLeadUser);
                accounts.Add(techLeadAccount);

                // ===== 3. Receptionists =====
                for (int i = 1; i <= 2; i++)
                {
                    var receptionistUser = new User
                    {
                        FirstName = "Lễ Tân",
                        LastName = i == 1 ? "Nguyễn Văn" : "Trần Thị",
                        Email = $"receptionist{i}@aptcare.vn",
                        PhoneNumber = $"0{phoneReceptionist + i:D9}",
                        CitizenshipIdentity = $"{cccdReceptionist + i}",
                        Status = ActiveStatus.Active
                    };
                    var receptionistAccount = new Account
                    {
                        Username = $"receptionist{i}@aptcare.vn",
                        Role = AccountRole.Receptionist,
                        EmailConfirmed = true,
                        LockoutEnabled = false,
                        MustChangePassword = false,
                        User = receptionistUser
                    };
                    receptionistAccount.PasswordHash = passwordHasher.HashPassword(receptionistAccount, "string");
                    users.Add(receptionistUser);
                    accounts.Add(receptionistAccount);
                }

                // ===== 4. Technicians =====
                var allTechniques = context.Techniques.ToList();
                for (int i = 1; i <= 10; i++)
                {
                    var techUser = new User
                    {
                        FirstName = "Kỹ Thuật Viên",
                        LastName = $"Số {i}",
                        Email = $"technician{i}@aptcare.vn",
                        PhoneNumber = $"0{phoneTechnician + i:D9}",
                        CitizenshipIdentity = $"{cccdTechnician + i}",
                        Status = ActiveStatus.Active
                    };
                    var techAccount = new Account
                    {
                        Username = $"technician{i}@aptcare.vn",
                        Role = AccountRole.Technician,
                        EmailConfirmed = true,
                        LockoutEnabled = false,
                        MustChangePassword = false,
                        User = techUser
                    };
                    techAccount.PasswordHash = passwordHasher.HashPassword(techAccount, "string");
                    users.Add(techUser);
                    accounts.Add(techAccount);

                    // Gán tất cả các kỹ năng cho kỹ thuật viên này
                    foreach (var technique in allTechniques)
                    {
                        technicianTechniques.Add(new TechnicianTechnique
                        {
                            Technician = techUser,
                            Technique = technique
                        });
                    }
                }

                var apartments = context.Apartments.ToList();
                int totalApartments = apartments.Count;
                int occupiedApartmentsCount = totalApartments / 2;

                var occupiedApartments = apartments.Take(occupiedApartmentsCount).ToList();

                int residentIndex = 1;
                foreach (var apartment in occupiedApartments)
                {
                    for (int u = 1; u <= 4; u++)
                    {
                        string firstName = u == 1 ? "Chủ" : "Cư Dân";
                        string lastName = $"{apartment.Room}_{u}";
                        string email = $"resident{residentIndex}@aptcare.vn";
                        string phone = $"0{phoneResident + residentIndex:D9}";
                        string cccd = $"{cccdResident + residentIndex}";
                        var user = new User
                        {
                            FirstName = firstName,
                            LastName = lastName,
                            Email = email,
                            PhoneNumber = phone,
                            CitizenshipIdentity = cccd,
                            Status = ActiveStatus.Active
                        };
                        var account = new Account
                        {
                            Username = $"resident{residentIndex}@aptcare.vn",
                            Role = AccountRole.Resident,
                            EmailConfirmed = true,
                            LockoutEnabled = false,
                            MustChangePassword = false,
                            User = user
                        };
                        account.PasswordHash = passwordHasher.HashPassword(account, "string");
                        users.Add(user);
                        accounts.Add(account);

                        residentIndex++;
                    }
                }
                context.Accounts.AddRange(accounts);
                context.TechnicianTechniques.AddRange(technicianTechniques);
                await context.SaveChangesAsync();
            }
        }

        private static async Task SeedUserApartments(AptCareSystemDBContext context)
        {
            if (!context.UserApartments.Any())
            {
                var userApartments = new List<UserApartment>();
                var apartments = context.Apartments.OrderBy(a => a.ApartmentId).AsNoTracking().ToList();
                var users = context.Users.OrderBy(u => u.UserId).AsNoTracking().ToList();

                int totalApartments = apartments.Count;
                int occupiedApartmentsCount = totalApartments / 2;
                var occupiedApartments = apartments.Take(occupiedApartmentsCount).ToList();
                int userIdx = 15;
                foreach (var apartment in occupiedApartments)
                {
                    for (int u = 1; u <= 4; u++)
                    {
                        var user = users[userIdx];
                        userApartments.Add(new UserApartment
                        {
                            UserId = user.UserId,
                            ApartmentId = apartment.ApartmentId,
                            RoleInApartment = u == 1 ? RoleInApartmentType.Owner : RoleInApartmentType.Member,
                            CreatedAt = DateTime.Now,
                            Status = ActiveStatus.Active
                        });
                        userIdx++;
                    }
                }
                context.UserApartments.AddRange(userApartments);
                await context.SaveChangesAsync();
            }
        }

        private static async Task SeedCommonAreas(AptCareSystemDBContext context)
        {
            if (!context.CommonAreas.Any())
            {
                var floors = context.Floors.OrderBy(x => x.FloorNumber).ToList();
                var commonAreas = new List<CommonArea>();

                // Tạo khu vực cho mỗi tầng
                foreach (var floor in floors)
                {
                    commonAreas.Add(new CommonArea
                    {
                        FloorId = floor.FloorId,
                        AreaCode = $"L{floor.FloorNumber:D2}-CORRIDOR",
                        Name = $"Hành lang tầng {floor.FloorNumber}",
                        Description = $"Hành lang chính khu vực tầng {floor.FloorNumber}",
                        Location = $"Tầng {floor.FloorNumber}",
                        Status = ActiveStatus.Active
                    });

                    commonAreas.Add(new CommonArea
                    {
                        FloorId = floor.FloorId,
                        AreaCode = $"L{floor.FloorNumber:D2}-LIFT",
                        Name = $"Khu vực thang máy tầng {floor.FloorNumber}",
                        Description = $"Sảnh chờ thang máy tầng {floor.FloorNumber}",
                        Location = $"Giữa tầng {floor}",
                        Status = ActiveStatus.Active
                    });

                    commonAreas.Add(new CommonArea
                    {
                        FloorId = floor.FloorId,
                        AreaCode = $"L{floor.FloorNumber:D2}-ELECTRIC",
                        Name = $"Phòng điện tầng {floor}",
                        Description = $"Phòng kỹ thuật điện tầng {floor.FloorNumber}",
                        Location = $"Cuối hành lang tầng {floor.FloorNumber}",
                        Status = ActiveStatus.Active
                    });

                    commonAreas.Add(new CommonArea
                    {
                        FloorId = floor.FloorId,
                        AreaCode = $"L{floor:D2}-WASTE",
                        Name = $"Phòng rác tầng {floor.FloorNumber}",
                        Description = $"Khu vực tập kết rác tầng {floor.FloorNumber}",
                        Location = $"Góc hành lang tầng {floor.FloorNumber}",
                        Status = ActiveStatus.Active
                    });
                }

                // Khu vực chung ngoài tầng
                commonAreas.AddRange(new[]
                {
                    new CommonArea { FloorId = null, AreaCode = "PARK-B1", Name = "Hầm xe B1", Description = "Khu vực đỗ xe cư dân và khách", Location = "Tầng hầm", Status = ActiveStatus.Active },
                    new CommonArea { FloorId = null, AreaCode = "POOL", Name = "Hồ bơi", Description = "Hồ bơi ngoài trời cho cư dân", Location = "Khu tiện ích tầng trệt", Status = ActiveStatus.Active },
                    new CommonArea { FloorId = null, AreaCode = "ROOF", Name = "Sân thượng", Description = "Khu vực đặt bồn nước và thiết bị thông gió", Location = "Tầng mái", Status = ActiveStatus.Active }
                });

                context.CommonAreas.AddRange(commonAreas);
                await context.SaveChangesAsync();
            }
        }

        private static async Task SeedCommonAreaObjects(AptCareSystemDBContext context)
        {
            if (!context.CommonAreaObjects.Any())
            {
                var areaObjects = new List<CommonAreaObject>();
                var areas = context.CommonAreas.AsNoTracking().ToList();

                foreach (var area in areas)
                {
                    if (area.AreaCode.Contains("CORRIDOR"))
                    {
                        areaObjects.Add(new CommonAreaObject { CommonAreaId = area.CommonAreaId, Name = $"Đèn hành lang {area.Name}", Description = "Hệ thống chiếu sáng dọc hành lang", Status = ActiveStatus.Active });
                        areaObjects.Add(new CommonAreaObject { CommonAreaId = area.CommonAreaId, Name = $"Camera hành lang {area.Name}", Description = "Camera an ninh giám sát hành lang", Status = ActiveStatus.Active });
                        areaObjects.Add(new CommonAreaObject { CommonAreaId = area.CommonAreaId, Name = $"Cảm biến khói {area.Name}", Description = "Phát hiện khói, kết nối hệ thống báo cháy", Status = ActiveStatus.Active });
                    }
                    else if (area.AreaCode.Contains("LIFT"))
                    {
                        areaObjects.Add(new CommonAreaObject { CommonAreaId = area.CommonAreaId, Name = $"Thang máy A - {area.Name}", Description = "Thang máy chở người khu A", Status = ActiveStatus.Active });
                        areaObjects.Add(new CommonAreaObject { CommonAreaId = area.CommonAreaId, Name = $"Thang máy B - {area.Name}", Description = "Thang máy chở hàng khu B", Status = ActiveStatus.Active });
                    }
                    else if (area.AreaCode.Contains("ELECTRIC"))
                    {
                        areaObjects.Add(new CommonAreaObject { CommonAreaId = area.CommonAreaId, Name = $"Tủ điện tổng - {area.Name}", Description = "Phân phối điện cho tầng", Status = ActiveStatus.Active });
                    }
                    else if (area.AreaCode.Contains("WASTE"))
                    {
                        areaObjects.Add(new CommonAreaObject { CommonAreaId = area.CommonAreaId, Name = $"Quạt thông gió - {area.Name}", Description = "Hút mùi, thông gió phòng rác", Status = ActiveStatus.Active });
                    }
                }

                // Khu vực chung
                var basement = areas.FirstOrDefault(a => a.AreaCode == "PARK-B1");
                if (basement != null)
                {
                    areaObjects.Add(new CommonAreaObject { CommonAreaId = basement.CommonAreaId, Name = "Đèn chiếu sáng hầm xe", Description = "Dàn đèn huỳnh quang chiếu sáng toàn bộ khu vực B1", Status = ActiveStatus.Active });
                    areaObjects.Add(new CommonAreaObject { CommonAreaId = basement.CommonAreaId, Name = "Camera an ninh hầm B1", Description = "Camera giám sát khu vực đỗ xe", Status = ActiveStatus.Active });
                    areaObjects.Add(new CommonAreaObject { CommonAreaId = basement.CommonAreaId, Name = "Cảm biến CO", Description = "Giám sát nồng độ khí thải trong hầm", Status = ActiveStatus.Active });
                }

                var roof = areas.FirstOrDefault(a => a.AreaCode == "ROOF");
                if (roof != null)
                {
                    areaObjects.Add(new CommonAreaObject { CommonAreaId = roof.CommonAreaId, Name = "Bồn nước mái", Description = "Bồn chứa nước chính của tòa nhà", Status = ActiveStatus.Active });
                    areaObjects.Add(new CommonAreaObject { CommonAreaId = roof.CommonAreaId, Name = "Quạt thông gió mái", Description = "Thiết bị hút gió tầng mái", Status = ActiveStatus.Active });
                }

                var pool = areas.FirstOrDefault(a => a.AreaCode == "POOL");
                if (pool != null)
                {
                    areaObjects.Add(new CommonAreaObject { CommonAreaId = pool.CommonAreaId, Name = "Máy lọc nước hồ bơi", Description = "Hệ thống lọc tuần hoàn nước hồ bơi", Status = ActiveStatus.Active });
                    areaObjects.Add(new CommonAreaObject { CommonAreaId = pool.CommonAreaId, Name = "Đèn hồ bơi", Description = "Chiếu sáng ban đêm quanh hồ bơi", Status = ActiveStatus.Active });
                }

                context.CommonAreaObjects.AddRange(areaObjects);
                await context.SaveChangesAsync();
            }
        }

        private static async Task SeedSlots(AptCareSystemDBContext context)
        {
            if (!context.Slots.Any())
            {
                var now = DateTime.Now;

                var slots = new List<Slot>
                {
                    new Slot
                    {
                        SlotName = "Ca sáng",
                        FromTime = new TimeSpan(8, 0, 0),   // 08:00
                        ToTime = new TimeSpan(16, 0, 0),    // 16:00
                        LastUpdated = now,
                        DisplayOrder = 1,
                        Status = ActiveStatus.Active
                    },
                    new Slot
                    {
                        SlotName = "Ca tối",
                        FromTime = new TimeSpan(16, 0, 0),  // 16:00
                        ToTime = new TimeSpan(23, 59, 0),   // 23:59
                        LastUpdated = now,
                        DisplayOrder = 2,
                        Status = ActiveStatus.Active
                    },
                    new Slot
                    {
                        SlotName = "Ca đêm",
                        FromTime = new TimeSpan(0, 0, 0),   // 00:00
                        ToTime = new TimeSpan(8, 0, 0),     // 08:00
                        LastUpdated = now,
                        DisplayOrder = 3,
                        Status = ActiveStatus.Active
                    }
                };

                context.Slots.AddRange(slots);
                await context.SaveChangesAsync();
            }
        }

        private static async Task SeedAccessories(AptCareSystemDBContext context)
        {
            if (!context.Accessories.Any())
            {
                var accessories = new List<Accessory>
                {
                    new Accessory
                    {
                        Name = "Bóng đèn LED 9W",
                        Descrption = "Bóng đèn LED tiết kiệm điện, dùng cho chiếu sáng căn hộ.",
                        Price = 35000,
                        Quantity = 200,
                        Status = ActiveStatus.Active
                    },
                    new Accessory
                    {
                        Name = "Ổ cắm điện 3 chấu",
                        Descrption = "Ổ cắm điện 3 chấu tiêu chuẩn, an toàn cho thiết bị.",
                        Price = 45000,
                        Quantity = 150,
                        Status = ActiveStatus.Active
                    },
                    new Accessory
                    {
                        Name = "Khóa cửa tay gạt",
                        Descrption = "Khóa cửa tay gạt bằng hợp kim, dùng cho cửa phòng.",
                        Price = 120000,
                        Quantity = 50,
                        Status = ActiveStatus.Active
                    },
                    new Accessory
                    {
                        Name = "Ống nước PVC 21mm",
                        Descrption = "Ống nước PVC chịu áp lực, đường kính 21mm.",
                        Price = 25000,
                        Quantity = 300,
                        Status = ActiveStatus.Active
                    },
                    new Accessory
                    {
                        Name = "Cảm biến khói",
                        Descrption = "Thiết bị cảm biến khói dùng cho hệ thống báo cháy.",
                        Price = 180000,
                        Quantity = 30,
                        Status = ActiveStatus.Active
                    },
                    new Accessory
                    {
                        Name = "Công tắc điện",
                        Descrption = "Công tắc điện âm tường, phù hợp cho mọi loại phòng.",
                        Price = 20000,
                        Quantity = 100,
                        Status = ActiveStatus.Active
                    },
                    new Accessory
                    {
                        Name = "Quạt hút gió nhà tắm",
                        Descrption = "Quạt hút gió gắn tường, giảm ẩm mốc cho nhà tắm.",
                        Price = 220000,
                        Quantity = 40,
                        Status = ActiveStatus.Active
                    },
                    new Accessory
                    {
                        Name = "Van nước 1 chiều",
                        Descrption = "Van nước 1 chiều bằng đồng, chống rò rỉ.",
                        Price = 35000,
                        Quantity = 80,
                        Status = ActiveStatus.Active
                    },
                    new Accessory
                    {
                        Name = "Bản lề cửa inox",
                        Descrption = "Bản lề cửa bằng inox 304, chống gỉ sét.",
                        Price = 15000,
                        Quantity = 120,
                        Status = ActiveStatus.Active
                    },
                    new Accessory
                    {
                        Name = "Dây điện đôi 2x1.5mm",
                        Descrption = "Dây điện đôi lõi đồng, cách điện PVC, tiết diện 2x1.5mm.",
                        Price = 12000,
                        Quantity = 500,
                        Status = ActiveStatus.Active
                    }
                };

                context.Accessories.AddRange(accessories);
                await context.SaveChangesAsync();
            }
        }


    }
}
