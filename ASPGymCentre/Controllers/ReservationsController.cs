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

        // Само админ вижда всички резервации
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

        // Страница за user: налични тренировки + неговите резервации
        public async Task<IActionResult> MyReservations()
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(userId))
                return Challenge();

            var exercises = await _context.Exercises
                .Include(e => e.Plans)
                .Include(e => e.Instructors)
                .OrderBy(e => e.Day)
                .ThenBy(e => e.StartTime)
                .ToListAsync();

            var myReservations = await _context.Reservations
                .Where(r => r.ClientId == userId)
                .Include(r => r.Exercises)
                    .ThenInclude(e => e.Plans)
                .Include(r => r.Exercises)
                    .ThenInclude(e => e.Instructors)
                .OrderByDescending(r => r.RegisteredDate)
                .ToListAsync();

            ViewBag.AvailableExercises = exercises;
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

        // Само админ вижда детайли
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var reservation = await _context.Reservations
                .Include(r => r.Clients)
                .Include(r => r.Exercises)
                    .ThenInclude(e => e.Plans)
                .Include(r => r.Exercises)
                    .ThenInclude(e => e.Instructors)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (reservation == null) return NotFound();

            return View(reservation);
        }

        // Оставяме Create, ако искаш и dropdown вариант
        public IActionResult Create()
        {
            var exercises = _context.Exercises
                .Select(e => new
                {
                    Id = e.Id,
                    Text = e.Day + " " + e.StartTime + " - " + e.EndTime
                }).ToList();

            ViewBag.ExerciseId = new SelectList(exercises, "Id", "Text");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ExerciseId")] Reservation reservation)
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(userId))
                return Challenge();

            if (reservation.ExerciseId == 0)
            {
                ModelState.AddModelError("ExerciseId", "Моля, изберете тренировка.");
            }

            reservation.ClientId = userId;
            reservation.RegisteredDate = DateTime.Now;

            bool alreadyReserved = await _context.Reservations
                .AnyAsync(r => r.ClientId == userId && r.ExerciseId == reservation.ExerciseId);

            if (alreadyReserved)
            {
                ModelState.AddModelError("", "Вече имате резервация за тази тренировка.");
            }

            if (ModelState.IsValid)
            {
                _context.Add(reservation);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(MyReservations));
            }

            var exercises = _context.Exercises
                .Select(e => new
                {
                    Id = e.Id,
                    Text = e.Day + " " + e.StartTime + " - " + e.EndTime
                }).ToList();

            ViewBag.ExerciseId = new SelectList(exercises, "Id", "Text", reservation.ExerciseId);

            return View(reservation);
        }

        // Само админ редактира
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var reservation = await _context.Reservations.FindAsync(id);
            if (reservation == null) return NotFound();

            var exercises = _context.Exercises
                .Select(e => new
                {
                    Id = e.Id,
                    Text = e.Day + " " + e.StartTime + " - " + e.EndTime
                }).ToList();

            ViewBag.ExerciseId = new SelectList(exercises, "Id", "Text", reservation.ExerciseId);

            return View(reservation);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,ClientId,ExerciseId")] Reservation model)
        {
            if (id != model.Id) return NotFound();

            if (model.ExerciseId == 0)
            {
                ModelState.AddModelError("ExerciseId", "Моля, изберете тренировка.");
            }

            var reservation = await _context.Reservations.FindAsync(id);
            if (reservation == null) return NotFound();

            if (ModelState.IsValid)
            {
                reservation.ExerciseId = model.ExerciseId;
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            var exercises = _context.Exercises
                .Select(e => new
                {
                    Id = e.Id,
                    Text = e.Day + " " + e.StartTime + " - " + e.EndTime
                }).ToList();

            ViewBag.ExerciseId = new SelectList(exercises, "Id", "Text", model.ExerciseId);

            return View(model);
        }

        // Само админ трие
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var reservation = await _context.Reservations
                .Include(r => r.Clients)
                .Include(r => r.Exercises)
                    .ThenInclude(e => e.Plans)
                .Include(r => r.Exercises)
                    .ThenInclude(e => e.Instructors)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (reservation == null) return NotFound();

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