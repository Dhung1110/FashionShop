using Microsoft.AspNetCore.Mvc;
using FashionShop.Models;
using FashionShop.Data;
using Microsoft.EntityFrameworkCore;

namespace FashionShop.Controllers;

public class CartController : Controller
{
    private readonly ApplicationDbContext _context;

    public CartController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: Cart
    public async Task<IActionResult> Index()
    {
        var userId = User.Identity?.Name;
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToAction("Login", "Account");
        }

        var cartItems = await _context.CartItems
            .Include(ci => ci.Product)
            .Include(ci => ci.Product.Category)
            .Where(ci => ci.UserId == userId)
            .ToListAsync();

        // Calculate totals
        decimal subtotal = cartItems.Sum(ci => ci.TotalPrice);
        decimal shipping = subtotal > 1000 ? 0 : 30000; // Free shipping for orders over 1,000,000 VND
        decimal total = subtotal + shipping;

        ViewBag.Subtotal = subtotal;
        ViewBag.Shipping = shipping;
        ViewBag.Total = total;

        return View(cartItems);
    }

    // POST: Cart/Add
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int productId, int quantity = 1)
    {
        var userId = User.Identity?.Name;
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToAction("Login", "Account");
        }

        var product = await _context.Products.FindAsync(productId);
        if (product == null)
        {
            return NotFound();
        }

        if (product.StockQuantity < quantity)
        {
            TempData["Error"] = "Not enough stock available.";
            return RedirectToAction("Details", "Products", new { id = productId });
        }

        var existingCartItem = await _context.CartItems
            .FirstOrDefaultAsync(ci => ci.UserId == userId && ci.ProductId == productId);

        if (existingCartItem != null)
        {
            // Update existing cart item
            if (existingCartItem.Quantity + quantity > product.StockQuantity)
            {
                TempData["Error"] = "Not enough stock available.";
                return RedirectToAction("Details", "Products", new { id = productId });
            }
            
            existingCartItem.Quantity += quantity;
            existingCartItem.UnitPrice = product.Price;
            existingCartItem.TotalPrice = existingCartItem.Quantity * existingCartItem.UnitPrice;
            existingCartItem.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            // Create new cart item
            var cartItem = new CartItem
            {
                UserId = userId,
                ProductId = productId,
                Quantity = quantity,
                UnitPrice = product.Price,
                TotalPrice = quantity * product.Price,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.CartItems.Add(cartItem);
        }

        await _context.SaveChangesAsync();
        TempData["Success"] = "Product added to cart.";
        return RedirectToAction("Index", "Products");
    }

    // POST: Cart/Update
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int cartItemId, int quantity)
    {
        var userId = User.Identity?.Name;
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToAction("Login", "Account");
        }

        var cartItem = await _context.CartItems
            .Include(ci => ci.Product)
            .FirstOrDefaultAsync(ci => ci.Id == cartItemId && ci.UserId == userId);

        if (cartItem == null)
        {
            return NotFound();
        }

        if (quantity <= 0)
        {
            // Remove item if quantity is 0
            _context.CartItems.Remove(cartItem);
        }
        else
        {
            // Update quantity
            if (quantity > cartItem.Product.StockQuantity)
            {
                TempData["Error"] = "Not enough stock available.";
                return RedirectToAction("Index");
            }

            cartItem.Quantity = quantity;
            cartItem.UnitPrice = cartItem.Product.Price;
            cartItem.TotalPrice = quantity * cartItem.UnitPrice;
            cartItem.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return RedirectToAction("Index");
    }

    // POST: Cart/Remove
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(int cartItemId)
    {
        var userId = User.Identity?.Name;
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToAction("Login", "Account");
        }

        var cartItem = await _context.CartItems
            .FirstOrDefaultAsync(ci => ci.Id == cartItemId && ci.UserId == userId);

        if (cartItem != null)
        {
            _context.CartItems.Remove(cartItem);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Product removed from cart.";
        }

        return RedirectToAction("Index");
    }

    // POST: Cart/Clear
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Clear()
    {
        var userId = User.Identity?.Name;
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToAction("Login", "Account");
        }

        var cartItems = await _context.CartItems
            .Where(ci => ci.UserId == userId)
            .ToListAsync();

        _context.CartItems.RemoveRange(cartItems);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Cart cleared.";

        return RedirectToAction("Index");
    }
}