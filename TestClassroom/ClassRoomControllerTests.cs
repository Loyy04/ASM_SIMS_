using ASM_SIMS.Controllers;
using ASM_SIMS.DB;
using ASM_SIMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace ASM_SIMS.Tests
{
    public class ClassRoomControllerTests
    {
        private readonly SimsDataContext _dbContext;
        private readonly Mock<ISession> _sessionMock;
        private readonly Mock<HttpContext> _httpContextMock;

        public ClassRoomControllerTests()
        {
            // Thiết lập cơ sở dữ liệu in-memory
            var options = new DbContextOptionsBuilder<SimsDataContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _dbContext = new SimsDataContext(options);

            // Thiết lập mock cho HttpContext và ISession
            _sessionMock = new Mock<ISession>();
            _httpContextMock = new Mock<HttpContext>();
            _httpContextMock.Setup(c => c.Session).Returns(_sessionMock.Object);

            // Giả lập session với UserId mặc định
            SetupSession("1");

            // Tạo dữ liệu mẫu
            SeedData();
        }

        private void SetupSession(string userId)
        {
            var userIdBytes = Encoding.UTF8.GetBytes(userId);
            _sessionMock.Setup(s => s.TryGetValue("UserId", out userIdBytes)).Returns(true);
        }

        private void SeedData()
        {
            // Thêm dữ liệu mẫu cho Courses
            _dbContext.Courses.AddRange(
                new Courses { Id = 1, NameCourse = "Math", Status = "Active", CreatedAt = DateTime.Now, StartDate = DateOnly.FromDateTime(DateTime.Now), EndDate = DateOnly.FromDateTime(DateTime.Now.AddDays(30)), Vote = 0 },
                new Courses { Id = 2, NameCourse = "Physics", Status = "Active", CreatedAt = DateTime.Now, StartDate = DateOnly.FromDateTime(DateTime.Now), EndDate = DateOnly.FromDateTime(DateTime.Now.AddDays(30)), Vote = 0 }
            );

            // Thêm dữ liệu mẫu cho Accounts
            _dbContext.Accounts.AddRange(
                new Account { Id = 1, RoleId = 2, Username = "teacher1", Password = "password", Email = "teacher1@example.com", Phone = "1234567890", Address = "123 Teacher St", CreatedAt = DateTime.Now },
                new Account { Id = 2, RoleId = 2, Username = "teacher2", Password = "password", Email = "teacher2@example.com", Phone = "0987654321", Address = "456 Teacher St", CreatedAt = DateTime.Now },
                new Account { Id = 3, RoleId = 3, Username = "student1", Password = "password", Email = "student1@example.com", Phone = "123456789", Address = "789 Student St", CreatedAt = DateTime.Now }
            );

            // Thêm dữ liệu mẫu cho Teachers
            _dbContext.Teachers.AddRange(
                new Teacher { Id = 1, AccountId = 1, FullName = "John Doe", Email = "john.doe@example.com", Phone = "1234567890", Address = "123 Main St", Status = "Active", CreatedAt = DateTime.Now },
                new Teacher { Id = 2, AccountId = 2, FullName = "Jane Smith", Email = "jane.smith@example.com", Phone = "0987654321", Address = "456 Oak St", Status = "Active", CreatedAt = DateTime.Now }
            );

            // Thêm dữ liệu mẫu cho Students
            _dbContext.Students.AddRange(
                new Student { Id = 1, AccountId = 3, FullName = "Nguyen Van A", Email = "a@example.com", Phone = "123456789", Address = "Hanoi", Status = "Active", CreatedAt = DateTime.Now },
                new Student { Id = 2, AccountId = 3, FullName = "Tran Thi B", Email = "b@example.com", Phone = "987654321", Address = "HCM", Status = "Active", CreatedAt = DateTime.Now }
            );

            // Thêm dữ liệu mẫu cho ClassRooms
            _dbContext.ClassRooms.AddRange(
                new ClassRoom
                {
                    Id = 1,
                    ClassName = "Math Class",
                    CourseId = 1,
                    TeacherId = 1,
                    StartDate = DateOnly.FromDateTime(DateTime.Now),
                    EndDate = DateOnly.FromDateTime(DateTime.Now.AddDays(30)),
                    Schedule = "Mon-Wed-Fri 8:00-10:00",
                    Location = "Room 101",
                    Status = "Active",
                    CreatedAt = DateTime.Now,
                    Students = new List<Student>()
                },
                new ClassRoom
                {
                    Id = 2,
                    ClassName = "Physics Class",
                    CourseId = 2,
                    TeacherId = 2,
                    StartDate = DateOnly.FromDateTime(DateTime.Now),
                    EndDate = DateOnly.FromDateTime(DateTime.Now.AddDays(30)),
                    Schedule = "Tue-Thu 10:00-12:00",
                    Location = "Room 102",
                    Status = "Active",
                    CreatedAt = DateTime.Now,
                    Students = new List<Student>()
                }
            );

            _dbContext.SaveChanges();
        }

        [Fact]
        public void Index_WithSession_ReturnsViewWithClassRooms()
        {
            var controller = new ClassRoomController(_dbContext)
            {
                ControllerContext = new ControllerContext { HttpContext = _httpContextMock.Object }
            };

            var result = controller.Index();

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<List<ClassRoomViewModel>>(viewResult.Model);
            Assert.Equal(2, model.Count);
            Assert.Equal("Math Class", model[0].ClassName);
            Assert.Equal("Physics Class", model[1].ClassName);
            Assert.Equal("Class Rooms", viewResult.ViewData["Title"]);
        }

        [Fact]
        public void Index_WithoutSession_RedirectsToLogin()
        {
            var emptyBytes = Array.Empty<byte>();
            _sessionMock.Setup(s => s.TryGetValue("UserId", out emptyBytes)).Returns(false);
            var controller = new ClassRoomController(_dbContext)
            {
                ControllerContext = new ControllerContext { HttpContext = _httpContextMock.Object }
            };

            var result = controller.Index();

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Login", redirectResult.ControllerName);
        }

        [Fact]
        public void Create_Get_ReturnsViewWithEmptyModel()
        {
            var controller = new ClassRoomController(_dbContext)
            {
                ControllerContext = new ControllerContext { HttpContext = _httpContextMock.Object }
            };

            var result = controller.Create();

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<ClassRoomViewModel>(viewResult.Model);
            Assert.Null(model.ClassName);
            var courses = Assert.IsType<List<Courses>>(viewResult.ViewData["Courses"]);
            var teachers = Assert.IsType<List<Teacher>>(viewResult.ViewData["Teachers"]);
            Assert.Equal(2, courses.Count);
            Assert.Equal(2, teachers.Count);
        }

      

        [Fact]
        public void Create_Post_DuplicateClassName_ReturnsViewWithError()
        {
            var controller = new ClassRoomController(_dbContext)
            {
                ControllerContext = new ControllerContext { HttpContext = _httpContextMock.Object }
            };
            var duplicateClassRoom = new ClassRoomViewModel
            {
                ClassName = "Math Class", // Tên đã tồn tại
                CourseId = 1,
                TeacherId = 1,
                StartDate = DateOnly.FromDateTime(DateTime.Now),
                EndDate = DateOnly.FromDateTime(DateTime.Now.AddDays(30)),
                Schedule = "Mon-Wed 9:00-11:00",
                Location = "Room 104",
                Status = "Active"
            };

            var result = controller.Create(duplicateClassRoom);

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<ClassRoomViewModel>(viewResult.Model);
            Assert.Equal("Math Class", model.ClassName);
            Assert.False(controller.ModelState.IsValid);
            Assert.Contains("Class name already exists.", controller.ModelState["ClassName"].Errors[0].ErrorMessage);
        }

        [Fact]
        public void Edit_Get_ExistingId_ReturnsViewWithModel()
        {
            var controller = new ClassRoomController(_dbContext)
            {
                ControllerContext = new ControllerContext { HttpContext = _httpContextMock.Object }
            };

            var result = controller.Edit(1);

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<ClassRoomViewModel>(viewResult.Model);
            Assert.Equal("Math Class", model.ClassName);
            Assert.Equal(1, model.Id);
            var courses = Assert.IsType<List<Courses>>(viewResult.ViewData["Courses"]);
            var teachers = Assert.IsType<List<Teacher>>(viewResult.ViewData["Teachers"]);
            Assert.Equal(2, courses.Count);
            Assert.Equal(2, teachers.Count);
        }

       
      

        [Fact]
        public void AddStudentToClass_Get_ExistingClassRoomId_ReturnsViewWithModel()
        {
            var controller = new ClassRoomController(_dbContext)
            {
                ControllerContext = new ControllerContext { HttpContext = _httpContextMock.Object }
            };

            var result = controller.AddStudentToClass(1);

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<AssignStudentsViewModel>(viewResult.Model);
            Assert.Equal(1, model.ClassRoomId);
            Assert.Equal("Math Class", model.ClassRoomName);
            Assert.Equal(2, model.Students.Count);
            Assert.All(model.Students, s => Assert.False(s.IsSelected)); // Chưa có student nào được chọn
        }



        [Fact]
        public void Details_ExistingId_ReturnsViewWithModel()
        {
            var controller = new ClassRoomController(_dbContext)
            {
                ControllerContext = new ControllerContext { HttpContext = _httpContextMock.Object }
            };

            var result = controller.Details(1);

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<ClassRoomViewModel>(viewResult.Model);
            Assert.Equal("Math Class", model.ClassName);
            Assert.Equal(1, model.Id);
            var students = Assert.IsType<List<StudentViewModel>>(viewResult.ViewData["Students"]);
            Assert.Empty(students);
            Assert.Equal("Math", viewResult.ViewData["CourseName"]);
            Assert.Equal("John Doe", viewResult.ViewData["TeacherName"]);
        }

        [Fact]
        public void Details_NonExistingId_ReturnsNotFound()
        {
            var controller = new ClassRoomController(_dbContext)
            {
                ControllerContext = new ControllerContext { HttpContext = _httpContextMock.Object }
            };

            var result = controller.Details(999);

            Assert.IsType<NotFoundResult>(result);
        }
    }
}