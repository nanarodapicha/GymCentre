using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using ASPGymCentre.Data;
using ASPGymCentre.Models;

namespace ASPGymCentre.Controllers
{
    public class ExercisesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ExercisesController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var data = _context.Exercises
                .Include(e => e.Instructors)
                .Include(e => e.Plans);

            return View(await data.ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var exercise = await _context.Exercises
                .Include(e => e.Instructors)
                .Include(e => e.Plans)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (exercise == null)
            {
                return NotFound();
            }

            return View(exercise);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            ViewData["InstructorId"] = new SelectList(_context.Instructors, "Id", "Name");
            ViewData["PlanId"] = new SelectList(_context.Plans, "Id", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(Exercise exercise)
        {
            if (exercise.PlanId == 0)
            {
                ModelState.AddModelError("PlanId", "Моля, изберете план.");
            }

            if (exercise.InstructorId == 0)
            {
                ModelState.AddModelError("InstructorId", "Моля, изберете инструктор.");
            }

            if (string.IsNullOrWhiteSpace(exercise.Day))
            {
                ModelState.AddModelError("Day", "Полето Ден е задължително.");
            }

            if (string.IsNullOrWhiteSpace(exercise.StartTime))
            {
                ModelState.AddModelError("StartTime", "Полето Начален час е задължително.");
            }

            if (string.IsNullOrWhiteSpace(exercise.EndTime))
            {
                ModelState.AddModelError("EndTime", "Полето Краен час е задължително.");
            }

            if (ModelState.IsValid)
            {
                exercise.RegisteredDate = DateTime.Now;
                _context.Exercises.Add(exercise);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["InstructorId"] = new SelectList(_context.Instructors, "Id", "Name", exercise.InstructorId);
            ViewData["PlanId"] = new SelectList(_context.Plans, "Id", "Name", exercise.PlanId);

            return View(exercise);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var exercise = await _context.Exercises.FindAsync(id);
            if (exercise == null)
            {
                return NotFound();
            }

            ViewData["InstructorId"] = new SelectList(_context.Instructors, "Id", "Name", exercise.InstructorId);
            ViewData["PlanId"] = new SelectList(_context.Plans, "Id", "Name", exercise.PlanId);

            return View(exercise);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, Exercise exercise)
        {
            if (id != exercise.Id)
            {
                return NotFound();
            }

            if (exercise.PlanId == 0)
            {
                ModelState.AddModelError("PlanId", "Моля, изберете план.");
            }

            if (exercise.InstructorId == 0)
            {
                ModelState.AddModelError("InstructorId", "Моля, изберете инструктор.");
            }

            if (string.IsNullOrWhiteSpace(exercise.Day))
            {
                ModelState.AddModelError("Day", "Полето Ден е задължително.");
            }

            if (string.IsNullOrWhiteSpace(exercise.StartTime))
            {
                ModelState.AddModelError("StartTime", "Полето Начален час е задължително.");
            }

            if (string.IsNullOrWhiteSpace(exercise.EndTime))
            {
                ModelState.AddModelError("EndTime", "Полето Краен час е задължително.");
            }

            var existingExercise = await _context.Exercises
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == id);

            if (existingExercise == null)
            {
                return NotFound();
            }

            exercise.RegisteredDate = existingExercise.RegisteredDate;

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(exercise);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Exercises.Any(e => e.Id == exercise.Id))
                    {
                        return NotFound();
                    }

                    throw;
                }
            }

            ViewData["InstructorId"] = new SelectList(_context.Instructors, "Id", "Name", exercise.InstructorId);
            ViewData["PlanId"] = new SelectList(_context.Plans, "Id", "Name", exercise.PlanId);

            return View(exercise);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var exercise = await _context.Exercises
                .Include(e => e.Instructors)
                .Include(e => e.Plans)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (exercise == null)
            {
                return NotFound();
            }

            return View(exercise);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var exercise = await _context.Exercises.FindAsync(id);

            if (exercise != null)
            {
                _context.Exercises.Remove(exercise);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}