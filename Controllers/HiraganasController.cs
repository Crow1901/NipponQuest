using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NipponQuest.Data;
using NipponQuest.Models;

namespace NipponQuest.Controllers
{
    public class HiraganasController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HiraganasController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Hiraganas
        public async Task<IActionResult> Index()
        {
            return View(await _context.Hiraganas.ToListAsync());
        }

        // GET: Hiraganas/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var hiragana = await _context.Hiraganas
                .FirstOrDefaultAsync(m => m.Id == id);
            if (hiragana == null)
            {
                return NotFound();
            }

            return View(hiragana);
        }

        // GET: Hiraganas/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Hiraganas/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Symbol,Romaji,StrokeOrderUrl,UnlockLevel")] Hiragana hiragana)
        {
            if (ModelState.IsValid)
            {
                _context.Add(hiragana);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(hiragana);
        }

        // GET: Hiraganas/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var hiragana = await _context.Hiraganas.FindAsync(id);
            if (hiragana == null)
            {
                return NotFound();
            }
            return View(hiragana);
        }

        // POST: Hiraganas/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Symbol,Romaji,StrokeOrderUrl,UnlockLevel")] Hiragana hiragana)
        {
            if (id != hiragana.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(hiragana);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!HiraganaExists(hiragana.Id))
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
            return View(hiragana);
        }

        // GET: Hiraganas/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var hiragana = await _context.Hiraganas
                .FirstOrDefaultAsync(m => m.Id == id);
            if (hiragana == null)
            {
                return NotFound();
            }

            return View(hiragana);
        }

        // POST: Hiraganas/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var hiragana = await _context.Hiraganas.FindAsync(id);
            if (hiragana != null)
            {
                _context.Hiraganas.Remove(hiragana);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool HiraganaExists(int id)
        {
            return _context.Hiraganas.Any(e => e.Id == id);
        }
    }
}
