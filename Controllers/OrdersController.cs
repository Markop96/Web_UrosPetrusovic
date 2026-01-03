using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using UrosPetrusovic.Data;
using UrosPetrusovic.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;

namespace UrosPetrusovic.Controllers
{
    public class CartItem
    {
        public int ItemId { get; set; }
        public string Name { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }

    [Authorize]
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public OrdersController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [Authorize(Roles = "USER")]
        [HttpPost]
        public IActionResult AddToCart(int itemId, int quantity)
        {
            var item = _context.Items.Find(itemId);
            if (item == null) return NotFound();

            var cart = GetCart();
            var existing = cart.FirstOrDefault(c => c.ItemId == itemId);

            if (existing != null)
                existing.Quantity += quantity;
            else
                cart.Add(new CartItem
                {
                    ItemId = item.Id,
                    Name = item.Name,
                    Quantity = quantity,
                    Price = item.Price
                });

            SaveCart(cart);

            TempData["Success"] = $"Uspešno ste dodali {quantity} x {item.Name} u korpu!";
            return RedirectToAction("Details", "Items", new { id = itemId });
        }

        [Authorize(Roles = "USER")]
        public IActionResult Cart()
        {
            var cart = GetCart();
            return View(cart);
        }


        [Authorize(Roles = "USER")]
        [HttpPost]
        public async Task<IActionResult> Checkout(string FullName, string Phone, string Address)
        {
            var cart = GetCart();
            if (!cart.Any())
            {
                TempData["Error"] = "Vaša korpa je prazna!";
                return RedirectToAction("Cart");
            }

            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            var order = new Order
            {
                UserId = userId,
                OrderDate = DateTime.Now,
                Status = "Na čekanju",

                CustomerName = FullName,
                ContactPhone = Phone,
                ShippingAddress = Address,

                TotalPrice = cart.Sum(x => x.Price * x.Quantity),
                OrderItems = cart.Select(c => new OrderItem
                {
                    ItemId = c.ItemId,
                    Quantity = c.Quantity,
                    UnitPrice = c.Price
                }).ToList()
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            HttpContext.Session.Remove("Cart");

            TempData["Success"] = "Porudžbina je uspešno kreirana!";
            return RedirectToAction("MyOrders");
        }

        [Authorize(Roles = "USER")]
        public async Task<IActionResult> MyOrders()
        {
            var userId = _userManager.GetUserId(User);
            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }


        [Authorize(Roles = "USER")]
        public async Task<IActionResult> Edit(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Item)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            var userId = _userManager.GetUserId(User);
            if (order.UserId != userId && !User.IsInRole("ADMIN")) return Forbid();

            if (order.Status != "Na čekanju")
            {
                TempData["Error"] = "Izmena nije moguća jer porudžbina više nije na čekanju.";
                return RedirectToAction("MyOrders");
            }

            order.TotalPrice = order.OrderItems.Sum(oi => oi.UnitPrice * oi.Quantity);

            return View(order);
        }


        [Authorize(Roles = "USER")]

        [HttpPost]
        public async Task<IActionResult> Edit(Order updatedOrder)
        {
            var originalOrder = await _context.Orders.AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == updatedOrder.Id);

            if (originalOrder == null) return NotFound();
            if (originalOrder.Status != "Na čekanju")
                return BadRequest("Izmena više nije dozvoljena.");

            _context.Update(updatedOrder);
            await _context.SaveChangesAsync();
            return RedirectToAction("MyOrders");
        }

        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> AdminIndex()
        {
            var allOrders = await _context.Orders
                .Include(u => u.User)        
                .Include(o => o.OrderItems)  
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(allOrders);
        }

