using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ASPGymCentre.Data;
using ASPGymCentre.Models;
using Microsoft.AspNetCore.Authorization;

namespace ASPGymCentre.Controllers
{
    public class InstructorsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public InstructorsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            return View(
                await _context.Instructors
                    .OrderBy(x => x.Name)
                    .ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var instructor = await _context.Instructors
                .FirstOrDefaultAsync(m => m.Id == id);

            if (instructor == null)
            {
                return NotFound();
            }

            return View(instructor);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([Bind("Id,Name,Description,Image,PhoneNumber")] Instructor instructor)
        {
            if (string.IsNullOrWhiteSpace(instructor.Name))
            {
                ModelState.AddModelError("Name", "Полето Име е задължително.");
            }

            if (string.IsNullOrWhiteSpace(instructor.Description))
            {
                ModelState.AddModelError("Description", "Полето Описание е задължително.");
            }

            if (string.IsNullOrWhiteSpace(instructor.Image))
            {
                ModelState.AddModelError("Image", "Полето Снимка е задължително.");
            }

            if (string.IsNullOrWhiteSpace(instructor.PhoneNumber))
            {
                ModelState.AddModelError("PhoneNumber", "Полето Телефонен номер е задължително.");
            }

            if (ModelState.IsValid)
            {
                instructor.RegisteredDate = DateTime.Now;
                _context.Add(instructor);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(instructor);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var instructor = await _context.Instructors.FindAsync(id);
            if (instructor == null)
            {
                return NotFound();
            }

            return View(instructor);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,Image,PhoneNumber")] Instructor instructor)
        {
            if (id != instructor.Id)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(instructor.Name))
            {
                ModelState.AddModelError("Name", "Полето Име е задължително.");
            }

            if (string.IsNullOrWhiteSpace(instructor.Description))
            {
                ModelState.AddModelError("Description", "Полето Описание е задължително.");
            }

            if (string.IsNullOrWhiteSpace(instructor.Image))
            {
                ModelState.AddModelError("Image", "Полето Снимка е задължително.");
            }

            if (string.IsNullOrWhiteSpace(instructor.PhoneNumber))
            {
                ModelState.AddModelError("PhoneNumber", "Полето Телефонен номер е задължително.");
            }

            var existingInstructor = await _context.Instructors
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == id);

            if (existingInstructor == null)
            {
                return NotFound();
            }

            instructor.RegisteredDate = existingInstructor.RegisteredDate;

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(instructor);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!InstructorExists(instructor.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }

                return RedirectToAction(nameof(Index));
            }

            return View(instructor);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var instructor = await _context.Instructors
                .FirstOrDefaultAsync(m => m.Id == id);

            if (instructor == null)
            {
                return NotFound();
            }

            return View(instructor);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var instructor = await _context.Instructors.FindAsync(id);

            if (instructor != null)
            {
                _context.Instructors.Remove(instructor);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool InstructorExists(int id)
        {
            return _context.Instructors.Any(e => e.Id == id);
        }
    }
}