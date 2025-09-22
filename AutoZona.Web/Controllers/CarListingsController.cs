using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AutoZona.Domain.DomainModels;
using AutoZona.Repository;

namespace AutoZona.Web.Controllers
{
    public class CarListingsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CarListingsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: CarListings
        public async Task<IActionResult> Index()
        {
            return View(await _context.CarListings.ToListAsync());
        }

        // GET: CarListings/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var carListing = await _context.CarListings
                .FirstOrDefaultAsync(m => m.Id == id);
            if (carListing == null)
            {
                return NotFound();
            }

            return View(carListing);
        }

        // GET: CarListings/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: CarListings/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Make,Model,Year,Price,Description,Mileage,Fuel,Color,Transmission,BodyType,IsActive,ListingOwnerId,CreatedAt,UpdatedAt,Id")] CarListing carListing)
        {
            if (ModelState.IsValid)
            {
                carListing.Id = Guid.NewGuid();
                _context.Add(carListing);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(carListing);
        }

        // GET: CarListings/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var carListing = await _context.CarListings.FindAsync(id);
            if (carListing == null)
            {
                return NotFound();
            }
            return View(carListing);
        }

        // POST: CarListings/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Make,Model,Year,Price,Description,Mileage,Fuel,Color,Transmission,BodyType,IsActive,ListingOwnerId,CreatedAt,UpdatedAt,Id")] CarListing carListing)
        {
            if (id != carListing.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(carListing);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CarListingExists(carListing.Id))
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
            return View(carListing);
        }

        // GET: CarListings/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var carListing = await _context.CarListings
                .FirstOrDefaultAsync(m => m.Id == id);
            if (carListing == null)
            {
                return NotFound();
            }

            return View(carListing);
        }

        // POST: CarListings/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var carListing = await _context.CarListings.FindAsync(id);
            if (carListing != null)
            {
                _context.CarListings.Remove(carListing);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CarListingExists(Guid id)
        {
            return _context.CarListings.Any(e => e.Id == id);
        }
    }
}