        [Authorize(Roles = "ADMIN")]

        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int orderId, string newStatus)
        {
            var order = await _context.Orders.FindAsync(orderId);

            if (order == null) return NotFound();

            if (order.Status == "Otkazano")
            {
                TempData["Error"] = "Nije moguće menjati status porudžbine koja je već otkazana.";
                return RedirectToAction(nameof(AdminIndex));
            }

            order.Status = newStatus;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Status porudžbine #{orderId} uspešno promenjen u {newStatus}.";
            return RedirectToAction(nameof(AdminIndex));
        }

        [Authorize(Roles = "USER,ADMIN")]

        private List<CartItem> GetCart()
        {
            var sessionData = HttpContext.Session.GetString("Cart");
            if (string.IsNullOrEmpty(sessionData)) return new List<CartItem>();

            try
            {
                return JsonConvert.DeserializeObject<List<CartItem>>(sessionData) ?? new List<CartItem>();
            }
            catch
            {
                return new List<CartItem>();
            }
        }


        [Authorize(Roles = "USER,ADMIN")]

        private void SaveCart(List<CartItem> cart)
        {
            var settings = new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore };
            string jsonString = JsonConvert.SerializeObject(cart, settings);
            HttpContext.Session.SetString("Cart", jsonString);
        }

        [Authorize(Roles = "USER,ADMIN")]

        [HttpPost]
        public async Task<IActionResult> CancelOrder(int orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);

            if (order == null || order.Status != "Na čekanju")
            {
                TempData["Error"] = "Porudžbina se ne može otkazati jer je već procesuirana.";
                return RedirectToAction("MyOrders");
            }

            order.Status = "Otkazano";
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Porudžbina #{orderId} je uspešno otkazana.";
            return RedirectToAction("MyOrders");
        }

        [Authorize(Roles = "USER,ADMIN")]

        public async Task<IActionResult> Details(int id)
        {
            var order = await _context.Orders
                .Include(u => u.User)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Item)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            var userId = _userManager.GetUserId(User);
            if (order.UserId != userId && !User.IsInRole("ADMIN"))
            {
                return Forbid();
            }

            return View(order);
        }

        [Authorize(Roles = "USER,ADMIN")]

        [HttpPost]
        public async Task<IActionResult> UpdateItemQuantity(int orderId, int orderItemId, int newQuantity)
        {
            var order = await _context.Orders.Include(o => o.OrderItems).FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null || order.Status != "Na čekanju") return BadRequest();

            var item = order.OrderItems.FirstOrDefault(oi => oi.Id == orderItemId);
            if (item != null)
            {
                item.Quantity = newQuantity;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Edit", new { id = orderId });
        }


        [Authorize(Roles = "USER,ADMIN")]

        [HttpPost]
        public async Task<IActionResult> RemoveItem(int orderId, int orderItemId)
        {
            var item = await _context.OrderItems.FindAsync(orderItemId);

            if (item != null)
            {
                _context.OrderItems.Remove(item);
                await _context.SaveChangesAsync();
            }

            var order = await _context.Orders.Include(o => o.OrderItems).FirstOrDefaultAsync(o => o.Id == orderId);
            if (order != null && !order.OrderItems.Any())
            {
                _context.Orders.Remove(order);
                await _context.SaveChangesAsync();
                return RedirectToAction("MyOrders");
            }

            return RedirectToAction("Edit", new { id = orderId });
        }

        [Authorize(Roles = "USER,ADMIN")]

        [HttpPost]
        public IActionResult UpdateCartQuantity(int itemId, int quantity)
        {
            var sessionData = HttpContext.Session.GetString("Cart");
            if (sessionData != null)
            {
                var cart = JsonConvert.DeserializeObject<List<CartItem>>(sessionData);
                var item = cart.FirstOrDefault(c => c.ItemId == itemId);
                if (item != null && quantity > 0)
                {
                    item.Quantity = quantity;
                }
                HttpContext.Session.SetString("Cart", JsonConvert.SerializeObject(cart));
            }
            return RedirectToAction("Cart");
        }

        [Authorize(Roles = "USER,ADMIN")]

        [HttpPost]
        public IActionResult RemoveFromCart(int itemId)
        {
            var sessionData = HttpContext.Session.GetString("Cart");
            if (sessionData != null)
            {
                var cart = JsonConvert.DeserializeObject<List<CartItem>>(sessionData);
                var item = cart.FirstOrDefault(c => c.ItemId == itemId);
                if (item != null)
                {
                    cart.Remove(item);
                }
                HttpContext.Session.SetString("Cart", JsonConvert.SerializeObject(cart));
            }
            return RedirectToAction("Cart");
        }

    }
}