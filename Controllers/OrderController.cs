using Microsoft.AspNetCore.Mvc;
using FashionShop.Models;
using FashionShop.Data;
using Microsoft.EntityFrameworkCore;

namespace FashionShop.Controllers;

public class OrderController : Controller
{
    private readonly ApplicationDbContext _context;

    public OrderController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: Order/Checkout
    public async Task<IActionResult> Checkout()
    {
        var userId = User.Identity?.Name;
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToAction("Login", "Account");
        }

        var cartItems = await _context.CartItems
            .Include(ci => ci.Product)
            .Where(ci => ci.UserId == userId)
            .ToListAsync();

        if (!cartItems.Any())
        {
            TempData["Error"] = "Your cart is empty.";
            return RedirectToAction("Index", "Cart");
        }

        // Calculate totals
        decimal subtotal = cartItems.Sum(ci => ci.TotalPrice);
        decimal shipping = subtotal > 1000 ? 0 : 30000; // Free shipping for orders over 1,000,000 VND
        decimal total = subtotal + shipping;

        // Create order model
        var order = new Order
        {
            UserId = userId,
            CustomerName = $"{await GetFirstName(userId)} {await GetLastName(userId)}",
            CustomerEmail = userId,
            CustomerAddress = await GetUserAddress(userId),
            CustomerCity = await GetUserCity(userId),
            CustomerProvince = await GetUserProvince(userId),
            CustomerPostalCode = await GetUserPostalCode(userId),
            TotalAmount = total,
            Status = OrderStatus.Pending,
            OrderDate = DateTime.UtcNow,
            OrderDetails = cartItems.Select(ci => new OrderDetail
            {
                ProductId = ci.ProductId,
                UnitPrice = ci.UnitPrice,
                Quantity = ci.Quantity,
                TotalPrice = ci.TotalPrice
            }).ToList()
        };

        return View(order);
    }

    // POST: Order/Complete
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(Order order)
    {
        var userId = User.Identity?.Name;
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToAction("Login", "Account");
        }

        if (ModelState.IsValid)
        {
            // Update order with customer information
            order.UserId = userId;
            order.CustomerName = order.CustomerName;
            order.CustomerEmail = order.CustomerEmail;
            order.CustomerAddress = order.CustomerAddress;
            order.CustomerCity = order.CustomerCity;
            order.CustomerProvince = order.CustomerProvince;
            order.CustomerPostalCode = order.CustomerPostalCode;
            order.Status = OrderStatus.Pending;
            order.OrderDate = DateTime.UtcNow;

            // Add order to database
            _context.Orders.Add(order);
            
            // Update product stock
            foreach (var detail in order.OrderDetails)
            {
                var product = await _context.Products.FindAsync(detail.ProductId);
                if (product != null)
                {
                    product.StockQuantity -= detail.Quantity;
                    product.UpdatedAt = DateTime.UtcNow;
                }
            }

            // Remove cart items
            var cartItems = await _context.CartItems
                .Where(ci => ci.UserId == userId)
                .ToListAsync();
            _context.CartItems.RemoveRange(cartItems);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Order placed successfully!";
            return RedirectToAction("Confirmation", new { id = order.Id });
        }

        return View("Checkout", order);
    }

    // GET: Order/Confirmation
    public async Task<IActionResult> Confirmation(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var order = await _context.Orders
            .Include(o => o.OrderDetails)
            .ThenInclude(od => od.Product)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
        {
            return NotFound();
        }

        return View(order);
    }

    // GET: Order/History
    public async Task<IActionResult> History()
    {
        var userId = User.Identity?.Name;
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToAction("Login", "Account");
        }

        var orders = await _context.Orders
            .Include(o => o.OrderDetails)
            .ThenInclude(od => od.Product)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();

        return View(orders);
    }

    // GET: Order/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var order = await _context.Orders
            .Include(o => o.OrderDetails)
            .ThenInclude(od => od.Product)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
        {
            return NotFound();
        }

        return View(order);
    }

    // Helper methods to get user information
    private async Task<string> GetFirstName(string userId)
    {
        var user = await _context.Users.FindAsync(userId);
        return user?.FirstName ?? "";
    }

    private async Task<string> GetLastName(string userId)
    {
        var user = await _context.Users.FindAsync(userId);
        return user?.LastName ?? "";
    }

    private async Task<string> GetUserAddress(string userId)
    {
        var user = await _context.Users.FindAsync(userId);
        return user?.Address ?? "";
    }

    private async Task<string> GetUserCity(string userId)
    {
        var user = await _context.Users.FindAsync(userId);
        return user?.City ?? "";
    }

    private async Task<string> GetUserProvince(string userId)
    {
        var user = await _context.Users.FindAsync(userId);
        return user?.Province ?? "";
    }

    private async Task<string> GetUserPostalCode(string userId)
    {
        var user = await _context.Users.FindAsync(userId);
        return user?.PostalCode ?? "";
    }
}