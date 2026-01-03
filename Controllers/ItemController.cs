using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using UrosPetrusovic.Data;
using UrosPetrusovic.Models;

namespace UrosPetrusovic.Controllers
{
    public class ItemsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ItemsController(ApplicationDbContext context)
        {
            _context = context;
        }
        [Authorize(Roles = "ADMIN")]
        public IActionResult Create()
        {

            ViewBag.SupplierId = new SelectList(_context.Suppliers, "Id", "Name");
            ViewBag.CatalogId = new SelectList(_context.Catalogs, "Id", "Name");
            return View();
        }
        [Authorize(Roles = "ADMIN")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Item item)
        {
            if (ModelState.IsValid)
            {
                if (item.SlikaFile != null && item.SlikaFile.Length > 0)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        await item.SlikaFile.CopyToAsync(memoryStream);
                        item.Slika = memoryStream.ToArray();
                    }
                }

                _context.Add(item);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewBag.SupplierId = new SelectList(_context.Suppliers, "Id", "Name", item.SupplierId);
            ViewBag.CatalogId = new SelectList(_context.Catalogs, "Id", "Name", item.CatalogId);
            return View(item);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var item = await _context.Items
                .Include(i => i.Supplier)
                .Include(i => i.Catalog)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (item == null) return NotFound();

            return View(item);
        }

        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var item = await _context.Items.FindAsync(id);
            if (item == null) return NotFound();

            ViewBag.SupplierId = new SelectList(_context.Suppliers, "Id", "Name", item.SupplierId);
            ViewBag.CatalogId = new SelectList(_context.Catalogs, "Id", "Name", item.CatalogId);
            return View(item);
        }

        [Authorize(Roles = "ADMIN")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Item item)
        {
            if (id != item.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    if (item.SlikaFile != null && item.SlikaFile.Length > 0)
                    {
                        using (var memoryStream = new MemoryStream())
                        {
                            await item.SlikaFile.CopyToAsync(memoryStream);
                            item.Slika = memoryStream.ToArray();
                        }
                    }
                    else
                    {
                        _context.Entry(item).Property(x => x.Slika).IsModified = false;
                    }

                    _context.Update(item);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Items.Any(e => e.Id == item.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewBag.SupplierId = new SelectList(_context.Suppliers, "Id", "Name", item.SupplierId);
            ViewBag.CatalogId = new SelectList(_context.Catalogs, "Id", "Name", item.CatalogId);
            return View(item);
        }

        [Authorize(Roles = "ADMIN")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var item = await _context.Items.FindAsync(id);
            if (item != null)
            {
                _context.Items.Remove(item);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }


        public async Task<IActionResult> Index()
        {
            var items = await _context.Items
                .Include(i => i.Supplier)
                .Include(i => i.Catalog)
                .ToListAsync();

            ViewBag.Catalogs = await _context.Catalogs.ToListAsync();

            return View(items);
        }
    }

}