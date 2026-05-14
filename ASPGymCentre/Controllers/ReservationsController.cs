using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ASPGymCentre.Data;
using ASPGymCentre.Models;

namespace ASPGymCentre.Controllers
{
    [Authorize]
    public class ReservationsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<Client> _userManager;

        public ReservationsController(ApplicationDbContext context, UserManager<Client> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            var reservations = _context.Reservations
                .Include(r => r.Clients)
                .Include(r => r.Exercises)
                    .ThenInclude(e => e.Plans)
                .Include(r => r.Exercises)
                    .ThenInclude(e => e.Instructors);

            return View(await reservations.ToListAsync());
        }

        public async Task<IActionResult> MyReservations(
    string searchDay,
    int? instructorId,
    int? planId)
        {
            var userId = _userManager.GetUserId(User);

            var exercisesQuery = _context.Exercises
                .Include(x => x.Reservations)
                .Include(x => x.Plans)
                .Include(x => x.Instructors)
                .AsQueryable();


            if (!string.IsNullOrEmpty(searchDay))
                exercisesQuery = exercisesQuery
                    .Where(x => x.Day == searchDay);


            if (instructorId.HasValue)
                exercisesQuery = exercisesQuery
                    .Where(x => x.InstructorId == instructorId);


            if (planId.HasValue)
                exercisesQuery = exercisesQuery
                    .Where(x => x.PlanId == planId);



            ViewBag.PlanId =
                new SelectList(
                    _context.Plans
                        .OrderBy(x => x.Name)
                        .ToList(),
                    "Id",
                    "Name",
                    planId);



            ViewBag.InstructorId =
                new SelectList(
                    _context.Instructors
                        .OrderBy(x => x.Name)
                        .ToList(),
                    "Id",
                    "Name",
                    instructorId);



            var orderedDays = new List<string>
    {
        "Понеделник",
        "Вторник",
        "Сряда",
        "Четвъртък",
        "Петък",
        "Събота",
        "Неделя"
    };


            var days = _context.Exercises
                .Select(x => x.Day)
                .Distinct()
                .AsEnumerable()
                .OrderBy(x => orderedDays.IndexOf(x))
                .ToList();


            ViewBag.Days =
                new SelectList(
                    days,
                    searchDay);



            ViewBag.AvailableExercises =
                await exercisesQuery.ToListAsync();



            var myReservations = await _context.Reservations
                .Where(x => x.ClientId == userId)
                .Include(x => x.Exercises)
                    .ThenInclude(x => x.Plans)
                .Include(x => x.Exercises)
                    .ThenInclude(x => x.Instructors)
                .OrderByDescending(x => x.RegisteredDate)
                .ToListAsync();



            return View(myReservations);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reserve(int exerciseId)
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(userId))
                return Challenge();

            var exerciseExists = await _context.Exercises.AnyAsync(e => e.Id == exerciseId);
            var reservationsCount = await _context.Reservations
    .CountAsync(r => r.ExerciseId == exerciseId);

            if (reservationsCount >= 15)
            {
                TempData["ReservationError"] = "Няма свободни места.";
                return RedirectToAction(nameof(MyReservations));
            }
            if (!exerciseExists)
            {
                TempData["ReservationError"] = "Невалидна тренировка.";
                return RedirectToAction(nameof(MyReservations));
            }

            bool alreadyReserved = await _context.Reservations
                .AnyAsync(r => r.ClientId == userId && r.ExerciseId == exerciseId);

            if (alreadyReserved)
            {
                TempData["ReservationError"] = "Вече имате резервация за тази тренировка.";
                return RedirectToAction(nameof(MyReservations));
            }

            var reservation = new Reservation
            {
                ClientId = userId,
                ExerciseId = exerciseId,
                RegisteredDate = DateTime.Now
            };

            _context.Reservations.Add(reservation);
            await _context.SaveChangesAsync();

