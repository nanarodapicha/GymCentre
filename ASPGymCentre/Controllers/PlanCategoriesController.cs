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
    public class PlanCategoriesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PlanCategoriesController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _context.PlanCategory.ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var planCategory = await _context.PlanCategory
                .FirstOrDefaultAsync(m => m.Id == id);

            if (planCategory == null)
            {
                return NotFound();
            }

            return View(planCategory);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([Bind("Id,Name,Description")] PlanCategory planCategory)
        {
            if (string.IsNullOrWhiteSpace(planCategory.Name))
            {
                ModelState.AddModelError("Name", "Полето Име е задължително.");
            }

            if (string.IsNullOrWhiteSpace(planCategory.Description))
            {
                ModelState.AddModelError("Description", "Полето Описание е задължително.");
            }

            if (ModelState.IsValid)
            {
                _context.Add(planCategory);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(planCategory);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var planCategory = await _context.PlanCategory.FindAsync(id);
            if (planCategory == null)
            {
                return NotFound();
            }

            return View(planCategory);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description")] PlanCategory planCategory)
        {
            if (id != planCategory.Id)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(planCategory.Name))
            {
                ModelState.AddModelError("Name", "Полето Име е задължително.");
            }

            if (string.IsNullOrWhiteSpace(planCategory.Description))
            {
                ModelState.AddModelError("Description", "Полето Описание е задължително.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(planCategory);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PlanCategoryExists(planCategory.Id))
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

            return View(planCategory);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var planCategory = await _context.PlanCategory
                .FirstOrDefaultAsync(m => m.Id == id);

            if (planCategory == null)
            {
                return NotFound();
            }

            return View(planCategory);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var planCategory = await _context.PlanCategory.FindAsync(id);

            if (planCategory != null)
            {
                _context.PlanCategory.Remove(planCategory);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool PlanCategoryExists(int id)
        {
            return _context.PlanCategory.Any(e => e.Id == id);
        }
    }
}