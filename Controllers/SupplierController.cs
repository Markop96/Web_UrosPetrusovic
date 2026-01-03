using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UrosPetrusovic.Data;
using UrosPetrusovic.Models;


[Authorize(Roles = "ADMIN")]

public class SupplierController : Controller
{
    private readonly ApplicationDbContext _context;
    public SupplierController(ApplicationDbContext context) => _context = context;

    public IActionResult Index() => View(_context.Suppliers.ToList());

    public IActionResult Create() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Supplier supplier)
    {
        if (!ModelState.IsValid) return View(supplier);
        _context.Suppliers.Add(supplier);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public IActionResult Edit(int id)
    {
        var supplier = _context.Suppliers.Find(id);
        if (supplier == null) return NotFound();
        return View(supplier);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Supplier supplier)
    {
        if (!ModelState.IsValid) return View(supplier);
        _context.Update(supplier);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public IActionResult Delete(int id)
    {
        var supplier = _context.Suppliers.Find(id);
        if (supplier == null) return NotFound();
        return View(supplier);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var supplier = _context.Suppliers.Find(id);
        _context.Suppliers.Remove(supplier);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}