            TempData["ReservationSuccess"] = "Резервацията беше създадена успешно.";
            return RedirectToAction(nameof(MyReservations));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelMine(int id)
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(userId))
                return Challenge();

            var reservation = await _context.Reservations
                .FirstOrDefaultAsync(r => r.Id == id && r.ClientId == userId);

            if (reservation == null)
            {
                TempData["ReservationError"] = "Резервацията не беше намерена.";
                return RedirectToAction(nameof(MyReservations));
            }

            _context.Reservations.Remove(reservation);
            await _context.SaveChangesAsync();

            TempData["ReservationSuccess"] = "Резервацията беше отказана успешно.";
            return RedirectToAction(nameof(MyReservations));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var reservation = await _context.Reservations
                .Include(r => r.Clients)
                .Include(r => r.Exercises)
                    .ThenInclude(e => e.Plans)
                .Include(r => r.Exercises)
                    .ThenInclude(e => e.Instructors)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (reservation == null)
                return NotFound();

            return View(reservation);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            var exercises = _context.Exercises
                .Select(e => new
                {
                    Id = e.Id,
                    Text = e.Day + " " + e.StartTime + " - " + e.EndTime
                })
                .ToList();

            var clients = _context.Users
                .OrderBy(c => c.UserName)
                .Select(c => new
                {
                    Id = c.Id,
                    Text = c.UserName + " (" + c.Name + " " + c.FamilyName + ")"
                })
                .ToList();

            ViewBag.ExerciseId = new SelectList(exercises, "Id", "Text");
            ViewBag.ClientId = new SelectList(clients, "Id", "Text");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([Bind("ClientId,ExerciseId")] Reservation reservation)
        {
            if (string.IsNullOrWhiteSpace(reservation.ClientId))
            {
                ModelState.AddModelError("ClientId", "Моля, изберете клиент.");
            }

            if (reservation.ExerciseId == 0)
            {
                ModelState.AddModelError("ExerciseId", "Моля, изберете тренировка.");
            }

            bool alreadyReserved = false;

            if (!string.IsNullOrWhiteSpace(reservation.ClientId) && reservation.ExerciseId != 0)
            {
                alreadyReserved = await _context.Reservations
                    .AnyAsync(r => r.ClientId == reservation.ClientId && r.ExerciseId == reservation.ExerciseId);
            }

            if (alreadyReserved)
            {
                ModelState.AddModelError("", "Този клиент вече има резервация за тази тренировка.");
            }

            if (ModelState.IsValid)
            {
                reservation.RegisteredDate = DateTime.Now;

                _context.Add(reservation);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            var exercises = _context.Exercises
                .Select(e => new
                {
                    Id = e.Id,
                    Text = e.Day + " " + e.StartTime + " - " + e.EndTime
                })
                .ToList();

            var clients = _context.Users
                .OrderBy(c => c.UserName)
                .Select(c => new
                {
                    Id = c.Id,
                    Text = c.UserName + " (" + c.Name + " " + c.FamilyName + ")"
                })
                .ToList();

            ViewBag.ExerciseId = new SelectList(exercises, "Id", "Text", reservation.ExerciseId);
            ViewBag.ClientId = new SelectList(clients, "Id", "Text", reservation.ClientId);

            return View(reservation);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var reservation = await _context.Reservations.FindAsync(id);
            if (reservation == null)
                return NotFound();

            var exercises = _context.Exercises
                .Select(e => new
                {
                    Id = e.Id,
                    Text = e.Day + " " + e.StartTime + " - " + e.EndTime
                })
                .ToList();

            var clients = _context.Users
                .OrderBy(c => c.UserName)
                .Select(c => new
                {
                    Id = c.Id,
                    Text = c.UserName + " (" + c.Name + " " + c.FamilyName + ")"
                })
                .ToList();

            ViewBag.ExerciseId = new SelectList(exercises, "Id", "Text", reservation.ExerciseId);
            ViewBag.ClientId = new SelectList(clients, "Id", "Text", reservation.ClientId);

            return View(reservation);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,ClientId,ExerciseId")] Reservation model)
        {
            if (id != model.Id)
                return NotFound();

            if (string.IsNullOrWhiteSpace(model.ClientId))
            {
                ModelState.AddModelError("ClientId", "Моля, изберете клиент.");
            }

            if (model.ExerciseId == 0)
            {
                ModelState.AddModelError("ExerciseId", "Моля, изберете тренировка.");
            }

            var reservation = await _context.Reservations.FindAsync(id);
            if (reservation == null)
                return NotFound();

            if (ModelState.IsValid)
            {
                reservation.ClientId = model.ClientId;
                reservation.ExerciseId = model.ExerciseId;

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            var exercises = _context.Exercises
                .Select(e => new
                {
                    Id = e.Id,
                    Text = e.Day + " " + e.StartTime + " - " + e.EndTime
                })
                .ToList();

            var clients = _context.Users
                .OrderBy(c => c.UserName)
                .Select(c => new
                {
                    Id = c.Id,
                    Text = c.UserName + " (" + c.Name + " " + c.FamilyName + ")"
                })
                .ToList();

            ViewBag.ExerciseId = new SelectList(exercises, "Id", "Text", model.ExerciseId);
            ViewBag.ClientId = new SelectList(clients, "Id", "Text", model.ClientId);

            return View(model);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var reservation = await _context.Reservations
                .Include(r => r.Clients)
                .Include(r => r.Exercises)
                    .ThenInclude(e => e.Plans)
                .Include(r => r.Exercises)
                    .ThenInclude(e => e.Instructors)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (reservation == null)
                return NotFound();

            return View(reservation);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var reservation = await _context.Reservations.FindAsync(id);

            if (reservation != null)
            {
                _context.Reservations.Remove(reservation);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}